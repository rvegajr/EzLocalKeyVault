using Microsoft.Extensions.Configuration;
using Noctusoft.EzLocalKeyVault.Extensions;

namespace ConfigurationSample;

public static class ConfigurationHelper
{
    private static bool IsIDE => Environment.GetEnvironmentVariable("VSCODE_PID") != null || 
                                Environment.GetEnvironmentVariable("JETBRAINS_IDE") != null;
    
    private static bool IsDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != null;
    
    private static string GetEnvironmentName() => 
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? 
        "Production";

    /// <summary>
    /// Configures environment-specific settings in the configuration builder
    /// </summary>
    /// <param name="configBuilder">The configuration builder to configure</param>
    public static void ConfigureEnvironmentSpecificSettings(ConfigurationBuilder configBuilder)
    {
        // Base configuration is always loaded
        configBuilder.SetBasePath(Directory.GetCurrentDirectory());
        configBuilder.AddJsonFile("appsettings.json", false, true);

        var environmentName = GetEnvironmentName();
        var isDevelopment = environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase);

        // Detect and load environment-specific settings
        if (IsIDE)
        {
            Console.WriteLine("Detected IDE environment");
            configBuilder.AddJsonFile("appsettings.IDE.json", true, true);
            configBuilder.ResolveFromLocalKeyvault();
        }
        else if (IsDocker)
        {
            Console.WriteLine("Detected Docker/Container environment");
            configBuilder.AddJsonFile("appsettings.Local.json", true, true);
            configBuilder.ResolveFromLocalKeyvault();
        }
        else
        {
            Console.WriteLine($"Using standard environment: {environmentName}");
            configBuilder.AddJsonFile($"appsettings.{environmentName}.json", true, true);
            
            // In development, we also want to use local key vault
            if (isDevelopment)
            {
                configBuilder.ResolveFromLocalKeyvault();
            }
        }

        // Always add environment variables last for overrides
        configBuilder.AddEnvironmentVariables();
    }
}
