using Noctusoft.EzLocalKeyVault.Core;

namespace Noctusoft.EzAzureLocalKeyVault.Core;

/// <summary>
/// Defines the contract for a hybrid key vault service that can use either Azure Key Vault or local key vault.
/// </summary>
/// <remarks>
/// <para>
/// The hybrid key vault service provides a unified approach to managing secrets by automatically switching 
/// between local key vault and Azure Key Vault based on the environment and configuration.
/// </para>
/// <para>
/// Usage example:
/// <code>
/// // Inject the service
/// public class MyService
/// {
///     private readonly IHybridKeyVault _keyVault;
///     
///     public MyService(IHybridKeyVault keyVault)
///     {
///         _keyVault = keyVault;
///     }
///     
///     public void DoSomething()
///     {
///         // Get a secret
///         var secret = _keyVault.GetSecret("my-secret");
///         
///         // Check which vault is being used
///         if (_keyVault.IsUsingLocalKeyVault)
///         {
///             Console.WriteLine("Using local key vault");
///         }
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public interface IHybridKeyVault : ILocalKeyVault
{
    /// <summary>
    /// Gets a value indicating whether the service is currently using the local key vault.
    /// </summary>
    /// <value>
    /// <c>true</c> if the service is using the local key vault; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This property can be used to determine which key vault implementation is currently active.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (keyVault.IsUsingLocalKeyVault)
    /// {
    ///     Console.WriteLine("Using local key vault");
    /// }
    /// </code>
    /// </example>
    bool IsUsingLocalKeyVault { get; }

    /// <summary>
    /// Gets a value indicating whether the service is currently using Azure Key Vault.
    /// </summary>
    /// <value>
    /// <c>true</c> if the service is using Azure Key Vault; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This property can be used to determine which key vault implementation is currently active.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (keyVault.IsUsingAzureKeyVault)
    /// {
    ///     Console.WriteLine("Using Azure Key Vault");
    /// }
    /// </code>
    /// </example>
    bool IsUsingAzureKeyVault { get; }

    /// <summary>
    /// Gets a secret from Azure Key Vault directly, bypassing any local key vault fallback.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve from Azure Key Vault.</param>
    /// <returns>
    /// The secret value if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method retrieves a secret directly from Azure Key Vault, regardless of the current vault mode.
    /// It will not fallback to the local key vault if the secret is not found or if Azure Key Vault is unavailable.
    /// </para>
    /// <para>
    /// If caching is enabled, the secret will be cached for the configured duration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get a secret directly from Azure Key Vault
    /// var secret = await keyVault.GetAzureSecretAsync("my-azure-secret");
    /// if (secret != null)
    /// {
    ///     Console.WriteLine($"Secret value: {secret}");
    /// }
    /// </code>
    /// </example>
    Task<string?> GetAzureSecretAsync(string secretName);

    /// <summary>
    /// Gets all secrets from Azure Key Vault directly, bypassing any local key vault fallback.
    /// </summary>
    /// <returns>
    /// A dictionary containing all secrets from Azure Key Vault.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method retrieves all secrets directly from Azure Key Vault, regardless of the current vault mode.
    /// It will not fallback to the local key vault if Azure Key Vault is unavailable.
    /// </para>
    /// <para>
    /// If caching is enabled, the secrets will be cached for the configured duration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get all secrets from Azure Key Vault
    /// var secrets = await keyVault.GetAllAzureSecretsAsync();
    /// foreach (var secret in secrets)
    /// {
    ///     Console.WriteLine($"Secret: {secret.Key} = {secret.Value}");
    /// }
    /// </code>
    /// </example>
    Task<Dictionary<string, string>> GetAllAzureSecretsAsync();

    /// <summary>
    /// Refreshes the cache of Azure Key Vault secrets.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method clears the current cache and retrieves all secrets from Azure Key Vault again.
    /// If caching is disabled or if Azure Key Vault is not configured, this method does nothing.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Refresh the cache of Azure Key Vault secrets
    /// await keyVault.RefreshCacheAsync();
    /// Console.WriteLine("Cache refreshed successfully");
    /// </code>
    /// </example>
    Task RefreshCacheAsync();
}