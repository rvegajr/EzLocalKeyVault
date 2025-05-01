using Microsoft.Extensions.Configuration;

namespace ConfigurationSample;

public class Program
{
    public static void Main(string[] args)
    {
        // Create the configuration builder
        var configBuilder = new ConfigurationBuilder();
        
        // Configure environment-specific settings
        ConfigurationHelper.ConfigureEnvironmentSpecificSettings(configBuilder);
        
        // Build the configuration
        var configuration = configBuilder.Build();
        
        // Example: Read a value that might be substituted from key vault
        var connectionString = configuration["ConnectionStrings:DefaultConnection"];
        Console.WriteLine($"Connection String: {connectionString}");
        
        // Example: Read another value
        var apiKey = configuration["ApiKey"];
        Console.WriteLine($"API Key: {apiKey}");
    }
}
