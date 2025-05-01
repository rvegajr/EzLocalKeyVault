using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Noctusoft.EzLocalKeyVault.Core;
using Noctusoft.EzLocalKeyVault.Extensions;
using Noctusoft.EzLocalKeyVault.Options;
using Xunit;

namespace Noctusoft.EzLocalKeyVault.Tests.Extensions;

public class ConfigurationExtensionsTests : IDisposable
{
    private readonly string _tempVaultFilePath;
    private readonly string _tempConfigFilePath;
    private bool _disposed;
    
    public ConfigurationExtensionsTests()
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
        
        // Create a temporary config file for testing
        _tempConfigFilePath = Path.GetTempFileName();
        File.WriteAllText(_tempConfigFilePath, @"{
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
            },
            ""EzLocalKeyVault"": {
                ""VaultFilePath"": """ + _tempVaultFilePath.Replace("\\", "\\\\") + @""",
                ""ReloadOnChange"": false,
                ""LogSubstitutions"": true,
                ""RedactionPatterns"": [""password"", ""secret"", ""key"", ""token""]
            }
        }");
    }
    
    [Fact]
    public void AddEzLocalKeyVault_ConfigurationBuilder_ShouldApplySubstitutions()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile(_tempConfigFilePath);
        
        // Act
        configBuilder.AddEzLocalKeyVault(_tempVaultFilePath, false);
        var config = configBuilder.Build();
        
        // Assert
        config["ElasticSearch:Environment"].Should().Be("integ");
        config["ElasticSearch:Uri"].Should().Be("https://elastic-cloud-endpoint.elastic-cloud.com:9243");
        config["ElasticSearch:Username"].Should().Be("elastic-user");
        config["ElasticSearch:Password"].Should().Be("elastic-password");
        config["JwtSettings:UsernameClaimName"].Should().Be("NEWVALUE");
        config["Logging:LogLevel:Default"].Should().Be("Error");
    }
    
    [Fact]
    public void AddEzLocalKeyVault_ServiceCollection_ShouldRegisterServices()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile(_tempConfigFilePath);
        var config = configBuilder.Build();
        
        var services = new ServiceCollection();
        
        // Add required logger services
        services.AddLogging(builder => builder.AddConsole());
        
        // Act
        services.AddEzLocalKeyVault(config);
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var keyVault = serviceProvider.GetService<ILocalKeyVault>();
        keyVault.Should().NotBeNull();
        keyVault.Should().BeOfType<LocalKeyVault>();
        
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<LocalKeyVaultOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.VaultFilePath.Should().Be(_tempVaultFilePath);
        options.ReloadOnChange.Should().BeFalse();
    }
    
    [Fact]
    public void AddEzLocalKeyVault_WithConfigureOptions_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add required logger services
        services.AddLogging(builder => builder.AddConsole());
        
        // Act
        services.AddEzLocalKeyVault(options => 
        {
            options.VaultFilePath = _tempVaultFilePath;
            options.ReloadOnChange = false;
        });
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var keyVault = serviceProvider.GetService<ILocalKeyVault>();
        keyVault.Should().NotBeNull();
        keyVault.Should().BeOfType<LocalKeyVault>();
        
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<LocalKeyVaultOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.VaultFilePath.Should().Be(_tempVaultFilePath);
        options.ReloadOnChange.Should().BeFalse();
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
            
            if (File.Exists(_tempConfigFilePath))
            {
                File.Delete(_tempConfigFilePath);
            }
        }
        
        _disposed = true;
    }
}
