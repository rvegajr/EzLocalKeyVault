# EzLocalKeyVault Sample Configurations

This directory contains sample configuration files that demonstrate how to use EzLocalKeyVault in different environments.

## Files Overview

- `appsettings.sample.json` - Base application settings with variable references
- `.local-vault.sample.json` - Sample vault file with secrets and property overrides
- `appsettings.Development.json` - Development-specific settings
- `.local-vault.development.json` - Development environment vault file
- `appsettings.Production.json` - Production-specific settings
- `.local-vault.production.json` - Production environment vault file (showing Azure Key Vault integration)

## How to Use These Samples

### Basic Usage

1. Copy `appsettings.sample.json` to your project as `appsettings.json`
2. Copy `.local-vault.sample.json` to your project as `.local-vault.json`
3. Add `.local-vault.json` to your `.gitignore` file
4. Configure your application to use EzLocalKeyVault:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add EzLocalKeyVault to the configuration
builder.Configuration.AddEzLocalKeyVault();

// Register EzLocalKeyVault services
builder.Services.AddEzLocalKeyVault(builder.Configuration);
```

### Environment-Specific Configuration

The samples demonstrate how to use different vault files for different environments:

1. Development environment:
   - Uses `.local-vault.development.json`
   - Enables more verbose logging
   - Enables file watching for real-time updates

2. Production environment:
   - Uses `.local-vault.production.json`
   - Shows integration with Azure Key Vault
   - Disables file watching and verbose logging

To use environment-specific configuration:

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// The environment name is automatically detected
// You can access it with builder.Environment.EnvironmentName

// Add EzLocalKeyVault with environment-specific settings
builder.Configuration.AddEzLocalKeyVault(options => {
    // The VaultFilePath will be read from appsettings.{Environment}.json
    // but you could also set it programmatically based on the environment
    if (builder.Environment.IsDevelopment()) {
        options.LogSubstitutions = true;
        options.ReloadOnChange = true;
    }
});

builder.Services.AddEzLocalKeyVault(builder.Configuration);
```

## Azure Key Vault Integration

The production sample demonstrates how to reference Azure Key Vault secrets:

```json
{
  "DB_PASSWORD": "$(AZURE_KEY_VAULT:DbPassword)"
}
```

This assumes you have implemented an Azure Key Vault provider that recognizes the `AZURE_KEY_VAULT:` prefix. See the [Azure Key Vault Integration](../../docs/AzureKeyVaultIntegration.md) documentation for details.

## Testing with These Samples

You can test the sample configurations using the EzLocalKeyVault CLI tool:

```bash
# Install the CLI tool
dotnet tool install --global Noctusoft.EzLocalKeyVault.Cli

# Process the sample template with the sample vault
ez-keyvault -t samples/SampleConfigs/appsettings.sample.json -v samples/SampleConfigs/.local-vault.sample.json -o processed-config.json

# Process with environment-specific files
ez-keyvault -t samples/SampleConfigs/appsettings.sample.json -v samples/SampleConfigs/.local-vault.development.json -o processed-dev-config.json
```

## Diagnostic Tools

You can use the EzLocalKeyVault diagnostic tools to troubleshoot issues with these sample configurations:

```csharp
// Generate a diagnostic report
var report = configuration.GenerateKeyVaultDiagnosticReport();
Console.WriteLine(report);

// Analyze substitution issues
var issues = serviceProvider.AnalyzeKeyVaultSubstitutionIssues();
foreach (var issue in issues)
{
    Console.WriteLine(issue);
}

// Export configuration to JSON
configuration.ExportConfigurationToJson("config-export.json");
```

For more information, see the [Diagnostics documentation](../../docs/Diagnostics.md).
