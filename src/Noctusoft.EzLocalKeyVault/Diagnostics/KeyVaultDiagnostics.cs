using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Noctusoft.EzLocalKeyVault.Core;
using Noctusoft.EzLocalKeyVault.Options;

namespace Noctusoft.EzLocalKeyVault.Diagnostics;

/// <summary>
/// Provides diagnostic utilities for troubleshooting EzLocalKeyVault configuration issues.
/// </summary>
public static class KeyVaultDiagnostics
{
    /// <summary>
    /// Generates a detailed diagnostic report for the local key vault configuration.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">Optional logger for output.</param>
    /// <returns>A diagnostic report as a string.</returns>
    public static string GenerateDiagnosticReport(IConfiguration configuration, ILogger? logger = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== EzLocalKeyVault Diagnostic Report ===");
        sb.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // Get options
        var options = new LocalKeyVaultOptions();
        configuration.GetSection("EzLocalKeyVault").Bind(options);

        sb.AppendLine("== Configuration Options ==");
        sb.AppendLine($"Vault File Path: {options.VaultFilePath}");
        sb.AppendLine($"Reload On Change: {options.ReloadOnChange}");
        sb.AppendLine($"Log Substitutions: {options.LogSubstitutions}");
        sb.AppendLine($"Redaction Patterns: {string.Join(", ", options.RedactionPatterns ?? Array.Empty<string>())}");
        sb.AppendLine();

        // Check vault file
        sb.AppendLine("== Vault File Status ==");
        var vaultFilePath = options.VaultFilePath ?? ".local-vault.json";
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
                    var secretCount = JsonDocument.Parse(vaultContent).RootElement.EnumerateObject().Count();
                    sb.AppendLine($"Secret Count: {secretCount}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error reading vault file: {ex.Message}");
            }
        }

        sb.AppendLine();

        // Check for variable references in configuration
        sb.AppendLine("== Variable References in Configuration ==");
        var variableReferences = FindVariableReferences(configuration);

        if (variableReferences.Count > 0)
        {
            sb.AppendLine($"Found {variableReferences.Count} variable references:");
            foreach (var reference in variableReferences)
            {
                sb.AppendLine($"  - {reference.Path}: {reference.Value}");
            }
        }
        else
        {
            sb.AppendLine("No variable references found in configuration.");
        }

        sb.AppendLine();

        // Check for property overrides
        if (vaultFileExists)
        {
            try
            {
                var vaultContent = File.ReadAllText(vaultFilePath);
                if (IsValidJson(vaultContent))
                {
                    sb.AppendLine("== Property Overrides in Vault ==");
                    var propertyOverrides = FindPropertyOverrides(vaultContent);

                    if (propertyOverrides.Count > 0)
                    {
                        sb.AppendLine($"Found {propertyOverrides.Count} property overrides:");
                        foreach (var propertyOverride in propertyOverrides)
                        {
                            sb.AppendLine($"  - {propertyOverride}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("No property overrides found in vault file.");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error analyzing vault file: {ex.Message}");
            }
        }

        sb.AppendLine();

        // Environment info
        sb.AppendLine("== Environment Information ==");
        sb.AppendLine(
            $"ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set"}");
        sb.AppendLine($"DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Not set"}");
        sb.AppendLine($"Current Directory: {Directory.GetCurrentDirectory()}");
        sb.AppendLine();

        // Log report if logger provided
        logger?.LogInformation("EzLocalKeyVault diagnostic report generated");

        return sb.ToString();
    }

    /// <summary>
    /// Analyzes the configuration for substitution issues and provides recommendations.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="localKeyVault">The local key vault instance.</param>
    /// <param name="logger">Optional logger for output.</param>
    /// <returns>A list of issues and recommendations.</returns>
    public static List<string> AnalyzeSubstitutionIssues(IConfiguration configuration, ILocalKeyVault localKeyVault,
        ILogger? logger = null)
    {
        var issues = new List<string>();
        var variableReferences = FindVariableReferences(configuration);
        var secrets = localKeyVault.GetAll();

        foreach (var reference in variableReferences)
        {
            // Extract variable name from $(VARIABLE-NAME)
            var match = System.Text.RegularExpressions.Regex.Match(reference.Value, @"\$\(([^)]+)\)");
            if (match.Success)
            {
                var variableName = match.Groups[1].Value;
                if (!secrets.ContainsKey(variableName))
                {
                    issues.Add(
                        $"Variable '{variableName}' referenced in '{reference.Path}' but not found in vault file.");
                }
            }
        }

        // Log issues if logger provided
        if (logger != null && issues.Count > 0)
        {
            logger.LogWarning("Found {IssueCount} substitution issues", issues.Count);
            foreach (var issue in issues)
            {
                logger.LogWarning(issue);
            }
        }

        return issues;
    }

    /// <summary>
    /// Compares before and after configurations to identify substitution results.
    /// </summary>
    /// <param name="beforeConfig">Configuration before substitution.</param>
    /// <param name="afterConfig">Configuration after substitution.</param>
    /// <param name="logger">Optional logger for output.</param>
    /// <returns>A report of the differences.</returns>
    public static string CompareConfigurations(IConfiguration beforeConfig, IConfiguration afterConfig,
        ILogger? logger = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Configuration Comparison Report ===");
        sb.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        var beforeDict = FlattenConfiguration(beforeConfig);
        var afterDict = FlattenConfiguration(afterConfig);

        sb.AppendLine("== Changed Values ==");
        var changedCount = 0;

        foreach (var key in beforeDict.Keys)
        {
            if (afterDict.TryGetValue(key, out var afterValue) &&
                beforeDict.TryGetValue(key, out var beforeValue) &&
                beforeValue != afterValue)
            {
                changedCount++;
                sb.AppendLine($"  - {key}:");
                sb.AppendLine($"      Before: {MaskSensitiveValue(key, beforeValue)}");
                sb.AppendLine($"      After:  {MaskSensitiveValue(key, afterValue)}");
            }
        }

        if (changedCount == 0)
        {
            sb.AppendLine("No values were changed by substitution.");
        }
        else
        {
            sb.AppendLine($"Total changed values: {changedCount}");
        }

        sb.AppendLine();

        // Log report if logger provided
        logger?.LogInformation("Configuration comparison report generated. {ChangedCount} values were changed.",
            changedCount);

        return sb.ToString();
    }

    /// <summary>
    /// Exports the current configuration to a JSON file for inspection.
    /// </summary>
    /// <param name="configuration">The configuration to export.</param>
    /// <param name="outputPath">The output file path.</param>
    /// <param name="maskSensitiveValues">Whether to mask sensitive values.</param>
    /// <param name="logger">Optional logger for output.</param>
    public static void ExportConfigurationToJson(IConfiguration configuration, string outputPath,
        bool maskSensitiveValues = true, ILogger? logger = null)
    {
        try
        {
            var configDict = FlattenConfiguration(configuration);
            var jsonObj = new Dictionary<string, object>();

            // Convert flat dictionary to hierarchical JSON
            foreach (var item in configDict)
            {
                var value = maskSensitiveValues ? MaskSensitiveValue(item.Key, item.Value) : item.Value;
                AddNestedProperty(jsonObj, item.Key, value);
            }

            // Serialize to JSON
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(jsonObj, jsonOptions);

            // Write to file
            File.WriteAllText(outputPath, json);

            logger?.LogInformation("Configuration exported to {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error exporting configuration to {OutputPath}", outputPath);
            throw;
        }
    }

    #region Helper Methods

    private static bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<(string Path, string Value)> FindVariableReferences(IConfiguration configuration)
    {
        var result = new List<(string, string)>();
        var flatConfig = FlattenConfiguration(configuration);

        foreach (var item in flatConfig)
        {
            if (item.Value != null && item.Value.Contains("$("))
            {
                result.Add((item.Key, item.Value));
            }
        }

        return result;
    }

    private static List<string> FindPropertyOverrides(string vaultContent)
    {
        var result = new List<string>();
        var jsonDoc = JsonDocument.Parse(vaultContent);

        foreach (var property in jsonDoc.RootElement.EnumerateObject())
        {
            if (property.Name.Contains(':'))
            {
                result.Add(property.Name);
            }
        }

        return result;
    }

    private static Dictionary<string, string> FlattenConfiguration(IConfiguration configuration)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        FlattenConfigurationRecursive(configuration, string.Empty, result);
        return result;
    }

    private static void FlattenConfigurationRecursive(IConfiguration configuration, string prefix,
        Dictionary<string, string> result)
    {
        foreach (var child in configuration.GetChildren())
        {
            var key = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}:{child.Key}";

            if (child.Value != null)
            {
                result[key] = child.Value;
            }

            FlattenConfigurationRecursive(child, key, result);
        }
    }

    private static string MaskSensitiveValue(string key, string value)
    {
        var sensitivePatterns = new[] { "password", "secret", "key", "token", "credential", "pwd" };

        if (sensitivePatterns.Any(pattern => key.Contains(pattern, StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrEmpty(value))
        {
            return "***REDACTED***";
        }

        return value;
    }

    private static void AddNestedProperty(Dictionary<string, object> dict, string path, string value)
    {
        var parts = path.Split(':');
        var current = dict;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];

            if (!current.ContainsKey(part))
            {
                current[part] = new Dictionary<string, object>();
            }

            if (current[part] is Dictionary<string, object> nestedDict)
            {
                current = nestedDict;
            }
            else
            {
                // Handle case where we're trying to add a nested property to a leaf node
                var newDict = new Dictionary<string, object>();
                current[part] = newDict;
                current = newDict;
            }
        }

        current[parts[^1]] = value;
    }

    #endregion
}