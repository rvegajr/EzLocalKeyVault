using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Noctusoft.EzAzureLocalKeyVault.Core;
using Noctusoft.EzAzureLocalKeyVault.Options;
using System.Text;

namespace Noctusoft.EzAzureLocalKeyVault.Extensions;

/// <summary>
/// Extension methods for diagnostics and troubleshooting the hybrid key vault integration.
/// </summary>
/// <remarks>
/// <para>
/// This class provides extension methods for generating diagnostic reports and testing the connection
/// to Azure Key Vault. These methods are useful for troubleshooting issues with the hybrid key vault
/// integration.
/// </para>
/// </remarks>
public static class DiagnosticsExtensions
{
    /// <summary>
    /// Generates a diagnostic report for the hybrid key vault configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">Optional logger for output.</param>
    /// <returns>A diagnostic report as a string.</returns>
    /// <remarks>
    /// <para>
    /// This method generates a comprehensive diagnostic report for the hybrid key vault configuration,
    /// including the current environment, configuration settings, and the status of the Azure Key Vault
    /// connection.
    /// </para>
    /// <para>
    /// The report includes:
    /// <list type="bullet">
    /// <item><description>Current environment information</description></item>
    /// <item><description>Hybrid key vault configuration settings</description></item>
    /// <item><description>Local key vault configuration settings</description></item>
    /// <item><description>Azure Key Vault connection status</description></item>
    /// <item><description>Which key vault is currently being used</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a controller or service
    /// public class DiagnosticsController : ControllerBase
    /// {
    ///     private readonly IConfiguration _configuration;
    ///     
    ///     public DiagnosticsController(IConfiguration configuration)
    ///     {
    ///         _configuration = configuration;
    ///     }
    ///     
    ///     [HttpGet("/diagnostics")]
    ///     public IActionResult GetDiagnostics()
    ///     {
    ///         var report = _configuration.GenerateHybridKeyVaultDiagnosticReport();
    ///         return Content(report, "text/plain");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static string GenerateHybridKeyVaultDiagnosticReport(this IConfiguration configuration,
        ILogger? logger = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== EzAzureLocalKeyVault Diagnostic Report ===");
        sb.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // Get options
        var options = new EzAzureLocalKeyVaultOptions();
        configuration.GetSection("EzAzureLocalKeyVault").Bind(options);

        sb.AppendLine("== Configuration Options ==");
        sb.AppendLine($"Azure Key Vault URI: {options.AzureKeyVaultUri ?? "Not configured"}");
        sb.AppendLine($"Use Local Key Vault: {options.UseLocalKeyVault}");
        sb.AppendLine($"Local Key Vault Environments: {string.Join(", ", options.LocalKeyVaultEnvironments)}");
        sb.AppendLine($"Fallback To Local Key Vault: {options.FallbackToLocalKeyVault}");
        sb.AppendLine($"Cache Azure Key Vault Secrets: {options.CacheAzureKeyVaultSecrets}");
        sb.AppendLine($"Cache Duration Minutes: {options.CacheDurationMinutes}");
        sb.AppendLine($"Azure Key Vault Prefix: {options.AzureKeyVaultPrefix}");
        sb.AppendLine();

        sb.AppendLine("== Local Key Vault Options ==");
        sb.AppendLine($"Vault File Path: {options.LocalKeyVaultOptions.VaultFilePath}");
        sb.AppendLine($"Reload On Change: {options.LocalKeyVaultOptions.ReloadOnChange}");
        sb.AppendLine($"Log Substitutions: {options.LocalKeyVaultOptions.LogSubstitutions}");
        sb.AppendLine(
            $"Redaction Patterns: {string.Join(", ", options.LocalKeyVaultOptions.RedactionPatterns ?? Array.Empty<string>())}");
        sb.AppendLine();

        // Check environment
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ??
                              configuration["DOTNET_ENVIRONMENT"] ?? "Production";
        sb.AppendLine("== Environment Information ==");
        sb.AppendLine($"Current Environment: {environmentName}");
        sb.AppendLine(
            $"Using Local Key Vault: {options.LocalKeyVaultEnvironments.Contains(environmentName, StringComparer.OrdinalIgnoreCase) || options.UseLocalKeyVault}");
        sb.AppendLine();

        // Check local vault file
        sb.AppendLine("== Local Vault File Status ==");
        var vaultFilePath = options.LocalKeyVaultOptions.VaultFilePath ?? ".local-vault.json";
        var vaultFileExists = File.Exists(vaultFilePath);
        sb.AppendLine($"Vault File Exists: {vaultFileExists}");

        if (vaultFileExists)
        {
            try
            {
                var vaultContent = File.ReadAllText(vaultFilePath);
                var isValidJson = IsValidJson(vaultContent);
                sb.AppendLine($"Valid JSON: {isValidJson}");

                if (isValidJson)
                {
                    var secretCount = System.Text.Json.JsonDocument.Parse(vaultContent).RootElement.EnumerateObject()
                        .Count();
                    sb.AppendLine($"Secret Count: {secretCount}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error reading vault file: {ex.Message}");
            }
        }

        sb.AppendLine();

        // Azure Key Vault connectivity check
        sb.AppendLine("== Azure Key Vault Status ==");
        if (string.IsNullOrEmpty(options.AzureKeyVaultUri))
        {
            sb.AppendLine("Azure Key Vault URI not configured");
        }
        else
        {
            sb.AppendLine($"Azure Key Vault URI: {options.AzureKeyVaultUri}");

            try
            {
                // Create a temporary service provider to get the hybrid key vault
                var services = new ServiceCollection();
                services.AddSingleton(configuration);
                services.AddLogging();
                services.AddEzAzureLocalKeyVault(configuration);
                var serviceProvider = services.BuildServiceProvider();

                var hybridKeyVault = serviceProvider.GetRequiredService<IHybridKeyVault>();

                if (hybridKeyVault.IsUsingAzureKeyVault)
                {
                    sb.AppendLine("Currently using Azure Key Vault");

                    try
                    {
                        var secrets = hybridKeyVault.GetAllAzureSecretsAsync().GetAwaiter().GetResult();
                        sb.AppendLine($"Successfully connected to Azure Key Vault");
                        sb.AppendLine($"Secret Count: {secrets.Count}");
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"Error connecting to Azure Key Vault: {ex.Message}");
                    }
                }
                else
                {
                    sb.AppendLine("Currently using Local Key Vault instead of Azure Key Vault");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error creating hybrid key vault: {ex.Message}");
            }
        }

        logger?.LogInformation("EzAzureLocalKeyVault diagnostic report generated");

        return sb.ToString();
    }

    /// <summary>
    /// Tests the connection to Azure Key Vault and returns a diagnostic report.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A diagnostic report as a string.</returns>
    /// <remarks>
    /// <para>
    /// This method tests the connection to Azure Key Vault and returns a diagnostic report with the results.
    /// It attempts to retrieve a list of secrets from Azure Key Vault and reports any errors that occur.
    /// </para>
    /// <para>
    /// The report includes:
    /// <list type="bullet">
    /// <item><description>Azure Key Vault URI</description></item>
    /// <item><description>Connection status</description></item>
    /// <item><description>Number of secrets retrieved (if successful)</description></item>
    /// <item><description>Error details (if the connection fails)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a controller or service
    /// public class DiagnosticsController : ControllerBase
    /// {
    ///     private readonly IServiceProvider _serviceProvider;
    ///     
    ///     public DiagnosticsController(IServiceProvider serviceProvider)
    ///     {
    ///         _serviceProvider = serviceProvider;
    ///     }
    ///     
    ///     [HttpGet("/test-azure-connection")]
    ///     public async Task&lt;IActionResult&gt; TestAzureConnection()
    ///     {
    ///         var report = await _serviceProvider.TestAzureKeyVaultConnection();
    ///         return Content(report, "text/plain");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async Task<string> TestAzureKeyVaultConnection(this IServiceProvider serviceProvider)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Azure Key Vault Connection Test ===");
        sb.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        try
        {
            var hybridKeyVault = serviceProvider.GetRequiredService<IHybridKeyVault>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // Get options
            var options = new EzAzureLocalKeyVaultOptions();
            configuration.GetSection("EzAzureLocalKeyVault").Bind(options);

            sb.AppendLine($"Azure Key Vault URI: {options.AzureKeyVaultUri ?? "Not configured"}");
            sb.AppendLine(
                $"Current Mode: {(hybridKeyVault.IsUsingAzureKeyVault ? "Azure Key Vault" : "Local Key Vault")}");
            sb.AppendLine();

            if (string.IsNullOrEmpty(options.AzureKeyVaultUri))
            {
                sb.AppendLine("Azure Key Vault URI not configured");
                return sb.ToString();
            }

            if (!hybridKeyVault.IsUsingAzureKeyVault)
            {
                sb.AppendLine("Currently using Local Key Vault instead of Azure Key Vault");
                sb.AppendLine("Attempting to connect to Azure Key Vault directly...");
                sb.AppendLine();
            }

            try
            {
                var startTime = DateTime.UtcNow;
                var secrets = await hybridKeyVault.GetAllAzureSecretsAsync();
                var duration = DateTime.UtcNow - startTime;

                sb.AppendLine("Connection successful!");
                sb.AppendLine($"Retrieved {secrets.Count} secrets");
                sb.AppendLine($"Connection time: {duration.TotalMilliseconds:F2} ms");

                // List a few secret names (without values)
                if (secrets.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Secret names (up to 5):");
                    foreach (var secretName in secrets.Keys.Take(5))
                    {
                        sb.AppendLine($"- {secretName}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Connection failed!");
                sb.AppendLine($"Error: {ex.Message}");

                if (ex.InnerException != null)
                {
                    sb.AppendLine($"Inner error: {ex.InnerException.Message}");
                }

                sb.AppendLine();
                sb.AppendLine("Troubleshooting tips:");
                sb.AppendLine("1. Verify that the Azure Key Vault URI is correct");
                sb.AppendLine("2. Check that the application has the necessary permissions to access the key vault");
                sb.AppendLine("3. Ensure that the DefaultAzureCredential is properly configured");
                sb.AppendLine("4. Check network connectivity to Azure Key Vault");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"Error getting hybrid key vault service: {ex.Message}");
        }

        return sb.ToString();
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}