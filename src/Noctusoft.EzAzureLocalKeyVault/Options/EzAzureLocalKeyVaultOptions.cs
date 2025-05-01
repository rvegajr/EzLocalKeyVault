using Noctusoft.EzLocalKeyVault.Options;

namespace Noctusoft.EzAzureLocalKeyVault.Options;

/// <summary>
/// Options for configuring the hybrid key vault service.
/// </summary>
/// <remarks>
/// <para>
/// This class provides configuration options for the hybrid key vault service, which can automatically
/// switch between Azure Key Vault and local key vault based on the environment and configuration.
/// </para>
/// <para>
/// Configuration example in appsettings.json:
/// <code>
/// {
///   "EzAzureLocalKeyVault": {
///     "AzureKeyVaultUri": "https://your-key-vault.vault.azure.net/",
///     "UseLocalKeyVault": false,
///     "LocalKeyVaultEnvironments": ["Development", "LocalDevelopment"],
///     "FallbackToLocalKeyVault": true,
///     "CacheAzureKeyVaultSecrets": true,
///     "CacheDurationMinutes": 60,
///     "AzureKeyVaultPrefix": "AZURE_KEY_VAULT:",
///     "LocalKeyVaultOptions": {
///       "VaultFilePath": ".local-vault.json",
///       "ReloadOnChange": true,
///       "LogSubstitutions": true
///     }
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public class EzAzureLocalKeyVaultOptions
{
    /// <summary>
    /// Gets or sets the URI of the Azure Key Vault.
    /// </summary>
    /// <value>
    /// The URI of the Azure Key Vault, e.g., "https://your-key-vault.vault.azure.net/".
    /// </value>
    /// <remarks>
    /// <para>
    /// This property is required when using Azure Key Vault. If not provided, the hybrid key vault
    /// will only use the local key vault.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// options.AzureKeyVaultUri = "https://your-key-vault.vault.azure.net/";
    /// </code>
    /// </para>
    /// </remarks>
    public string? AzureKeyVaultUri { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use local key vault regardless of the environment.
    /// </summary>
    /// <value>
    /// <c>true</c> to always use local key vault; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When set to <c>true</c>, the hybrid key vault will always use the local key vault, regardless of the
    /// current environment or other settings. This is useful for local development or testing.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// // Force using local key vault
    /// options.UseLocalKeyVault = true;
    /// </code>
    /// </para>
    /// </remarks>
    public bool UseLocalKeyVault { get; set; }

    /// <summary>
    /// Gets or sets the list of environment names that should use local key vault.
    /// </summary>
    /// <value>
    /// A list of environment names that should use local key vault.
    /// </value>
    /// <remarks>
    /// <para>
    /// If the current environment name is in this list, the hybrid key vault will use the local key vault.
    /// This is useful for automatically switching between local key vault and Azure Key Vault based on the environment.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// options.LocalKeyVaultEnvironments = new List&lt;string&gt; { "Development", "LocalDevelopment", "Testing" };
    /// </code>
    /// </para>
    /// </remarks>
    public List<string> LocalKeyVaultEnvironments { get; set; } = new() { "Development", "LocalDevelopment" };

    /// <summary>
    /// Gets or sets a value indicating whether to fallback to local key vault if Azure Key Vault is unavailable.
    /// </summary>
    /// <value>
    /// <c>true</c> to fallback to local key vault if Azure Key Vault is unavailable; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When set to <c>true</c>, the hybrid key vault will fallback to the local key vault if Azure Key Vault
    /// is unavailable or if a secret is not found in Azure Key Vault. This is useful for ensuring that the
    /// application can still function even if Azure Key Vault is temporarily unavailable.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// // Enable fallback to local key vault
    /// options.FallbackToLocalKeyVault = true;
    /// </code>
    /// </para>
    /// </remarks>
    public bool FallbackToLocalKeyVault { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to cache Azure Key Vault secrets locally.
    /// </summary>
    /// <value>
    /// <c>true</c> to cache Azure Key Vault secrets locally; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When set to <c>true</c>, the hybrid key vault will cache Azure Key Vault secrets locally for the
    /// duration specified by <see cref="CacheDurationMinutes"/>. This can improve performance by reducing
    /// the number of requests to Azure Key Vault.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// // Enable caching of Azure Key Vault secrets
    /// options.CacheAzureKeyVaultSecrets = true;
    /// options.CacheDurationMinutes = 30; // Cache for 30 minutes
    /// </code>
    /// </para>
    /// </remarks>
    public bool CacheAzureKeyVaultSecrets { get; set; } = true;

    /// <summary>
    /// Gets or sets the duration in minutes to cache Azure Key Vault secrets.
    /// </summary>
    /// <value>
    /// The duration in minutes to cache Azure Key Vault secrets.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property specifies how long Azure Key Vault secrets should be cached locally when
    /// <see cref="CacheAzureKeyVaultSecrets"/> is set to <c>true</c>. After this duration, the
    /// cache will be refreshed the next time a secret is requested.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// // Cache secrets for 15 minutes
    /// options.CacheDurationMinutes = 15;
    /// </code>
    /// </para>
    /// </remarks>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the prefix for Azure Key Vault secret references.
    /// </summary>
    /// <value>
    /// The prefix for Azure Key Vault secret references.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property specifies the prefix that should be used to explicitly reference secrets in Azure Key Vault.
    /// For example, if the prefix is "AZURE_KEY_VAULT:", then "$(AZURE_KEY_VAULT:SecretName)" will retrieve
    /// the secret named "SecretName" directly from Azure Key Vault, bypassing any local key vault.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// // Set the prefix for Azure Key Vault secret references
    /// options.AzureKeyVaultPrefix = "AKV:";
    /// 
    /// // Then in configuration:
    /// // "ApiKey": "$(AKV:ApiKey)"
    /// </code>
    /// </para>
    /// </remarks>
    public string AzureKeyVaultPrefix { get; set; } = "AZURE_KEY_VAULT:";

    /// <summary>
    /// Gets or sets the options for the local key vault.
    /// </summary>
    /// <value>
    /// The options for the local key vault.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property specifies the options for the local key vault, such as the vault file path,
    /// whether to reload the vault file when it changes, and whether to log substitutions.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// options.LocalKeyVaultOptions = new LocalKeyVaultOptions
    /// {
    ///     VaultFilePath = ".local-vault.development.json",
    ///     ReloadOnChange = true,
    ///     LogSubstitutions = true
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    public LocalKeyVaultOptions LocalKeyVaultOptions { get; set; } = new();
}