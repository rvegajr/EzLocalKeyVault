using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Noctusoft.EzAzureLocalKeyVault.Options;
using Noctusoft.EzLocalKeyVault.Core;
using Noctusoft.EzLocalKeyVault.Options;
using System.IO;

namespace Noctusoft.EzAzureLocalKeyVault.Core;

/// <summary>
/// Implementation of the hybrid key vault service that can use either Azure Key Vault or local key vault.
/// </summary>
public class HybridKeyVault : IHybridKeyVault, ILocalKeyVault
{
    private readonly EzAzureLocalKeyVaultOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<HybridKeyVault> _logger;
    private readonly LocalKeyVault _localKeyVault;
    private readonly SecretClient? _azureSecretClient;
    private readonly ConcurrentDictionary<string, CachedSecret> _secretCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private DateTime _lastCacheRefresh = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridKeyVault"/> class.
    /// </summary>
    /// <param name="options">The options for configuring the hybrid key vault.</param>
    /// <param name="environment">The host environment.</param>
    /// <param name="logger">The logger.</param>
    public HybridKeyVault(
        IOptions<EzAzureLocalKeyVaultOptions> options,
        IHostEnvironment environment,
        ILogger<HybridKeyVault> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;

        // Create the local key vault
        _localKeyVault = new LocalKeyVault(
            new LocalKeyVaultOptions
            {
                VaultFilePath = _options.LocalKeyVaultOptions.VaultFilePath,
                ReloadOnChange = _options.LocalKeyVaultOptions.ReloadOnChange,
                LogSubstitutions = _options.LocalKeyVaultOptions.LogSubstitutions,
                RedactionPatterns = _options.LocalKeyVaultOptions.RedactionPatterns
            },
            _logger as ILogger<LocalKeyVault> ??
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<LocalKeyVault>());

        // Create the Azure Key Vault client if a URI is provided
        if (!string.IsNullOrEmpty(_options.AzureKeyVaultUri))
        {
            try
            {
                _azureSecretClient = new SecretClient(
                    new Uri(_options.AzureKeyVaultUri),
                    new DefaultAzureCredential());

                _logger.LogInformation("Azure Key Vault client initialized with URI: {Uri}", _options.AzureKeyVaultUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Key Vault client with URI: {Uri}",
                    _options.AzureKeyVaultUri);
            }
        }
    }

    /// <inheritdoc />
    public bool IsUsingLocalKeyVault => ShouldUseLocalKeyVault();

    /// <inheritdoc />
    public bool IsUsingAzureKeyVault => !IsUsingLocalKeyVault;

    /// <inheritdoc />
    public string? GetSecret(string key)
    {
        if (ShouldUseLocalKeyVault())
        {
            _logger.LogDebug("Using local key vault to get secret: {Key}", key);
            return _localKeyVault.GetSecret(key);
        }

        // Check if we need to refresh the cache
        RefreshCacheIfNeeded().GetAwaiter().GetResult();

        // Check if the key is in the Azure Key Vault format
        if (key.StartsWith(_options.AzureKeyVaultPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var secretName = key.Substring(_options.AzureKeyVaultPrefix.Length);
            _logger.LogDebug("Getting Azure Key Vault secret: {SecretName}", secretName);
            return GetAzureSecretAsync(secretName).GetAwaiter().GetResult();
        }

        // Try to get the secret from the cache
        if (_secretCache.TryGetValue(key, out var cachedSecret) && !cachedSecret.IsExpired)
        {
            _logger.LogDebug("Retrieved secret from cache: {Key}", key);
            return cachedSecret.Value;
        }

        // Try to get the secret from Azure Key Vault
        try
        {
            var secretValue = GetAzureSecretAsync(key).GetAwaiter().GetResult();
            if (secretValue != null)
            {
                if (_options.CacheAzureKeyVaultSecrets)
                {
                    _secretCache[key] = new CachedSecret(secretValue,
                        DateTime.UtcNow.AddMinutes(_options.CacheDurationMinutes));
                }

                return secretValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret from Azure Key Vault: {Key}", key);
        }

        // Fallback to local key vault if enabled
        if (_options.FallbackToLocalKeyVault)
        {
            _logger.LogDebug("Falling back to local key vault for secret: {Key}", key);
            return _localKeyVault.GetSecret(key);
        }

        return null;
    }

    /// <inheritdoc />
    public IDictionary<string, string> GetAll()
    {
        if (ShouldUseLocalKeyVault())
        {
            _logger.LogDebug("Using local key vault to get all secrets");
            return _localKeyVault.GetAll();
        }

        // Refresh the cache if needed
        RefreshCacheIfNeeded().GetAwaiter().GetResult();

        // Get all secrets from Azure Key Vault
        try
        {
            var secrets = GetAllAzureSecretsAsync().GetAwaiter().GetResult();

            // If fallback is enabled, merge with local secrets
            if (_options.FallbackToLocalKeyVault)
            {
                var localSecrets = _localKeyVault.GetAll();
                foreach (var secret in localSecrets)
                {
                    if (!secrets.ContainsKey(secret.Key))
                    {
                        secrets[secret.Key] = secret.Value;
                    }
                }
            }

            return secrets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all secrets from Azure Key Vault");

            // Fallback to local key vault if enabled
            if (_options.FallbackToLocalKeyVault)
            {
                _logger.LogDebug("Falling back to local key vault for all secrets");
                return _localKeyVault.GetAll();
            }

            return new Dictionary<string, string>();
        }
    }

    /// <inheritdoc />
    public KeyVaultSubstitutionResult ApplySubstitutions(IConfigurationBuilder builder)
    {
        if (ShouldUseLocalKeyVault())
        {
            _logger.LogDebug("Using local key vault to apply substitutions");
            return _localKeyVault.ApplySubstitutions(builder);
        }

        // Create a custom configuration provider that uses the hybrid key vault
        builder.Add(new HybridKeyVaultConfigurationSource(this, _options, _logger));

        // Return a placeholder result since the actual substitution happens in the configuration provider
        return new KeyVaultSubstitutionResult
        {
            TotalTokens = 0,
            SubstitutedTokens = 0,
            RedactedSecrets = 0,
            PropertyOverrides = 0,
            VaultFileFound = true
        };
    }

    /// <inheritdoc />
    public async Task<string?> GetAzureSecretAsync(string secretName)
    {
        if (_azureSecretClient == null)
        {
            _logger.LogWarning("Azure Key Vault client is not initialized");
            return null;
        }

        try
        {
            var response = await _azureSecretClient.GetSecretAsync(secretName);
            _logger.LogDebug("Retrieved secret from Azure Key Vault: {SecretName}", secretName);

            if (_options.CacheAzureKeyVaultSecrets)
            {
                _secretCache[secretName] = new CachedSecret(
                    response.Value.Value,
                    DateTime.UtcNow.AddMinutes(_options.CacheDurationMinutes));
            }

            return response.Value.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret from Azure Key Vault: {SecretName}", secretName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetAllAzureSecretsAsync()
    {
        var secrets = new Dictionary<string, string>();

        if (_azureSecretClient == null)
        {
            _logger.LogWarning("Azure Key Vault client is not initialized");
            return secrets;
        }

        try
        {
            // Get all secrets from Azure Key Vault
            var secretProperties = _azureSecretClient.GetPropertiesOfSecretsAsync();

            await foreach (var secretProperty in secretProperties)
            {
                try
                {
                    var response = await _azureSecretClient.GetSecretAsync(secretProperty.Name);
                    secrets[secretProperty.Name] = response.Value.Value;

                    if (_options.CacheAzureKeyVaultSecrets)
                    {
                        _secretCache[secretProperty.Name] = new CachedSecret(
                            response.Value.Value,
                            DateTime.UtcNow.AddMinutes(_options.CacheDurationMinutes));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get secret from Azure Key Vault: {SecretName}",
                        secretProperty.Name);
                }
            }

            _logger.LogInformation("Retrieved {Count} secrets from Azure Key Vault", secrets.Count);
            _lastCacheRefresh = DateTime.UtcNow;

            return secrets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all secrets from Azure Key Vault");
            return secrets;
        }
    }

    /// <inheritdoc />
    public async Task RefreshCacheAsync()
    {
        if (!_options.CacheAzureKeyVaultSecrets || _azureSecretClient == null)
        {
            return;
        }

        await _cacheLock.WaitAsync();
        try
        {
            _logger.LogInformation("Refreshing Azure Key Vault secrets cache");
            _secretCache.Clear();
            await GetAllAzureSecretsAsync();
            _lastCacheRefresh = DateTime.UtcNow;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task RefreshCacheIfNeeded()
    {
        if (!_options.CacheAzureKeyVaultSecrets || _azureSecretClient == null)
        {
            return;
        }

        if ((DateTime.UtcNow - _lastCacheRefresh).TotalMinutes >= _options.CacheDurationMinutes)
        {
            await RefreshCacheAsync();
        }
    }

    private bool ShouldUseLocalKeyVault()
    {
        // If explicitly set to use local key vault, return true
        if (_options.UseLocalKeyVault)
        {
            _logger.LogDebug("Using local key vault because UseLocalKeyVault is set to true");

            // Check if the local key vault file exists
            if (!string.IsNullOrEmpty(_options.LocalKeyVaultOptions.VaultFilePath) &&
                !File.Exists(_options.LocalKeyVaultOptions.VaultFilePath))
            {
                _logger.LogWarning("Local key vault file not found at path: {VaultFilePath}. " +
                                   "UseLocalKeyVault is set to true, but the file does not exist.",
                    _options.LocalKeyVaultOptions.VaultFilePath);

                // If Azure Key Vault is configured, try to use it instead
                if (!string.IsNullOrEmpty(_options.AzureKeyVaultUri) && _azureSecretClient != null)
                {
                    _logger.LogInformation(
                        "Falling back to Azure Key Vault because local key vault file was not found");
                    return false;
                }

                // If we can't fall back to Azure Key Vault, throw an exception
                throw new FileNotFoundException(
                    $"Local key vault file not found at path: {_options.LocalKeyVaultOptions.VaultFilePath}. " +
                    "UseLocalKeyVault is set to true, but the file does not exist and no Azure Key Vault fallback is available.",
                    _options.LocalKeyVaultOptions.VaultFilePath);
            }

            return true;
        }

        // If the current environment is in the list of local key vault environments, check if the file exists
        if (_options.LocalKeyVaultEnvironments.Contains(_environment.EnvironmentName, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Environment {EnvironmentName} is configured to use local key vault",
                _environment.EnvironmentName);

            // Check if the local key vault file exists
            if (!string.IsNullOrEmpty(_options.LocalKeyVaultOptions.VaultFilePath) &&
                !File.Exists(_options.LocalKeyVaultOptions.VaultFilePath))
            {
                _logger.LogWarning("Local key vault file not found at path: {VaultFilePath}. " +
                                   "Environment {EnvironmentName} is configured to use local key vault, but the file does not exist.",
                    _options.LocalKeyVaultOptions.VaultFilePath, _environment.EnvironmentName);

                // If Azure Key Vault is configured, try to use it instead
                if (!string.IsNullOrEmpty(_options.AzureKeyVaultUri) && _azureSecretClient != null)
                {
                    _logger.LogInformation(
                        "Falling back to Azure Key Vault because local key vault file was not found");
                    return false;
                }

                // If FallbackToLocalKeyVault is true but we can't find the file, log a warning
                if (_options.FallbackToLocalKeyVault)
                {
                    _logger.LogError("Cannot fall back to local key vault because the file does not exist, " +
                                     "and FallbackToLocalKeyVault is set to true");
                }

                // If we can't fall back to Azure Key Vault, throw an exception
                throw new FileNotFoundException(
                    $"Local key vault file not found at path: {_options.LocalKeyVaultOptions.VaultFilePath}. " +
                    $"Environment {_environment.EnvironmentName} is configured to use local key vault, but the file does not exist " +
                    "and no Azure Key Vault fallback is available.",
                    _options.LocalKeyVaultOptions.VaultFilePath);
            }

            return true;
        }

        // If Azure Key Vault client is not initialized and fallback is enabled, use local key vault
        if (_azureSecretClient == null && _options.FallbackToLocalKeyVault)
        {
            _logger.LogDebug("Azure Key Vault client is not initialized and FallbackToLocalKeyVault is enabled");

            // Check if the local key vault file exists
            if (!string.IsNullOrEmpty(_options.LocalKeyVaultOptions.VaultFilePath) &&
                !File.Exists(_options.LocalKeyVaultOptions.VaultFilePath))
            {
                _logger.LogError(
                    "Cannot fall back to local key vault because the file does not exist at path: {VaultFilePath}",
                    _options.LocalKeyVaultOptions.VaultFilePath);

                throw new FileNotFoundException(
                    $"Local key vault file not found at path: {_options.LocalKeyVaultOptions.VaultFilePath}. " +
                    "Azure Key Vault is not available and the local key vault file does not exist.",
                    _options.LocalKeyVaultOptions.VaultFilePath);
            }

            return true;
        }

        _logger.LogDebug("Using Azure Key Vault");
        return false;
    }

    private class CachedSecret
    {
        public string Value { get; }
        public DateTime ExpiresAt { get; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public CachedSecret(string value, DateTime expiresAt)
        {
            Value = value;
            ExpiresAt = expiresAt;
        }
    }
}