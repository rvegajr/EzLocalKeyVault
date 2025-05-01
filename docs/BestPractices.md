# Best Practices for EzLocalKeyVault

This document outlines best practices for using the Noctusoft.EzLocalKeyVault library in your applications.

## Secret Management

### Vault File Location

- **Default location**: Store your `.local-vault.json` file in the project root directory
- **Custom location**: For shared development environments, consider storing the vault file in a location outside the project directory
- **Environment-specific vaults**: Use different vault files for different environments (e.g., `.local-vault.development.json`, `.local-vault.test.json`)

### Secret Naming Conventions

- Use UPPERCASE_WITH_UNDERSCORES for secret names to distinguish them from regular configuration keys
- Use a consistent prefix for related secrets (e.g., `DB_*` for database-related secrets)
- Avoid using special characters in secret names

### Vault File Structure

```json
{
  "DB_SERVER": "localhost",
  "DB_NAME": "mydatabase",
  "DB_USER": "dbuser",
  "DB_PASSWORD": "securepassword",
  "API_KEY": "abc123xyz456",
  "FEATURE_FLAGS": {
    "ENABLE_NEW_UI": true,
    "ENABLE_BETA_FEATURES": false
  }
}
```

### Source Control

- **ALWAYS** add `.local-vault.json` to your `.gitignore` file
- Consider providing a `.local-vault.example.json` file with dummy values for reference
- Document the required secrets in your README.md file

## Configuration Patterns

### Variable Substitution

Use the `$(VARIABLE-NAME)` syntax for variable substitution in your configuration files:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=$(DB_SERVER);Database=$(DB_NAME);User Id=$(DB_USER);Password=$(DB_PASSWORD);"
  }
}
```

### Property Path Overrides

For complex configuration structures, use property path overrides:

```json
// In .local-vault.json
{
  "Logging:LogLevel:Default": "Debug",
  "Logging:LogLevel:Microsoft": "Warning"
}
```

This will override the corresponding properties in your configuration.

### Combining with Other Configuration Sources

When using EzLocalKeyVault with other configuration sources, consider the order of precedence:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")                   // Base configuration
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true) // Environment-specific
    .AddEzLocalKeyVault()                              // Local secrets
    .AddEnvironmentVariables()                         // Environment variables (higher precedence)
    .AddCommandLine(args)                              // Command line (highest precedence)
    .Build();
```

## Security Considerations

### Sensitive Information

- Treat all secrets in the vault file as sensitive information
- Never log secret values, even in development environments
- Use the built-in secret redaction feature to mask secrets in logs

### Secret Rotation

- Regularly rotate secrets, especially in shared development environments
- Document the process for updating secrets
- Consider using a secret manager tool for production environments

### Access Control

- Limit access to the vault file to only those who need it
- Consider encrypting the vault file for additional security
- Use different vault files for different teams or projects

## Performance Considerations

### File Watching

The `ReloadOnChange` option watches for changes to the vault file and automatically reloads the configuration. This is useful during development but may have performance implications:

```csharp
// Enable file watching (default)
builder.Configuration.AddEzLocalKeyVault(reloadOnChange: true);

// Disable file watching for better performance
builder.Configuration.AddEzLocalKeyVault(reloadOnChange: false);
```

### Caching

The library caches secrets in memory to avoid reading the vault file for every request. This improves performance but may cause issues if secrets are changed externally:

- Use the file watching feature during development to ensure changes are picked up
- Restart the application after changing secrets in production

## Testing

### Unit Testing

Create a test-specific vault file for unit tests:

```csharp
// In test setup
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.test.json")
    .AddEzLocalKeyVault("test-vault.json")
    .Build();
```

### Integration Testing

For integration tests, consider using a mock implementation of `ILocalKeyVault`:

```csharp
// Create a mock implementation
var mockKeyVault = new Mock<ILocalKeyVault>();
mockKeyVault.Setup(x => x.GetSecret("DB_SERVER")).Returns("test-server");
mockKeyVault.Setup(x => x.GetSecret("DB_NAME")).Returns("test-db");

// Register the mock in your service collection
services.AddSingleton(mockKeyVault.Object);
```

## Troubleshooting

### Common Issues

1. **Secret not found**: Check that the secret exists in the vault file with the exact name
2. **File not found**: Verify the path to the vault file
3. **Invalid JSON**: Ensure the vault file contains valid JSON
4. **Permission denied**: Check file permissions for the vault file

### Debugging

Enable debug logging to see detailed information about the key vault operations:

```csharp
builder.Logging.AddFilter("Noctusoft.EzLocalKeyVault", LogLevel.Debug);
```

### Validation

Validate your configuration during startup to catch missing or invalid secrets early:

```csharp
// Validate required configuration values
var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Required configuration 'ConnectionStrings:DefaultConnection' is missing or empty");
}
```

## Conclusion

By following these best practices, you can effectively use EzLocalKeyVault to manage secrets in your applications while maintaining security and performance.
