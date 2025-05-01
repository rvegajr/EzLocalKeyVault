# Packaging and Publishing

This document provides instructions for packaging and publishing the EzLocalKeyVault libraries to NuGet.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version 8.0 or later)
- [NuGet CLI](https://www.nuget.org/downloads) or the `dotnet` CLI
- A NuGet account and API key for publishing

## Building NuGet Packages

### Building All Packages

To build all NuGet packages at once:

```bash
dotnet pack -c Release
```

This will create NuGet packages for all projects in the solution that are configured for packaging.

### Building Individual Packages

To build a specific package:

```bash
# For the core library
dotnet pack src/Noctusoft.EzLocalKeyVault/Noctusoft.EzLocalKeyVault.csproj -c Release

# For the Azure integration library
dotnet pack src/Noctusoft.EzAzureLocalKeyVault/Noctusoft.EzAzureLocalKeyVault.csproj -c Release

# For the CLI tool
dotnet pack src/Noctusoft.EzLocalKeyVault.Cli/Noctusoft.EzLocalKeyVault.Cli.csproj -c Release
```

The packages will be created in the `bin/Release` directory of each project.

## Publishing to NuGet.org

### Using dotnet CLI

```bash
# For the core library
dotnet nuget push src/Noctusoft.EzLocalKeyVault/bin/Release/Noctusoft.EzLocalKeyVault.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# For the Azure integration library
dotnet nuget push src/Noctusoft.EzAzureLocalKeyVault/bin/Release/Noctusoft.EzAzureLocalKeyVault.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# For the CLI tool
dotnet nuget push src/Noctusoft.EzLocalKeyVault.Cli/bin/Release/Noctusoft.EzLocalKeyVault.Cli.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

Replace `YOUR_API_KEY` with your NuGet API key.

### Using GitHub Actions

The repository includes GitHub Actions workflows for automated packaging and publishing. To use them:

1. Store your NuGet API key as a GitHub secret named `NUGET_API_KEY`
2. Push a tag with a version number (e.g., `v1.0.0`) to trigger the release workflow

## Installing the CLI as a .NET Global Tool

Once the CLI package is published to NuGet.org, you can install it as a global tool:

```bash
dotnet tool install --global Noctusoft.EzLocalKeyVault.Cli
```

This will make the `ez-keyvault` command available globally on your system.

To update to the latest version:

```bash
dotnet tool update --global Noctusoft.EzLocalKeyVault.Cli
```

To uninstall:

```bash
dotnet tool uninstall --global Noctusoft.EzLocalKeyVault.Cli
```

## Local Development Testing

For testing during development, you can install the tool locally from your build output:

```bash
# Pack the CLI project
dotnet pack src/Noctusoft.EzLocalKeyVault.Cli/Noctusoft.EzLocalKeyVault.Cli.csproj -c Release

# Install from the local package
dotnet tool install --global --add-source src/Noctusoft.EzLocalKeyVault.Cli/bin/Release Noctusoft.EzLocalKeyVault.Cli
```

## Version Management

The project uses [GitVersion](https://gitversion.net/) to automatically manage version numbers based on Git history and tags. The configuration is in the `GitVersion.yml` file.

When creating a release:

1. Ensure you're on the main branch with a clean working directory
2. Create and push a tag with the desired version (e.g., `git tag v1.2.0 && git push --tags`)
3. The GitHub Actions workflow will automatically build and publish the packages with the correct version numbers

## Package Dependencies

Make sure all package dependencies are correctly specified in the project files before publishing. The current dependencies are:

- For `Noctusoft.EzLocalKeyVault`:
  - Microsoft.Extensions.Configuration
  - Microsoft.Extensions.Logging
  - Microsoft.Extensions.Options

- For `Noctusoft.EzAzureLocalKeyVault`:
  - Azure.Identity
  - Azure.Security.KeyVault.Secrets
  - Microsoft.Extensions.Configuration.AzureKeyVault
  - Microsoft.Extensions.Hosting.Abstractions
  - Microsoft.Extensions.Options.ConfigurationExtensions
  - Noctusoft.EzLocalKeyVault

- For `Noctusoft.EzLocalKeyVault.Cli`:
  - System.CommandLine
  - Noctusoft.EzLocalKeyVault
