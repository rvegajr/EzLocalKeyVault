namespace Noctusoft.EzLocalKeyVault.Core;

/// <summary>
/// Represents the result of a key vault substitution operation.
/// </summary>
public class KeyVaultSubstitutionResult
{
    /// <summary>
    /// Gets the total number of tokens found in the configuration.
    /// </summary>
    public int TotalTokens { get; init; }

    /// <summary>
    /// Gets the number of tokens that were successfully substituted.
    /// </summary>
    public int SubstitutedTokens { get; init; }

    /// <summary>
    /// Gets the number of tokens that were redacted in logs.
    /// </summary>
    public int RedactedSecrets { get; init; }

    /// <summary>
    /// Gets the number of direct property overrides applied.
    /// </summary>
    public int PropertyOverrides { get; init; }

    /// <summary>
    /// Gets a value indicating whether the vault file was found.
    /// </summary>
    public bool VaultFileFound { get; init; }
}