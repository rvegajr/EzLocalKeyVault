using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Noctusoft.EzLocalKeyVault.Core;
using Noctusoft.EzLocalKeyVault.Diagnostics;
using System.Text;

namespace Noctusoft.EzLocalKeyVault.Extensions;

/// <summary>
/// Extension methods for diagnostics and troubleshooting EzLocalKeyVault.
/// </summary>
public static class DiagnosticsExtensions
{
    /// <summary>
    /// Generates a diagnostic report for the EzLocalKeyVault configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">Optional logger for output.</param>
    /// <returns>A diagnostic report as a string.</returns>
    public static string GenerateKeyVaultDiagnosticReport(this IConfiguration configuration, ILogger? logger = null)
    {
        return KeyVaultDiagnostics.GenerateDiagnosticReport(configuration, logger);
    }

    /// <summary>
    /// Analyzes the configuration for substitution issues and provides recommendations.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">Optional logger for output.</param>
    /// <returns>A list of issues and recommendations.</returns>
    public static List<string> AnalyzeKeyVaultSubstitutionIssues(this IServiceProvider serviceProvider,
        ILogger? logger = null)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var keyVault = serviceProvider.GetRequiredService<ILocalKeyVault>();

        return KeyVaultDiagnostics.AnalyzeSubstitutionIssues(configuration, keyVault, logger);
    }

    /// <summary>
    /// Exports the current configuration to a JSON file for inspection.
    /// </summary>
    /// <param name="configuration">The configuration to export.</param>
    /// <param name="outputPath">The output file path.</param>
    /// <param name="maskSensitiveValues">Whether to mask sensitive values.</param>
    /// <param name="logger">Optional logger for output.</param>
    public static void ExportConfigurationToJson(this IConfiguration configuration, string outputPath,
        bool maskSensitiveValues = true, ILogger? logger = null)
    {
        KeyVaultDiagnostics.ExportConfigurationToJson(configuration, outputPath, maskSensitiveValues, logger);
    }

    /// <summary>
    /// Adds diagnostic endpoints to the application for troubleshooting EzLocalKeyVault.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="pathPrefix">The path prefix for the diagnostic endpoints. Defaults to "/_diagnostics/keyvault".</param>
    /// <returns>The web application.</returns>
    public static IApplicationBuilder UseEzLocalKeyVaultDiagnostics(this IApplicationBuilder app,
        string pathPrefix = "/_diagnostics/keyvault")
    {
        app.Map(pathPrefix, builder =>
        {
            // Report endpoint
            builder.Map("/report", appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
                    var loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("EzLocalKeyVault.Diagnostics");

                    var report = configuration.GenerateKeyVaultDiagnosticReport(logger);

                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(report);
                });
            });

            // Issues endpoint
            builder.Map("/issues", appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    var serviceProvider = context.RequestServices;
                    var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("EzLocalKeyVault.Diagnostics");

                    var issues = serviceProvider.AnalyzeKeyVaultSubstitutionIssues(logger);

                    context.Response.ContentType = "text/plain";

                    if (issues.Count == 0)
                    {
                        await context.Response.WriteAsync("No substitution issues found.");
                    }
                    else
                    {
                        await context.Response.WriteAsync($"Found {issues.Count} substitution issues:\n\n");
                        foreach (var issue in issues)
                        {
                            await context.Response.WriteAsync($"- {issue}\n");
                        }
                    }
                });
            });

            // Export endpoint
            builder.Map("/export", appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
                    var loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("EzLocalKeyVault.Diagnostics");

                    var maskSensitive = true;
                    if (context.Request.Query.TryGetValue("mask", out var maskValues) &&
                        maskValues.Count > 0 &&
                        bool.TryParse(maskValues[0], out var maskValue))
                    {
                        maskSensitive = maskValue;
                    }

                    var tempFile = Path.GetTempFileName();
                    try
                    {
                        configuration.ExportConfigurationToJson(tempFile, maskSensitive, logger);

                        var json = await File.ReadAllTextAsync(tempFile);

                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(json);
                    }
                    finally
                    {
                        File.Delete(tempFile);
                    }
                });
            });
        });

        return app;
    }
}