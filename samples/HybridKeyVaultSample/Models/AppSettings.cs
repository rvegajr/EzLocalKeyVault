namespace HybridKeyVaultSample.Models;

/// <summary>
/// Represents the application settings after key vault substitutions have been applied.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the service URL.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the connection strings.
    /// </summary>
    public ConnectionStrings ConnectionStrings { get; set; } = new();

    /// <summary>
    /// Gets or sets the hybrid key vault options.
    /// </summary>
    public HybridKeyVaultOptions EzAzureLocalKeyVault { get; set; } = new();
}

/// <summary>
/// Represents the connection strings in the application settings.
/// </summary>
public class ConnectionStrings
{
    /// <summary>
    /// Gets or sets the default connection string.
    /// </summary>
    public string? DefaultConnection { get; set; }
}

/// <summary>
/// Represents the hybrid key vault options in the application settings.
/// </summary>
public class HybridKeyVaultOptions
{
    /// <summary>
    /// Gets or sets the Azure Key Vault URI.
    /// </summary>
    public string? AzureKeyVaultUri { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use local key vault.
    /// </summary>
    public bool UseLocalKeyVault { get; set; }

    /// <summary>
    /// Gets or sets the list of environment names that should use local key vault.
    /// </summary>
    public List<string> LocalKeyVaultEnvironments { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to fallback to local key vault if Azure Key Vault is unavailable.
    /// </summary>
    public bool FallbackToLocalKeyVault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to cache Azure Key Vault secrets.
    /// </summary>
    public bool CacheAzureKeyVaultSecrets { get; set; }

    /// <summary>
    /// Gets or sets the cache duration in minutes.
    /// </summary>
    public int CacheDurationMinutes { get; set; }

    /// <summary>
    /// Gets or sets the Azure Key Vault prefix.
    /// </summary>
    public string? AzureKeyVaultPrefix { get; set; }

    /// <summary>
    /// Gets or sets the local key vault options.
    /// </summary>
    public LocalKeyVaultOptions LocalKeyVaultOptions { get; set; } = new();
}

/// <summary>
/// Represents the local key vault options in the application settings.
/// </summary>
public class LocalKeyVaultOptions
{
    /// <summary>
    /// Gets or sets the vault file path.
    /// </summary>
    public string? VaultFilePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to reload on change.
    /// </summary>
    public bool ReloadOnChange { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to log substitutions.
    /// </summary>
    public bool LogSubstitutions { get; set; }
}
