namespace Noctusoft.EzLocalKeyVault.Options;

/// <summary>
/// Configuration options for the local key vault.
/// </summary>
public class LocalKeyVaultOptions
{
    /// <summary>
    /// Gets or sets the path to the local vault file.
    /// </summary>
    /// <remarks>
    /// Default value is ".local-vault.json".
    /// </remarks>
    public string VaultFilePath { get; set; } = ".local-vault.json";

    /// <summary>
    /// Gets or sets a value indicating whether the vault file should be reloaded when it changes.
    /// </summary>
    /// <remarks>
    /// Default value is true.
    /// </remarks>
    public bool ReloadOnChange { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether substitutions should be logged.
    /// </summary>
    /// <remarks>
    /// Default value is true.
    /// </remarks>
    public bool LogSubstitutions { get; set; } = true;

    /// <summary>
    /// Gets or sets the patterns used to identify values that should be redacted in logs.
    /// </summary>
    /// <remarks>
    /// Default patterns are "password", "secret", "key", and "token".
    /// </remarks>
    public string[] RedactionPatterns { get; set; } = { "password", "secret", "key", "token" };
}