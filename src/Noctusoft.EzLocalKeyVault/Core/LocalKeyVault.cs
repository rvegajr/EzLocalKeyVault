using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Noctusoft.EzLocalKeyVault.Options;

namespace Noctusoft.EzLocalKeyVault.Core;

/// <summary>
/// Implementation of <see cref="ILocalKeyVault"/> that provides local key vault functionality.
/// </summary>
public class LocalKeyVault : ILocalKeyVault
{
    private readonly LocalKeyVaultOptions _options;
    private readonly ILogger<LocalKeyVault> _logger;
    private readonly ConcurrentDictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Regex _tokenPattern = new(@"\$\(([^)]+)\)", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalKeyVault"/> class.
    /// </summary>
    /// <param name="options">The options for the local key vault.</param>
    /// <param name="logger">The logger.</param>
    public LocalKeyVault(LocalKeyVaultOptions options, ILogger<LocalKeyVault> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadVaultFile();

        if (_options.ReloadOnChange)
        {
            SetupFileWatcher();
        }
    }

    /// <inheritdoc />
    public string? GetSecret(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        return _secrets.TryGetValue(key, out var value) ? value : null;
    }

    /// <inheritdoc />
    public IDictionary<string, string> GetAll()
    {
        return new Dictionary<string, string>(_secrets, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public KeyVaultSubstitutionResult ApplySubstitutions(IConfigurationBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var result = new KeyVaultSubstitutionResult
        {
            VaultFileFound = File.Exists(_options.VaultFilePath)
        };

        if (!result.VaultFileFound)
        {
            _logger.LogWarning("Vault file not found at {VaultFilePath}. No substitutions will be applied.",
                _options.VaultFilePath);
            return result;
        }

        // Build the configuration to get the current state
        var config = builder.Build();

        // Process token substitutions
        var totalTokens = 0;
        var substitutedTokens = 0;
        var redactedSecrets = 0;

        // Flatten the configuration to find all values with tokens
        var allSettings = FlattenConfiguration(config);

        foreach (var setting in allSettings)
        {
            var key = setting.Key;
            var value = setting.Value;

            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            var matches = _tokenPattern.Matches(value);
            if (matches.Count > 0)
            {
                totalTokens += matches.Count;

                var newValue = value;
                foreach (Match match in matches)
                {
                    var tokenName = match.Groups[1].Value;
                    if (_secrets.TryGetValue(tokenName, out var secretValue))
                    {
                        newValue = newValue.Replace(match.Value, secretValue);
                        substitutedTokens++;

                        if (_options.LogSubstitutions)
                        {
                            var logValue =
                                SecretRedactor.RedactIfSecret(tokenName, secretValue, _options.RedactionPatterns);
                            if (logValue != secretValue)
                            {
                                redactedSecrets++;
                            }

                            _logger.LogDebug("Substituted token {TokenName} in {ConfigKey}", tokenName, key);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Token {TokenName} not found in vault", tokenName);
                    }
                }

                // Add the modified value to the memory configuration source
                builder.AddInMemoryCollection(new[] { new KeyValuePair<string, string?>(key, newValue) });
            }
        }

        // Process direct property overrides
        var propertyOverrides = 0;
        foreach (var secret in _secrets)
        {
            // If the key contains a colon, it's a direct property override
            if (secret.Key.Contains(':'))
            {
                builder.AddInMemoryCollection(new[] { new KeyValuePair<string, string?>(secret.Key, secret.Value) });
                propertyOverrides++;

                if (_options.LogSubstitutions)
                {
                    var logValue = SecretRedactor.RedactIfSecret(secret.Key, secret.Value, _options.RedactionPatterns);
                    if (logValue != secret.Value)
                    {
                        redactedSecrets++;
                    }

                    _logger.LogDebug("Applied direct property override for {ConfigKey}", secret.Key);
                }
            }
        }

        _logger.LogInformation(
            "Key vault substitution complete. Total tokens: {TotalTokens}, Substituted: {SubstitutedTokens}, Property overrides: {PropertyOverrides}",
            totalTokens,
            substitutedTokens,
            propertyOverrides);

        return new KeyVaultSubstitutionResult
        {
            TotalTokens = totalTokens,
            SubstitutedTokens = substitutedTokens,
            RedactedSecrets = redactedSecrets,
            PropertyOverrides = propertyOverrides,
            VaultFileFound = true
        };
    }

    private void LoadVaultFile()
    {
        if (!File.Exists(_options.VaultFilePath))
        {
            _logger.LogWarning("Vault file not found at {VaultFilePath}", _options.VaultFilePath);
            return;
        }

        try
        {
            var json = File.ReadAllText(_options.VaultFilePath);
            var vaultData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (vaultData == null)
            {
                _logger.LogError("Failed to deserialize vault file {VaultFilePath}", _options.VaultFilePath);
                return;
            }

            _secrets.Clear();

            foreach (var item in vaultData)
            {
                var key = item.Key;
                var value = item.Value.ValueKind == JsonValueKind.String
                    ? item.Value.GetString()
                    : item.Value.ToString();

                if (value != null)
                {
                    _secrets[key] = value;

                    if (_options.LogSubstitutions)
                    {
                        var logValue = SecretRedactor.RedactIfSecret(key, value, _options.RedactionPatterns);
                        _logger.LogDebug("Loaded secret {Key}: {Value}", key, logValue);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} secrets from vault file {VaultFilePath}", _secrets.Count,
                _options.VaultFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vault file {VaultFilePath}", _options.VaultFilePath);
        }
    }

    private void SetupFileWatcher()
    {
        var directory = Path.GetDirectoryName(_options.VaultFilePath);
        var fileName = Path.GetFileName(_options.VaultFilePath);

        if (string.IsNullOrEmpty(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Directory {Directory} does not exist. File watcher will not be set up.", directory);
            return;
        }

        var watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };

        watcher.Changed += (sender, args) =>
        {
            _logger.LogInformation("Vault file {VaultFilePath} changed. Reloading...", _options.VaultFilePath);

            // Add a small delay to ensure the file is not locked
            Thread.Sleep(100);
            LoadVaultFile();
        };

        watcher.EnableRaisingEvents = true;

        _logger.LogDebug("File watcher set up for {VaultFilePath}", _options.VaultFilePath);
    }

    private static Dictionary<string, string> FlattenConfiguration(IConfiguration configuration)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void VisitSection(IConfiguration section, string prefix)
        {
            foreach (var child in section.GetChildren())
            {
                var path = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}:{child.Key}";

                if (child.Value != null)
                {
                    result[path] = child.Value;
                }

                VisitSection(child, path);
            }
        }

        VisitSection(configuration, string.Empty);
        return result;
    }
}