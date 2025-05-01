using Microsoft.Extensions.Configuration;

namespace Noctusoft.EzLocalKeyVault.Core;

/// <summary>
/// Interface for a local key vault that provides secret management and configuration substitution.
/// </summary>
public interface ILocalKeyVault
{
    /// <summary>
    /// Gets a secret from the vault by its key.
    /// </summary>
    /// <param name="key">The key of the secret to retrieve.</param>
    /// <returns>The secret value, or null if the key is not found.</returns>
    string? GetSecret(string key);

    /// <summary>
    /// Gets all secrets from the vault.
    /// </summary>
    /// <returns>A dictionary of all secrets in the vault.</returns>
    IDictionary<string, string> GetAll();

    /// <summary>
    /// Applies substitutions to the configuration using values from the vault.
    /// </summary>
    /// <param name="builder">The configuration builder to apply substitutions to.</param>
    /// <returns>A result object containing information about the substitution operation.</returns>
    KeyVaultSubstitutionResult ApplySubstitutions(IConfigurationBuilder builder);
}