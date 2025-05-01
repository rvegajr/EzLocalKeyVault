# EzLocalKeyVault

A utility library that simplifies using Azure Key Vault and local key vault for .NET applications.

## Quick Start - Hybrid Key Vault Integration

The easiest way to get started is with the hybrid key vault integration, which automatically switches between Azure Key Vault and local key vault based on your environment.

### 1. Install the NuGet Package

```bash
dotnet add package Noctusoft.EzAzureLocalKeyVault --version 1.0.1
```

### 2. Add to Your Application (Single Line Usage)

```csharp
// In Program.cs
using Noctusoft.EzAzureLocalKeyVault.Extensions;

var builder = WebApplication.CreateBuilder(args);

// One line to add hybrid key vault to your application
builder.UseEzAzureLocalKeyVault((context, options) => 
{
    options.AzureKeyVaultUri = "https://your-key-vault.vault.azure.net/";
});

var app = builder.Build();
// ...
```

For even simpler usage, you can configure the Azure Key Vault URI in your appsettings.json:

```csharp
// In Program.cs
using Noctusoft.EzAzureLocalKeyVault.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Simplest one-line usage (gets settings from appsettings.json)
builder.UseEzAzureLocalKeyVault();

var app = builder.Build();
// ...
```

### 3. Configure (Optional)

Add configuration to your `appsettings.json`:

```json
{
  "EzAzureLocalKeyVault": {
    "AzureKeyVaultUri": "https://your-key-vault.vault.azure.net/",
    "LocalKeyVaultEnvironments": ["Development", "LocalDevelopment"],
    "FallbackToLocalKeyVault": true
  }
}
```

### 4. Use in Your Code

```csharp
// Inject the service
public class MyService
{
    private readonly IHybridKeyVault _keyVault;
    
    public MyService(IHybridKeyVault keyVault)
    {
        _keyVault = keyVault;
    }
    
    public void DoSomething()
    {
        // Get a secret
        var secret = _keyVault.GetSecret("my-secret");
    }
}
```

### 5. Using Typed Configuration

One of the most powerful features is the ability to bind configuration to strongly-typed classes after key vault substitutions have been applied:

```csharp
// Define your configuration classes
public class AppSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public ConnectionStrings ConnectionStrings { get; set; } = new();
}

public class ConnectionStrings
{
    public string DefaultConnection { get; set; } = string.Empty;
}

// In Program.cs
using Noctusoft.EzAzureLocalKeyVault.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.UseEzAzureLocalKeyVault();

// Get typed configuration after key vault substitutions
var appSettings = builder.Configuration.GetTypedSection<AppSettings>();

// Or register with dependency injection
// Empty string means "use the root configuration"
builder.Services.AddTypedConfiguration<AppSettings>(builder.Configuration, "");

// If your settings are in a specific section:
builder.Services.AddTypedConfiguration<AppSettings>(builder.Configuration, "AppSettings");

// Or do both at once
var settings = builder.Services.AddAndGetTypedConfiguration<AppSettings>(builder.Configuration, "");

var app = builder.Build();
// ...
```

Then use the typed configuration in your services:

```csharp
public class MyService
{
    private readonly AppSettings _settings;
    
    public MyService(IOptions<AppSettings> options)
    {
        _settings = options.Value;
    }
    
    public void DoSomething()
    {
        // Access your configuration with full IntelliSense support
        var apiKey = _settings.ApiKey;
        var connectionString = _settings.ConnectionStrings.DefaultConnection;
    }
}
```

That's it! The library will automatically:
- Use local key vault in development environments
- Use Azure Key Vault in production environments
- Fall back to local key vault if Azure Key Vault is unavailable
- Fall back to Azure Key Vault if local key vault file is missing
- Cache Azure Key Vault secrets for better performance

## Features

- **Environment Detection**: Automatically use local key vault in development, Azure Key Vault in production
- **Intelligent Fallback**: Gracefully handle missing files or unavailable services
- **Secret Caching**: Improve performance by caching Azure Key Vault secrets
- **Typed Configuration**: Bind configuration to strongly-typed classes
- **Variable Substitution**: Replace placeholders in configuration with secrets
- **CLI Tool**: Process templates from the command line

## Original EzLocalKeyVault Library

If you only need local key vault functionality without Azure Key Vault integration, you can use the original library:

```bash
dotnet add package Noctusoft.EzLocalKeyVault --version 1.0.1
```

### Basic Usage

```csharp
// In Program.cs
using Noctusoft.EzLocalKeyVault.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add the local key vault to the configuration
builder.Configuration.AddEzLocalKeyVault();

// Register the local key vault service
builder.Services.AddEzLocalKeyVault(builder.Configuration);

var app = builder.Build();
// ...
```

## Motivation

When developing applications that use Azure Key Vault in production, it can be challenging to work with configuration values locally. This package provides a simple solution by allowing you to:

1. Use a local JSON file as a key vault
2. Support variable substitution in your configuration files using the `$(VARIABLE-NAME)` syntax
3. Override configuration values directly using JSON property paths
4. Automatically reload when the vault file changes
5. Securely handle sensitive information by redacting secrets in logs

## Installation

```bash
# For local key vault only
dotnet add package Noctusoft.EzLocalKeyVault --version 1.0.1

# For hybrid Azure/Local key vault
dotnet add package Noctusoft.EzAzureLocalKeyVault --version 1.0.1

# For configuration validation and typed configuration support
dotnet add package Noctusoft.EzLocalKeyVault.Configuration --version 1.0.1
```

## Configuration Reference

You can customize the behavior of the local key vault by adding an `EzLocalKeyVault` section to your `appsettings.json`:

```json
{
  "EzLocalKeyVault": {
    "VaultFilePath": ".local-vault.json",
    "ReloadOnChange": true,
    "LogSubstitutions": true,
    "RedactionPatterns": ["password", "secret", "key", "token"]
  }
}
```

| Option | Description | Default |
|--------|-------------|---------|
| `VaultFilePath` | Path to the local vault file | `.local-vault.json` |
| `ReloadOnChange` | Whether to reload the configuration when the vault file changes | `true` |
| `LogSubstitutions` | Whether to log substitutions | `true` |
| `RedactionPatterns` | Patterns used to identify values that should be redacted in logs | `["password", "secret", "key", "token"]` |

## Advanced Usage

### Programmatic Configuration

You can also configure the local key vault programmatically:

```csharp
// Specify a custom vault file path
builder.Configuration.AddEzLocalKeyVault("custom-path/my-vault.json");

// Configure options
builder.Services.AddEzLocalKeyVault(options => {
    options.VaultFilePath = "custom-path/my-vault.json";
    options.ReloadOnChange = false;
    options.LogSubstitutions = false;
    options.RedactionPatterns = new[] { "custom-pattern" };
});
```

### Accessing the Key Vault Directly

You can inject the `ILocalKeyVault` interface to access the key vault directly:

```csharp
public class MyService
{
    private readonly ILocalKeyVault _keyVault;
    
    public MyService(ILocalKeyVault keyVault)
    {
        _keyVault = keyVault;
    }
    
    public void DoSomething()
    {
        // Get a specific secret
        var secret = _keyVault.GetSecret("my-secret");
        
        // Get all secrets
        var allSecrets = _keyVault.GetAll();
    }
}
```

### Using Diagnostic Utilities

EzLocalKeyVault includes diagnostic utilities to help troubleshoot configuration issues:

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

// For ASP.NET Core applications, add diagnostic endpoints
app.UseEzLocalKeyVaultDiagnostics();
// Access at /_diagnostics/keyvault/report, /_diagnostics/keyvault/issues, and /_diagnostics/keyvault/export
```

For more details, see the [Diagnostics documentation](docs/Diagnostics.md).

## Advanced Usage - Configuration Builder Integration

If you want to integrate key vault resolution into your existing configuration setup, you can use the `ResolveFromLocalKeyvault` extension method:

```csharp
public static void ConfigureEnvironmentSpecificSettings(ConfigurationBuilder configBuilder)
{
    // Base configuration is always loaded
    configBuilder.SetBasePath(Directory.GetCurrentDirectory());
    configBuilder.AddJsonFile("appsettings.json", false, true);

    // Load environment-specific settings
    var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    configBuilder.AddJsonFile($"appsettings.{environmentName}.json", true, true);

    // Resolve values from local key vault in development
    if (environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase))
    {
        configBuilder.ResolveFromLocalKeyvault();
    }

    // Add environment variables last for overrides
    configBuilder.AddEnvironmentVariables();
}
```

This approach allows you to:
1. Load your base configuration files
2. Apply environment-specific settings
3. Resolve key vault values where needed
4. Override with environment variables

For a complete example, check out the `samples/ConfigurationSample` directory.

## Security Notice

This package is designed for local development environments only and should not be used in production. The local vault file is stored in plain text and does not provide the same level of security as Azure Key Vault.

While the package includes features to redact sensitive information in logs, it's essential to be careful when handling secrets locally:

1. Never commit your `.local-vault.json` file to source control
2. Add `.local-vault.json` to your `.gitignore` file
3. Be cautious when sharing configuration files that contain variable references

## Documentation

For more detailed information, check out these documentation guides:

- [Best Practices](docs/BestPractices.md) - Recommended patterns and practices for using EzLocalKeyVault
- [Command Line Interface](docs/CommandLineInterface.md) - Using the ez-keyvault CLI tool
- [Azure Key Vault Integration](docs/AzureKeyVaultIntegration.md) - How to integrate with Azure Key Vault
- [Diagnostics](docs/Diagnostics.md) - Tools and utilities for troubleshooting configuration issues
- [Sample Configurations](samples/SampleConfigs/README.md) - Example appsettings.json and .local-vault.json files for different environments
- [Hybrid Key Vault Integration](docs/HybridKeyVaultIntegration.md) - Using the EzAzureLocalKeyVault library to seamlessly switch between Azure Key Vault and local key vault
- [Packaging and Publishing](docs/PackagingAndPublishing.md) - Instructions for creating NuGet packages and publishing to NuGet.org

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Changelog

### v1.0.0 (2025-05-01)

#### Added
- Initial release of `Noctusoft.EzLocalKeyVault` library
  - Local key vault implementation for development environments
  - Configuration substitution with variable references
  - Diagnostic utilities for troubleshooting
  - Command-line interface for processing templates
- New `Noctusoft.EzAzureLocalKeyVault` library
  - Hybrid key vault that automatically switches between Azure Key Vault and local key vault
  - Environment-based detection with intelligent fallback logic
  - Caching of Azure Key Vault secrets for improved performance
  - Direct Azure Key Vault references with prefix support
  - Strongly-typed configuration binding
- Enhanced fallback behavior
  - Automatic fallback to Azure Key Vault if local key vault file is missing
  - Detailed logging for troubleshooting
  - Clear error messages when no fallback options are available
- Comprehensive documentation
  - Installation and usage guides
  - Best practices
  - Sample applications
