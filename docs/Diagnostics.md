# EzLocalKeyVault Diagnostics

This document explains how to use the diagnostic utilities provided by the `Noctusoft.EzLocalKeyVault` library to troubleshoot configuration issues.

## Overview

The diagnostic utilities help you identify and resolve issues with your local key vault configuration, such as:

- Missing or invalid vault files
- Variable references that don't have corresponding secrets
- Configuration substitution problems
- Property override issues

## Using the Diagnostic Utilities

### Generating a Diagnostic Report

The diagnostic report provides a comprehensive overview of your key vault configuration:

```csharp
using Microsoft.Extensions.Configuration;
using Noctusoft.EzLocalKeyVault.Extensions;

// In your application code
var report = configuration.GenerateKeyVaultDiagnosticReport();
Console.WriteLine(report);
```

The report includes:

- Configuration options
- Vault file status
- Variable references in configuration
- Property overrides in the vault
- Environment information

### Analyzing Substitution Issues

To identify specific substitution issues:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Noctusoft.EzLocalKeyVault.Extensions;

// In your application code
var issues = serviceProvider.AnalyzeKeyVaultSubstitutionIssues();
foreach (var issue in issues)
{
    Console.WriteLine(issue);
}
```

This will identify variables referenced in your configuration that are missing from the vault file.

### Exporting Configuration to JSON

To export your configuration to a JSON file for inspection:

```csharp
using Microsoft.Extensions.Configuration;
using Noctusoft.EzLocalKeyVault.Extensions;

// In your application code
configuration.ExportConfigurationToJson("config-export.json", maskSensitiveValues: true);
```

The `maskSensitiveValues` parameter controls whether sensitive values (passwords, keys, etc.) are redacted in the output.

### Adding Diagnostic Endpoints to ASP.NET Core Applications

For ASP.NET Core applications, you can add diagnostic endpoints that allow you to access diagnostic information through HTTP requests:

```csharp
using Noctusoft.EzLocalKeyVault.Extensions;

// In Program.cs or Startup.cs
app.UseEzLocalKeyVaultDiagnostics();
```

This adds the following endpoints:

- `/_diagnostics/keyvault/report` - Returns the diagnostic report
- `/_diagnostics/keyvault/issues` - Returns a list of substitution issues
- `/_diagnostics/keyvault/export` - Exports the configuration as JSON

> **Security Note**: These endpoints expose configuration information that may include sensitive data. They should only be enabled in development environments or protected with appropriate authentication and authorization.

You can customize the path prefix:

```csharp
app.UseEzLocalKeyVaultDiagnostics("/admin/diagnostics/keyvault");
```

## Troubleshooting Common Issues

### Missing Vault File

If the diagnostic report shows that the vault file doesn't exist, check:

1. The file path in your configuration
2. That the file has been created in the expected location
3. That the application has read permissions for the file

### Invalid JSON in Vault File

If the vault file exists but is reported as invalid JSON:

1. Validate the JSON syntax using a tool like [JSONLint](https://jsonlint.com/)
2. Check for common JSON errors like missing commas or quotes
3. Ensure the file is saved with UTF-8 encoding

### Missing Variable Substitutions

If variables aren't being substituted:

1. Check that the variable names in your configuration match the keys in your vault file
2. Verify that the variable syntax is correct: `$(VARIABLE-NAME)`
3. Check for case sensitivity issues (variable names are case-sensitive)

### Property Override Issues

If property overrides aren't working:

1. Ensure the property path in the vault file matches the exact path in your configuration
2. Check that the path uses colons as separators (e.g., `Logging:LogLevel:Default`)
3. Verify that the property exists in your base configuration

## Advanced Diagnostics

### Comparing Before and After Configurations

To see exactly what changes were made by the key vault substitution:

```csharp
using Microsoft.Extensions.Configuration;
using Noctusoft.EzLocalKeyVault.Diagnostics;

// Create a configuration without key vault
var beforeConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Create a configuration with key vault
var afterConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEzLocalKeyVault()
    .Build();

// Compare the configurations
var comparisonReport = KeyVaultDiagnostics.CompareConfigurations(beforeConfig, afterConfig);
Console.WriteLine(comparisonReport);
```

This will show you exactly which values were changed by the substitution process.

## Conclusion

The diagnostic utilities provided by EzLocalKeyVault make it easier to identify and resolve configuration issues. By generating reports, analyzing substitution issues, and exporting configurations, you can quickly troubleshoot problems and ensure your application is correctly configured.
