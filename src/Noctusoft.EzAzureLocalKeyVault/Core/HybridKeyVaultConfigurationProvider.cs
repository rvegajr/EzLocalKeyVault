using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Noctusoft.EzAzureLocalKeyVault.Options;
using System.Collections.Generic; // Added this line

namespace Noctusoft.EzAzureLocalKeyVault.Core;

/// <summary>
/// Configuration provider for the hybrid key vault.
/// </summary>
public class HybridKeyVaultConfigurationProvider : ConfigurationProvider
{
    private readonly IHybridKeyVault _hybridKeyVault;
    private readonly EzAzureLocalKeyVaultOptions _options;
    private readonly ILogger _logger;
    private static readonly Regex _variablePattern = new(@"\$\(([^)]+)\)", RegexOptions.Compiled);
    private int _propertyOverrides = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridKeyVaultConfigurationProvider"/> class.
    /// </summary>
    /// <param name="hybridKeyVault">The hybrid key vault.</param>
    /// <param name="options">The options for configuring the hybrid key vault.</param>
    /// <param name="logger">The logger.</param>
    public HybridKeyVaultConfigurationProvider(
        IHybridKeyVault hybridKeyVault,
        EzAzureLocalKeyVaultOptions options,
        ILogger logger)
    {
        _hybridKeyVault = hybridKeyVault;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public override void Load()
    {
        // Get all secrets from the hybrid key vault
        var secrets = _hybridKeyVault.GetAll();

        // Add all secrets as property overrides
        foreach (var secret in secrets)
        {
            if (secret.Key.Contains(':'))
            {
                // This is a property override
                Data[secret.Key] = secret.Value;
                _propertyOverrides++;
            }
        }

        // Apply variable substitutions to the configuration
        ApplySubstitutions();
    }

    private void ApplySubstitutions()
    {
        var substitutionCount = 0;
        var totalTokens = 0;

        // Create a copy of the data to avoid modifying the collection during iteration
        var dataCopy = new Dictionary<string, string?>(Data);

        foreach (var item in dataCopy)
        {
            if (item.Value != null)
            {
                var matches = _variablePattern.Matches(item.Value);
                if (matches.Count > 0)
                {
                    totalTokens += matches.Count;
                    var newValue = item.Value;

                    foreach (Match match in matches)
                    {
                        var variableName = match.Groups[1].Value;
                        var secretValue = _hybridKeyVault.GetSecret(variableName);

                        if (secretValue != null)
                        {
                            newValue = newValue.Replace(match.Value, secretValue);
                            substitutionCount++;

                            if (_options.LocalKeyVaultOptions.LogSubstitutions)
                            {
                                _logger.LogDebug("Substituted token {Token} in {Key}", variableName, item.Key);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Token {Token} not found in vault", variableName);
                        }
                    }

                    Data[item.Key] = newValue;
                }
            }
        }

        _logger.LogInformation(
            "Key vault substitution complete. Total tokens: {TotalTokens}, Substituted: {SubstitutionCount}, Property overrides: {PropertyOverrides}",
            totalTokens, substitutionCount, _propertyOverrides);
    }
}