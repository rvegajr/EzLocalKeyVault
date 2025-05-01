using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Noctusoft.EzAzureLocalKeyVault.Options;

namespace Noctusoft.EzAzureLocalKeyVault.Core;

/// <summary>
/// Configuration source for the hybrid key vault.
/// </summary>
public class HybridKeyVaultConfigurationSource : IConfigurationSource
{
    private readonly IHybridKeyVault _hybridKeyVault;
    private readonly EzAzureLocalKeyVaultOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridKeyVaultConfigurationSource"/> class.
    /// </summary>
    /// <param name="hybridKeyVault">The hybrid key vault.</param>
    /// <param name="options">The options for configuring the hybrid key vault.</param>
    /// <param name="logger">The logger.</param>
    public HybridKeyVaultConfigurationSource(
        IHybridKeyVault hybridKeyVault,
        EzAzureLocalKeyVaultOptions options,
        ILogger logger)
    {
        _hybridKeyVault = hybridKeyVault;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new HybridKeyVaultConfigurationProvider(_hybridKeyVault, _options, _logger);
    }
}