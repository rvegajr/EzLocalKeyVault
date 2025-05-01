# ez-keyvault CLI Tool

A command-line tool for processing templates with EzLocalKeyVault substitutions.

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

### Examples

Process a template and output to console:
```bash
ez-keyvault -t appsettings.json -v .local-vault.json
```

Process a template and save to a file in the same directory:
```bash
ez-keyvault -t appsettings.json -v .local-vault.json -o processed-appsettings.json
```

Process a template and save to a specific path:
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

## Security Considerations

- The vault file should be kept secure and not committed to source control
- Add `.local-vault.json` to your `.gitignore` file
- The CLI tool will automatically redact sensitive information in logs (passwords, keys, tokens, etc.)
