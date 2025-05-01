using FluentAssertions;
using Noctusoft.EzLocalKeyVault.Core;
using Xunit;

namespace Noctusoft.EzLocalKeyVault.Tests.Core;

public class SecretRedactorTests
{
    [Theory]
    [InlineData("password", true)]
    [InlineData("UserPassword", true)]
    [InlineData("user_password", true)]
    [InlineData("secret", true)]
    [InlineData("api-secret", true)]
    [InlineData("MySecret123", true)]
    [InlineData("key", true)]
    [InlineData("ApiKey", true)]
    [InlineData("token", true)]
    [InlineData("auth-token", true)]
    [InlineData("username", false)]
    [InlineData("email", false)]
    [InlineData("config", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSecret_ShouldIdentifySecretsCorrectly(string key, bool expectedResult)
    {
        // Arrange
        var patterns = new[] { "password", "secret", "key", "token" };
        
        // Act
        var result = SecretRedactor.IsSecret(key, patterns);
        
        // Assert
        result.Should().Be(expectedResult);
    }
    
    [Theory]
    [InlineData("password", "sensitive-value", "***")]
    [InlineData("api-key", "1234567890", "***")]
    [InlineData("username", "john.doe", "john.doe")]
    [InlineData("email", "john@example.com", "john@example.com")]
    public void RedactIfSecret_ShouldRedactSecretsOnly(string key, string value, string expectedResult)
    {
        // Arrange
        var patterns = new[] { "password", "secret", "key", "token" };
        
        // Act
        var result = SecretRedactor.RedactIfSecret(key, value, patterns);
        
        // Assert
        result.Should().Be(expectedResult);
    }
}
