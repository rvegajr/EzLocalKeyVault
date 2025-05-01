using FluentAssertions;
using Noctusoft.EzLocalKeyVault.Options;
using Xunit;

namespace Noctusoft.EzLocalKeyVault.Tests.Options;

public class LocalKeyVaultOptionsTests
{
    [Fact]
    public void LocalKeyVaultOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new LocalKeyVaultOptions();
        
        // Assert
        options.VaultFilePath.Should().Be(".local-vault.json");
        options.ReloadOnChange.Should().BeTrue();
        options.LogSubstitutions.Should().BeTrue();
        options.RedactionPatterns.Should().Contain(new[] { "password", "secret", "key", "token" });
    }
    
    [Fact]
    public void LocalKeyVaultOptions_ShouldAllowCustomValues()
    {
        // Arrange & Act
        var options = new LocalKeyVaultOptions
        {
            VaultFilePath = "custom-vault.json",
            ReloadOnChange = false,
            LogSubstitutions = false,
            RedactionPatterns = new[] { "custom-pattern" }
        };
        
        // Assert
        options.VaultFilePath.Should().Be("custom-vault.json");
        options.ReloadOnChange.Should().BeFalse();
        options.LogSubstitutions.Should().BeFalse();
        options.RedactionPatterns.Should().ContainSingle().Which.Should().Be("custom-pattern");
    }
}
