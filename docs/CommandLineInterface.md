# EZ Local Key Vault CLI Tool

The `ez-keyvault` command-line tool allows you to process templates with EzLocalKeyVault substitutions directly from the command line, without needing to write any code.

## Installation

### Global Tool Installation

```bash
dotnet tool install --global Noctusoft.EzLocalKeyVault.Cli
```

### Local Tool Installation

```bash
dotnet tool install --local Noctusoft.EzLocalKeyVault.Cli
```

## Usage

```bash
ez-keyvault --template <template-file> --vault <vault-file> [--output <output-file>]
```

### Options

- `-t, --template <template-file>` (Required): The template file to process (JSON format)
- `-v, --vault <vault-file>` (Required): The key vault file containing secrets (JSON format)
- `-o, --output <output-file>` (Optional): The output file path
  - If just a filename (no path), writes to the same directory as the template
  - If a full path, writes to that exact path
  - If omitted, outputs to console

## Examples

### Process a template and output to console

This command processes the template file using the specified vault file and displays the result in the console:

```bash
ez-keyvault -t appsettings.json -v .local-vault.json
```

### Process a template and save to a file in the same directory

This command processes the template and saves the result to a file in the same directory as the template:

```bash
ez-keyvault -t appsettings.json -v .local-vault.json -o processed-appsettings.json
```

### Process a template and save to a specific path

This command processes the template and saves the result to a specific path:

```bash
ez-keyvault -t appsettings.json -v .local-vault.json -o /path/to/output/processed-appsettings.json
```

## Template Format

The template should be a valid JSON file with placeholders in the format `$(VARIABLE-NAME)` that will be replaced with values from the vault file.

Example template (appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=$(DB_SERVER);Database=$(DB_NAME);User Id=$(DB_USER);Password=$(DB_PASSWORD);"
  },
  "ApiKeys": {
    "ExternalService": "$(API_KEY)"
  }
}
```

## Vault File Format

The vault file should be a JSON file containing key-value pairs for the variables to be substituted.

Example vault file (.local-vault.json):
```json
{
  "DB_SERVER": "localhost",
  "DB_NAME": "mydatabase",
  "DB_USER": "dbuser",
  "DB_PASSWORD": "securepassword",
  "API_KEY": "abc123xyz456"
}
```

## Common Use Cases

### CI/CD Pipelines

The CLI tool is particularly useful in CI/CD pipelines where you need to generate configuration files with environment-specific values:

```bash
# In a build script
ez-keyvault -t appsettings.template.json -v .env-specific-vault.json -o appsettings.json
```

### Docker Containers

When building Docker containers, you can use the CLI tool to generate configuration at build time:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Install the CLI tool
RUN dotnet tool install --global Noctusoft.EzLocalKeyVault.Cli

# Generate configuration
COPY appsettings.template.json .
COPY .container-vault.json .
RUN ez-keyvault -t appsettings.template.json -v .container-vault.json -o appsettings.json

# Continue with your Dockerfile...
```

### Local Development

For local development, you can use the CLI tool to generate configuration files with your local secrets:

```bash
# Generate a development configuration
ez-keyvault -t appsettings.template.json -v .local-vault.json -o appsettings.Development.json

# Generate a production configuration (with different secrets)
ez-keyvault -t appsettings.template.json -v .prod-vault.json -o appsettings.Production.json
```

## Security Considerations

- The vault file should be kept secure and not committed to source control
- Add `.local-vault.json` and any other vault files to your `.gitignore` file
- The CLI tool will automatically redact sensitive information in logs (passwords, keys, tokens, etc.)
- Consider using environment variables for sensitive values in CI/CD pipelines rather than vault files

## Troubleshooting

### File Not Found

If you get a "File not found" error, make sure the template and vault files exist at the specified paths.

### Invalid JSON

If you get an error about invalid JSON, check that both your template and vault files contain valid JSON.

### Missing Substitutions

If variables aren't being substituted, check that the variable names in your template match the keys in your vault file.

## Building from Source

If you want to build the CLI tool from source:

```bash
git clone https://github.com/yourusername/ez-local-keyvault.git
cd ez-local-keyvault
dotnet build src/Noctusoft.EzLocalKeyVault.Cli
dotnet pack src/Noctusoft.EzLocalKeyVault.Cli -c Release
```

Then install the tool locally:

```bash
dotnet tool install --global --add-source ./src/Noctusoft.EzLocalKeyVault.Cli/bin/Release Noctusoft.EzLocalKeyVault.Cli
```
