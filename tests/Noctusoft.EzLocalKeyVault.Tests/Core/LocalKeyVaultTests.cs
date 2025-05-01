using System;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Noctusoft.EzLocalKeyVault.Core;
using Noctusoft.EzLocalKeyVault.Options;
using Xunit;

namespace Noctusoft.EzLocalKeyVault.Tests.Core;

public class LocalKeyVaultTests : IDisposable
{
    private readonly string _tempVaultFilePath;
    private readonly LocalKeyVaultOptions _options;
    private readonly ILogger<LocalKeyVault> _logger;
    private bool _disposed;

    public LocalKeyVaultTests()
    {
        // Create a temporary vault file for testing
        _tempVaultFilePath = Path.GetTempFileName();
        File.WriteAllText(_tempVaultFilePath, @"{
            ""APA-Platform-Prefix"": ""integ"",
            ""APA-ElasticCloud-Logging-URI"": ""https://elastic-cloud-endpoint.elastic-cloud.com:9243"",
            ""APA-ELASTIC-CLOUD-USERNAME"": ""elastic-user"",
            ""APA-ELASTIC-CLOUD-PASSWORD"": ""elastic-password"",
            ""JwtSettings:UsernameClaimName"": ""NEWVALUE"",
            ""Logging:LogLevel:Default"": ""Error""
        }");
        
        _options = new LocalKeyVaultOptions
        {
            VaultFilePath = _tempVaultFilePath,
            ReloadOnChange = false // Disable for testing
        };
        
        // Create a mock logger
        _logger = new TestLogger<LocalKeyVault>();
    }
    
    [Fact]
    public void GetSecret_ShouldReturnCorrectValue()
    {
        // Arrange
        var keyVault = new LocalKeyVault(_options, _logger);
        
        // Act
        var result = keyVault.GetSecret("APA-Platform-Prefix");
        
        // Assert
        result.Should().Be("integ");
    }
    
    [Fact]
    public void GetSecret_ShouldReturnNullForNonExistentKey()
    {
        // Arrange
        var keyVault = new LocalKeyVault(_options, _logger);
        
        // Act
        var result = keyVault.GetSecret("NonExistentKey");
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public void GetAll_ShouldReturnAllSecrets()
    {
        // Arrange
        var keyVault = new LocalKeyVault(_options, _logger);
        
        // Act
        var result = keyVault.GetAll();
        
        // Assert
        result.Should().ContainKey("APA-Platform-Prefix").WhoseValue.Should().Be("integ");
        result.Should().ContainKey("APA-ElasticCloud-Logging-URI").WhoseValue.Should().Be("https://elastic-cloud-endpoint.elastic-cloud.com:9243");
        result.Should().ContainKey("APA-ELASTIC-CLOUD-USERNAME").WhoseValue.Should().Be("elastic-user");
        result.Should().ContainKey("APA-ELASTIC-CLOUD-PASSWORD").WhoseValue.Should().Be("elastic-password");
        result.Should().ContainKey("JwtSettings:UsernameClaimName").WhoseValue.Should().Be("NEWVALUE");
        result.Should().ContainKey("Logging:LogLevel:Default").WhoseValue.Should().Be("Error");
    }
    
    [Fact]
    public void ApplySubstitutions_ShouldReplaceTokensInConfiguration()
    {
        // Arrange
        var keyVault = new LocalKeyVault(_options, _logger);
        var configJson = @"{
            ""AllowedHosts"": ""*"",
            ""ElasticSearch"": {
                ""Environment"": ""$(APA-Platform-Prefix)"",
                ""Uri"": ""$(APA-ElasticCloud-Logging-URI)"",
                ""Username"": ""$(APA-ELASTIC-CLOUD-USERNAME)"",
                ""Password"": ""$(APA-ELASTIC-CLOUD-PASSWORD)""
            },
            ""JwtSettings"": {
                ""UsernameClaimName"": ""apaempid""
            },
            ""Logging"": {
                ""LogLevel"": {
                    ""Default"": ""Information""
                }
            }
        }";
        
        var configPath = Path.GetTempFileName();
        File.WriteAllText(configPath, configJson);
        
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile(configPath);
        
        // Act
        var result = keyVault.ApplySubstitutions(configBuilder);
        var config = configBuilder.Build();
        
        // Assert
        result.TotalTokens.Should().Be(4);
        result.SubstitutedTokens.Should().Be(4);
        result.PropertyOverrides.Should().Be(2);
        
        config["ElasticSearch:Environment"].Should().Be("integ");
        config["ElasticSearch:Uri"].Should().Be("https://elastic-cloud-endpoint.elastic-cloud.com:9243");
        config["ElasticSearch:Username"].Should().Be("elastic-user");
        config["ElasticSearch:Password"].Should().Be("elastic-password");
        config["JwtSettings:UsernameClaimName"].Should().Be("NEWVALUE");
        config["Logging:LogLevel:Default"].Should().Be("Error");
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Clean up temporary files
            if (File.Exists(_tempVaultFilePath))
            {
                File.Delete(_tempVaultFilePath);
            }
        }

        _disposed = true;
    }
}

// Simple test logger implementation
public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
