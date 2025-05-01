# Noctusoft.EzLocalKeyVault - Developer Task List

## Overview
This document outlines the development tasks for creating a NuGet package that replicates Azure Key Vault functionality but for local development environments. The package will support variable substitution in configuration files using a local vault file.

## Project Structure

- [x] Create solution structure for Noctusoft.EzLocalKeyVault
- [x] Set up main project Noctusoft.EzLocalKeyVault
- [x] Set up test project Noctusoft.EzLocalKeyVault.Tests
- [x] Configure NuGet package metadata

## Core Components

### Settings Models
- [x] Create settings models that can be bound from appsettings.json
- [x] Implement LocalKeyVaultOptions class
- [x] Implement validation for settings

### Local Key Vault Implementation
- [x] Create ILocalKeyVault interface
- [x] Implement LocalKeyVault class
  - [x] Implement variable substitution ($(VARIABLE-NAME))
  - [x] Implement direct JSON property substitution (Property:Nested:Value)
  - [x] Support for reading local vault from .local-vault.json

### Configuration Extension
- [x] Create extension methods for IConfiguration
- [x] Implement AddLocalKeyVault() extension method
- [x] Implement ApplyLocalKeyVaultSubstitutions() method

### Secret Management
- [x] Implement secret detection/masking in logs
- [x] Add support for marking fields as secrets

### Logging
- [x] Implement logging middleware
  - [x] Log variable substitutions in lower environments
  - [x] Mask secrets in logs
  - [x] Log count of variables substituted in all environments

## Testing Suite (TDD Approach)

### Unit Tests
- [x] Test variable substitution ($(VARIABLE-NAME))
- [x] Test direct property substitution (Property:Nested:Value)
- [x] Test secret masking in logs
- [x] Test configuration binding
- [x] Test validation logic

### Integration Tests
- [x] Test with sample appsettings.json files
- [x] Test with sample .local-vault.json files
- [x] Test environment-specific behavior

## Documentation

- [x] Create README.md with usage examples
- [x] Add XML documentation comments for public APIs
- [x] Create sample projects demonstrating usage
- [x] Document best practices for storing local secrets

## CI/CD Pipeline

- [x] Set up build and test workflow
- [x] Set up NuGet package publishing
- [x] Configure versioning strategy

## Post-Release Tasks

- [x] Create usage documentation
- [x] Set up issue tracking
- [x] Plan for future enhancements
- [x] Fix duplicate PackageReference warnings for Microsoft.Extensions.Logging.Abstractions

## Security Considerations

- [x] Ensure secrets are not accidentally logged
- [x] Implement best practices for local secret storage
- [x] Add guidelines for production vs. development usage

## Development Process (Test-Driven Design)

1. For every checklist item above **start with a failing test**.
   * Use **xUnit** as the unit–testing framework and **FluentAssertions** for readable assertions.
   * Commit the failing test first ("Red" phase).
2. Implement the minimum production code required to make the test pass ("Green" phase).
3. **Refactor** mercilessly while keeping the build green; follow SOLID & KISS.
4. Ensure **code coverage ≥ 90 %** – the CI pipeline fails if the threshold is not met.
5. Run static analysis (`dotnet format`, StyleCop, FxCop analyzers) locally and in CI.

## Repository Layout

```
/Noctusoft.EzLocalKeyVault.sln
/src/Noctusoft.EzLocalKeyVault/
/src/Noctusoft.EzLocalKeyVault/Extensions/
/src/Noctusoft.EzLocalKeyVault/Logging/
/src/Noctusoft.EzLocalKeyVault/Options/
/tests/Noctusoft.EzLocalKeyVault.Tests/
/.github/workflows/build.yml
```

*Keep all production code under **`src`** and all tests under **`tests`**.*

## Detailed Implementation Tasks

### 1. Settings / Options Layer

- [x] Define strongly-typed option records in `Noctusoft.EzLocalKeyVault.Options` namespace:
  - `ElasticSearchOptions`
  - `JwtSettingsOptions`
  - `NotificationHubOptions`
  - `LocalKeyVaultOptions` – holds vault file path, reloadOnChange, redaction regex.
- [x] Add `IValidateOptions<T>` implementations to enforce mandatory properties.
- [x] Create `appsettings.schema.json` so editors can provide IntelliSense for placeholders.
- [x] Unit-test validation errors when required properties are missing.

### 2. Local Key Vault Core

- [x] Define `ILocalKeyVault` interface exposing:
  - `string? GetSecret(string key)`
  - `IDictionary<string,string> GetAll()`
  - `KeyVaultSubstitutionResult ApplySubstitutions(IConfigurationBuilder builder)`
- [x] Implement `LocalKeyVault` class:
  - Parse `.local-vault.json` into `ConcurrentDictionary<string,string>`.
  - Support two substitution syntaxes:
    1. **Token syntax** — `$(VARIABLE-NAME)` inside configuration values.
    2. **Property path override** — flat key `Logging:LogLevel:Default` present in vault file.
  - **Precedence order** (highest → lowest): CLI args → Environment variables → Vault overrides → Original config.
  - Watch the vault file with `FileSystemWatcher`; debounce 1 s; re-apply substitutions automatically.
  - Return a `KeyVaultSubstitutionResult` containing counts of total tokens, substituted tokens, and redacted secrets.
- [x] Add `SecretRedactor` helper:
  - Treat keys containing `password|secret|key|token` (case-insensitive) as secrets.
  - Mask values with `***` when logging.
- [x] Unit-tests:
  - Token resolution happy path / missing token fallback.
  - Property path overrides, including deep-nested JSON.
  - File reload behaviour with mocked file-system clock.

### 3. Configuration & DI Extensions

- [x] Provide `IConfigurationBuilder.AddEzLocalKeyVault(string vaultFilePath)`.
- [x] Provide `IServiceCollection.AddEzLocalKeyVault(this IConfiguration configuration)` to wire redaction & logging.
- [x] Ensure these extensions are **noop** when vault file is absent (e.g., in CI or prod).
- [x] Integration tests with `WebApplicationFactory<Program>` verifying the middleware.

### 4. Logging & Diagnostics

- [x] Add `ILogger<LocalKeyVault>` usage in core class.
- [x] Emit structured log with `EventId`:
  - `9001` — single substitution event (Debug).
  - `9002` — summary event after all substitutions (Information).
- [x] Ensure secrets are always redacted in logs; add guard tests.

### 5. CI/CD Pipeline (GitHub Actions)

- [x] `.github/workflows/build.yml` stages:
  1. **Setup**: `actions/setup-dotnet@v4` (SDK 8.0.x).
  2. **Restore**: `dotnet restore`.
  3. **Lint**: `dotnet format --verify-no-changes`.
  4. **Test**: `dotnet test --collect:"XPlat Code Coverage"`.
  5. **Pack**: `dotnet pack -c Release /p:IncludeSymbols=true`.
  6. **Upload Artifacts**: code coverage & NuGet package.
  7. **Publish** (main branch + tag): `dotnet nuget push` to NuGet.org (API key in GitHub secret).
- [x] Use `MinVer` for SemVer2 versioning driven by Git tags.

### 6. Documentation

- [x] `README.md` sections:
  1. **Motivation** – why local key vault.
  2. **Installation** – `dotnet add package Noctusoft.EzLocalKeyVault`.
  3. **Quick Start** – code snippet.
  4. **Configuration Reference** – table of options.
  5. **Security Notice** – redaction behaviour and limitations.
- [x] API surface documented with XML comments; enable `GenerateDocumentationFile` in csproj.
- [x] Provide `/samples/SampleApi` showcasing usage in ASP.NET Core 8.

### 7. Post-Release & Maintenance

- [x] Enable GitHub Discussions for Q&A.
- [x] Configure CodeQL analysis for security scanning.
- [x] Tri-age incoming issues weekly; maintain backlog labels (`bug`, `enhancement`, `good-first-issue`).

---

*End of extended developer checklist – v1.1*
