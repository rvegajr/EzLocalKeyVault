# Hybrid Key Vault Integration

This document explains how to use the `Noctusoft.EzAzureLocalKeyVault` library to seamlessly switch between Azure Key Vault and local key vault based on your environment.

## Overview

The `Noctusoft.EzAzureLocalKeyVault` library provides a unified approach to managing secrets by:

- Using local key vault for development environments
- Using Azure Key Vault for production environments
- Providing automatic fallback to local key vault if Azure Key Vault is unavailable
- Supporting caching of Azure Key Vault secrets for improved performance
- Maintaining the same API as `Noctusoft.EzLocalKeyVault` for compatibility

## Installation

Install the package from NuGet:

```bash
dotnet add package Noctusoft.EzAzureLocalKeyVault
```

## Configuration

Configure the hybrid key vault in your `appsettings.json` file:

```json
{
  "EzAzureLocalKeyVault": {
    "AzureKeyVaultUri": "https://your-key-vault.vault.azure.net/",
    "UseLocalKeyVault": false,
    "LocalKeyVaultEnvironments": ["Development", "LocalDevelopment"],
    "FallbackToLocalKeyVault": true,
    "CacheAzureKeyVaultSecrets": true,
    "CacheDurationMinutes": 60,
    "AzureKeyVaultPrefix": "AZURE_KEY_VAULT:",
    "LocalKeyVaultOptions": {
      "VaultFilePath": ".local-vault.json",
      "ReloadOnChange": true,
      "LogSubstitutions": true
    }
  }
}
```

### Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `AzureKeyVaultUri` | The URI of your Azure Key Vault | `null` |
| `UseLocalKeyVault` | Force using local key vault regardless of environment | `false` |
| `LocalKeyVaultEnvironments` | List of environment names that should use local key vault | `["Development", "LocalDevelopment"]` |
| `FallbackToLocalKeyVault` | Whether to fallback to local key vault if Azure Key Vault is unavailable | `true` |
| `CacheAzureKeyVaultSecrets` | Whether to cache Azure Key Vault secrets locally | `true` |
| `CacheDurationMinutes` | How long to cache Azure Key Vault secrets | `60` |
| `AzureKeyVaultPrefix` | Prefix for Azure Key Vault secret references | `"AZURE_KEY_VAULT:"` |
| `LocalKeyVaultOptions` | Options for the local key vault | See below |

#### Local Key Vault Options

| Option | Description | Default |
|--------|-------------|---------|
| `VaultFilePath` | Path to the local vault file | `.local-vault.json` |
| `ReloadOnChange` | Whether to reload the vault file when it changes | `true` |
| `LogSubstitutions` | Whether to log substitutions | `true` |
| `RedactionPatterns` | Patterns for redacting sensitive values in logs | `null` |

## Usage

### Basic Setup

Add the hybrid key vault to your application in `Program.cs`:

```csharp
using Noctusoft.EzAzureLocalKeyVault.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add the hybrid key vault to the configuration
builder.Configuration.AddEzAzureLocalKeyVault();

// Register the hybrid key vault services
builder.Services.AddEzAzureLocalKeyVault(builder.Configuration);

var app = builder.Build();
// ...
```

### Using with Host Builder

For a more streamlined setup, you can use the `UseEzAzureLocalKeyVault` extension method:

```csharp
using Noctusoft.EzAzureLocalKeyVault.Extensions;

Host.CreateDefaultBuilder(args)
    .UseEzAzureLocalKeyVault()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    });
```

### Accessing Secrets

You can access secrets through the `IHybridKeyVault` interface:

```csharp
using Noctusoft.EzAzureLocalKeyVault.Core;

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
        
        // Check which vault is being used
        if (_keyVault.IsUsingLocalKeyVault)
        {
            Console.WriteLine("Using local key vault");
        }
        else
        {
            Console.WriteLine("Using Azure Key Vault");
        }
        
        // Get a secret directly from Azure Key Vault
        var azureSecret = await _keyVault.GetAzureSecretAsync("my-azure-secret");
    }
}
```

### Variable Substitution

The hybrid key vault supports the same variable substitution syntax as `EzLocalKeyVault`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=$(DB_SERVER);Database=$(DB_NAME);User Id=$(DB_USER);Password=$(DB_PASSWORD);"
  }
}
```

### Azure Key Vault References

You can also reference Azure Key Vault secrets directly using the `AZURE_KEY_VAULT:` prefix:

```json
{
  "ApiKey": "$(AZURE_KEY_VAULT:ApiKey)",
  "ConnectionStrings": {
    "DefaultConnection": "Server=myserver;Database=mydb;User Id=$(AZURE_KEY_VAULT:DbUser);Password=$(AZURE_KEY_VAULT:DbPassword);"
  }
}
```

### Typed Configuration

The hybrid key vault library provides extension methods to easily bind configuration sections to strongly-typed classes after key vault substitutions have been applied:

```csharp
// Define your configuration classes
public class AppSettings
{
    public string ApiKey { get; set; }
    public string ServiceUrl { get; set; }
    public ConnectionStrings ConnectionStrings { get; set; }
}

public class ConnectionStrings
{
    public string DefaultConnection { get; set; }
}

// Register typed configuration with dependency injection
services.AddTypedConfiguration<AppSettings>(configuration, "");

// Access typed configuration using IOptions pattern
public class MyService
{
    private readonly AppSettings _settings;
    
    public MyService(IOptions<AppSettings> options)
    {
        _settings = options.Value;
    }
}

// Or get typed configuration directly from IConfiguration
var appSettings = configuration.GetTypedSection<AppSettings>("");
var connectionStrings = configuration.GetTypedSection<ConnectionStrings>("ConnectionStrings");

// You can also add and get typed configuration in one step
var settings = services.AddAndGetTypedConfiguration<AppSettings>(configuration, "");
```

This approach provides several benefits:
- Type safety and IntelliSense support
- Validation through data annotations
- Easier testing with strongly-typed mocks
- Better encapsulation of configuration concerns

## Environment-Specific Configuration

You can create environment-specific configurations:

### Development (appsettings.Development.json)

```json
{
  "EzAzureLocalKeyVault": {
    "UseLocalKeyVault": true,
    "LocalKeyVaultOptions": {
      "VaultFilePath": ".local-vault.development.json"
    }
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "EzAzureLocalKeyVault": {
    "UseLocalKeyVault": false,
    "FallbackToLocalKeyVault": false,
    "CacheAzureKeyVaultSecrets": true,
    "CacheDurationMinutes": 30
  }
}
```

## Diagnostics

The library includes diagnostic tools to help troubleshoot issues:

```csharp
using Noctusoft.EzAzureLocalKeyVault.Extensions;

// Generate a diagnostic report
var report = configuration.GenerateHybridKeyVaultDiagnosticReport();
Console.WriteLine(report);

// Test the connection to Azure Key Vault
var connectionTest = serviceProvider.TestAzureKeyVaultConnection();
Console.WriteLine(connectionTest);
```

## Azure Authentication

The library uses `DefaultAzureCredential` from the Azure.Identity package, which supports multiple authentication methods:

1. Environment variables (client ID, client secret, tenant ID)
2. Managed Identity
3. Visual Studio authentication
4. Azure CLI authentication
5. Azure PowerShell authentication

For local development, the easiest approach is to use the Azure CLI:

```bash
az login
```

For production environments, Managed Identity is recommended.

## Best Practices

1. **Development Environment**:
   - Use local key vault with a `.local-vault.json` file
   - Add `.local-vault.json` to your `.gitignore` file
   - Enable `ReloadOnChange` for real-time updates during development

2. **CI/CD Environment**:
   - Use Azure Key Vault with service principal authentication
   - Disable `FallbackToLocalKeyVault` to fail fast if Azure Key Vault is unavailable
   - Create a minimal `.local-vault.json` with non-sensitive values for testing

3. **Production Environment**:
   - Use Azure Key Vault with Managed Identity authentication
   - Enable caching to improve performance
   - Disable `FallbackToLocalKeyVault` to ensure secrets are always from Azure Key Vault

## Automatic Fallback Behavior

The hybrid key vault implements an intelligent fallback mechanism that handles various scenarios:

### Missing Local Key Vault File

When the system is configured to use the local key vault (either explicitly or based on the environment), but the local key vault file is missing:

1. The system will log a warning about the missing file
2. If Azure Key Vault is properly configured, it will automatically fall back to using Azure Key Vault
3. If Azure Key Vault is not available and no fallback options exist, it will throw a `FileNotFoundException` with a detailed message

This behavior is particularly useful in scenarios like:

- Development environments deployed on Azure without a local key vault file
- Testing environments where you want to use Azure Key Vault by default, but fall back to local if needed
- CI/CD pipelines where you might not have a local key vault file

Example log output when falling back to Azure Key Vault:

```
warn: Noctusoft.EzAzureLocalKeyVault.Core.HybridKeyVault[0]
      Local key vault file not found at path: .local-vault.json. Environment Development is configured to use local key vault, but the file does not exist.
info: Noctusoft.EzAzureLocalKeyVault.Core.HybridKeyVault[0]
      Falling back to Azure Key Vault because local key vault file was not found
```

### Azure Key Vault Unavailable

When Azure Key Vault is configured but unavailable (due to network issues, authentication problems, etc.):

1. If `FallbackToLocalKeyVault` is enabled and the local key vault file exists, it will use the local key vault
2. If the local key vault file doesn't exist, it will throw an exception with a detailed message

This ensures that your application will always have access to secrets, while providing clear error messages when neither option is available.

## Conclusion

The `Noctusoft.EzAzureLocalKeyVault` library provides a seamless way to use local key vault for development and Azure Key Vault for production, with automatic environment detection and fallback options. This approach simplifies secret management across different environments while maintaining a consistent API.
