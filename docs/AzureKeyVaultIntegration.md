# Integrating with Azure Key Vault

This guide explains how to integrate `Noctusoft.EzLocalKeyVault` with Azure Key Vault to create a seamless development-to-production workflow.

## Overview

A common scenario is to use local key vault during development and Azure Key Vault in production environments. This document provides guidance on implementing this pattern.

## Implementation Strategy

The recommended approach is to create environment-specific configuration logic in your application that selects the appropriate key vault provider based on the current environment.

### 1. Install Required Packages

```bash
# For local development (already in your project)
dotnet add package Noctusoft.EzLocalKeyVault

# For Azure Key Vault
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

### 2. Create a Configuration Extension

Create an extension method that selects the appropriate key vault based on the environment:

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Noctusoft.EzLocalKeyVault.Extensions;

public static class KeyVaultConfigurationExtensions
{
    public static IConfigurationBuilder AddEnvironmentAwareKeyVault(
        this IConfigurationBuilder builder,
        IHostEnvironment environment,
        string azureKeyVaultUri,
        string localVaultPath = ".local-vault.json")
    {
        // Check if we're in a development environment
        if (environment.IsDevelopment() || environment.EnvironmentName == "LocalDevelopment")
        {
            // Use local key vault for development
            return builder.AddEzLocalKeyVault(localVaultPath);
        }
        else
        {
            // Use Azure Key Vault for production and other environments
            var secretClient = new SecretClient(
                new Uri(azureKeyVaultUri),
                new DefaultAzureCredential());
                
            return builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        }
    }
}
```

### 3. Register in Program.cs or Startup.cs

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add environment-aware key vault
builder.Configuration.AddEnvironmentAwareKeyVault(
    builder.Environment, 
    "https://your-vault.vault.azure.net/");

// Continue with application setup...
```

## Advanced Scenarios

### Handling Different Naming Conventions

Azure Key Vault and local key vault might use different naming conventions. You can create a custom `KeyVaultSecretManager` to handle these differences:

```csharp
public class CustomKeyVaultSecretManager : KeyVaultSecretManager
{
    public override string GetKey(KeyVaultSecret secret)
    {
        // Convert Azure Key Vault naming convention to your application's convention
        // Example: Convert "ConnectionStrings--DefaultConnection" to "ConnectionStrings:DefaultConnection"
        return secret.Name.Replace("--", ":");
    }
}
```

### Using Environment Variables to Control Behavior

You can use environment variables to control which key vault to use:

```csharp
public static IConfigurationBuilder AddEnvironmentAwareKeyVault(
    this IConfigurationBuilder builder,
    IHostEnvironment environment,
    string azureKeyVaultUri,
    string localVaultPath = ".local-vault.json")
{
    // Check for explicit override via environment variable
    var forceLocalKeyVault = Environment.GetEnvironmentVariable("USE_LOCAL_KEYVAULT")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    var forceAzureKeyVault = Environment.GetEnvironmentVariable("USE_AZURE_KEYVAULT")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    
    if (forceLocalKeyVault)
    {
        return builder.AddEzLocalKeyVault(localVaultPath);
    }
    else if (forceAzureKeyVault)
    {
        var secretClient = new SecretClient(
            new Uri(azureKeyVaultUri),
            new DefaultAzureCredential());
            
        return builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
    }
    else
    {
        // Default behavior based on environment
        if (environment.IsDevelopment() || environment.EnvironmentName == "LocalDevelopment")
        {
            return builder.AddEzLocalKeyVault(localVaultPath);
        }
        else
        {
            var secretClient = new SecretClient(
                new Uri(azureKeyVaultUri),
                new DefaultAzureCredential());
                
            return builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        }
    }
}
```

## Security Considerations

1. **Never commit secrets to source control**: Always add `.local-vault.json` to your `.gitignore` file.
2. **Use managed identities in Azure**: When possible, use managed identities for Azure resources to access Key Vault.
3. **Restrict access to Azure Key Vault**: Use Azure RBAC to limit who can access secrets in your Azure Key Vault.
4. **Rotate secrets regularly**: Implement a process for regular secret rotation.

## Testing Considerations

When writing tests, you may want to use a mock key vault or a test-specific local vault file:

```csharp
// In test setup
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.test.json")
    .AddEzLocalKeyVault("test-vault.json")
    .Build();
```

## Troubleshooting

### Common Issues with Azure Key Vault

1. **Authentication failures**: Ensure your application has the correct permissions to access the Azure Key Vault.
2. **Missing secrets**: Verify that the secrets exist in the Key Vault with the expected names.
3. **Credential issues**: When running locally, make sure you're logged in with `az login` or have appropriate environment variables set.

### Common Issues with Local Key Vault

1. **File not found**: Ensure the `.local-vault.json` file exists in the expected location.
2. **Invalid JSON**: Verify that your local vault file contains valid JSON.
3. **Missing substitutions**: Check that your variable names in the configuration match the keys in your local vault file.

## Conclusion

By following this approach, you can create a seamless experience that uses local key vault during development and Azure Key Vault in production, without changing your application code.
