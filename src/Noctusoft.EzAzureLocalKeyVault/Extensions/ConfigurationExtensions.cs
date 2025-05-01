using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Noctusoft.EzAzureLocalKeyVault.Core;
using Noctusoft.EzAzureLocalKeyVault.Options;

namespace Noctusoft.EzAzureLocalKeyVault.Extensions;

/// <summary>
/// Extension methods for adding EzAzureLocalKeyVault to the configuration and service collection.
/// </summary>
/// <remarks>
/// <para>
/// This class provides extension methods for integrating the hybrid key vault service with ASP.NET Core
/// applications. The hybrid key vault service can automatically switch between Azure Key Vault and
/// local key vault based on the environment and configuration.
/// </para>
/// </remarks>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds the hybrid key vault configuration source to the configuration builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configureOptions">An optional action to configure the options.</param>
    /// <returns>The configuration builder.</returns>
    /// <remarks>
    /// <para>
    /// This method adds the hybrid key vault as a configuration source, which allows for key vault
    /// substitutions in your application's configuration. The hybrid key vault will automatically
    /// switch between Azure Key Vault and local key vault based on the environment and configuration.
    /// </para>
    /// <para>
    /// Configuration is read from the "EzAzureLocalKeyVault" section of your appsettings.json file.
    /// You can also provide additional configuration through the <paramref name="configureOptions"/> parameter.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// Basic usage:
    /// </para>
    /// <code>
    /// // In Program.cs
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Add the hybrid key vault to the configuration
    /// builder.Configuration.AddEzAzureLocalKeyVault();
    /// </code>
    /// <para>
    /// With custom options:
    /// </para>
    /// <code>
    /// // In Program.cs
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Add the hybrid key vault to the configuration with custom options
    /// builder.Configuration.AddEzAzureLocalKeyVault(options =>
    /// {
    ///     options.AzureKeyVaultUri = "https://my-key-vault.vault.azure.net/";
    ///     options.UseLocalKeyVault = builder.Environment.IsDevelopment();
    ///     options.FallbackToLocalKeyVault = true;
    /// });
    /// </code>
    /// </example>
    public static IConfigurationBuilder AddEzAzureLocalKeyVault(
        this IConfigurationBuilder builder,
        Action<EzAzureLocalKeyVaultOptions>? configureOptions = null)
    {
        // Create a temporary configuration to get the options
        var tempConfig = builder.Build();
        var options = new EzAzureLocalKeyVaultOptions();

        // Bind the options from the configuration
        tempConfig.GetSection("EzAzureLocalKeyVault").Bind(options);

        // Apply any custom configuration
        configureOptions?.Invoke(options);

        // Create a temporary logger for initial setup
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<HybridKeyVault>();

        // Create a temporary environment
        var environmentName = tempConfig["ASPNETCORE_ENVIRONMENT"] ?? tempConfig["DOTNET_ENVIRONMENT"] ?? "Production";
        var environment = new HostingEnvironment { EnvironmentName = environmentName };

        // Create the hybrid key vault
        var hybridKeyVault = new HybridKeyVault(
            Microsoft.Extensions.Options.Options.Create(options),
            environment,
            logger);

        // Apply substitutions
        hybridKeyVault.ApplySubstitutions(builder);

        return builder;
    }

    /// <summary>
    /// Adds the hybrid key vault services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureOptions">An optional action to configure the options.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the hybrid key vault service with the dependency injection container,
    /// allowing you to inject <see cref="IHybridKeyVault"/> into your services. The hybrid key vault
    /// will automatically switch between Azure Key Vault and local key vault based on the environment
    /// and configuration.
    /// </para>
    /// <para>
    /// Configuration is read from the "EzAzureLocalKeyVault" section of your appsettings.json file.
    /// You can also provide additional configuration through the <paramref name="configureOptions"/> parameter.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// Basic usage:
    /// </para>
    /// <code>
    /// // In Program.cs
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Register the hybrid key vault services
    /// builder.Services.AddEzAzureLocalKeyVault(builder.Configuration);
    /// 
    /// // Inject and use the service
    /// public class MyService
    /// {
    ///     private readonly IHybridKeyVault _keyVault;
    ///     
    ///     public MyService(IHybridKeyVault keyVault)
    ///     {
    ///         _keyVault = keyVault;
    ///     }
    ///     
    ///     public void DoSomething()
    ///     {
    ///         var secret = _keyVault.GetSecret("my-secret");
    ///         Console.WriteLine($"Secret: {secret}");
    ///     }
    /// }
    /// </code>
    /// <para>
    /// With custom options:
    /// </para>
    /// <code>
    /// // In Program.cs
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Register the hybrid key vault services with custom options
    /// builder.Services.AddEzAzureLocalKeyVault(builder.Configuration, options =>
    /// {
    ///     options.CacheAzureKeyVaultSecrets = true;
    ///     options.CacheDurationMinutes = 30;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEzAzureLocalKeyVault(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<EzAzureLocalKeyVaultOptions>? configureOptions = null)
    {
        // Configure options
        services.Configure<EzAzureLocalKeyVaultOptions>(configuration.GetSection("EzAzureLocalKeyVault"));

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register the hybrid key vault
        services.AddSingleton<IHybridKeyVault, HybridKeyVault>();

        // Also register as ILocalKeyVault for compatibility
        services.AddSingleton<Noctusoft.EzLocalKeyVault.Core.ILocalKeyVault>(sp =>
            sp.GetRequiredService<IHybridKeyVault>());

        return services;
    }

    /// <summary>
    /// Adds the hybrid key vault configuration source to the configuration builder and registers the services.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configureOptions">An optional action to configure the options.</param>
    /// <returns>The host builder.</returns>
    /// <remarks>
    /// <para>
    /// This method combines the functionality of <see cref="AddEzAzureLocalKeyVault(IConfigurationBuilder, Action{EzAzureLocalKeyVaultOptions})"/>
    /// and <see cref="AddEzAzureLocalKeyVault(IServiceCollection, IConfiguration, Action{EzAzureLocalKeyVaultOptions})"/>
    /// into a single method that can be used with the host builder.
    /// </para>
    /// <para>
    /// The hybrid key vault will automatically switch between Azure Key Vault and local key vault
    /// based on the environment and configuration. By default, it will use local key vault in development
    /// environments and Azure Key Vault in production environments.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// Basic usage:
    /// </para>
    /// <code>
    /// // In Program.cs
    /// Host.CreateDefaultBuilder(args)
    ///     .UseEzAzureLocalKeyVault()
    ///     .ConfigureWebHostDefaults(webBuilder =>
    ///     {
    ///         webBuilder.UseStartup&lt;Startup&gt;();
    ///     });
    /// </code>
    /// <para>
    /// With custom options:
    /// </para>
    /// <code>
    /// // In Program.cs
    /// Host.CreateDefaultBuilder(args)
    ///     .UseEzAzureLocalKeyVault((context, options) =>
    ///     {
    ///         options.AzureKeyVaultUri = "https://my-key-vault.vault.azure.net/";
    ///         options.UseLocalKeyVault = context.HostingEnvironment.IsDevelopment();
    ///         options.FallbackToLocalKeyVault = true;
    ///     })
    ///     .ConfigureWebHostDefaults(webBuilder =>
    ///     {
    ///         webBuilder.UseStartup&lt;Startup&gt;();
    ///     });
    /// </code>
    /// </example>
    public static IHostBuilder UseEzAzureLocalKeyVault(
        this IHostBuilder builder,
        Action<HostBuilderContext, EzAzureLocalKeyVaultOptions>? configureOptions = null)
    {
        // Add the configuration source
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddEzAzureLocalKeyVault(options =>
            {
                // Apply environment-specific settings
                options.UseLocalKeyVault = context.HostingEnvironment.IsDevelopment();

                // Apply custom configuration
                configureOptions?.Invoke(context, options);
            });
        });

        // Register the services
        builder.ConfigureServices((context, services) =>
        {
            services.AddEzAzureLocalKeyVault(context.Configuration,
                options => { configureOptions?.Invoke(context, options); });
        });

        return builder;
    }

    /// <summary>
    /// Gets a typed configuration section after key vault substitutions have been applied.
    /// </summary>
    /// <typeparam name="T">The type of the configuration section.</typeparam>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The name of the configuration section.</param>
    /// <returns>The typed configuration section.</returns>
    /// <remarks>
    /// <para>
    /// This method binds a configuration section to a strongly-typed class after key vault substitutions
    /// have been applied. This provides type safety and IntelliSense support when working with configuration values.
    /// </para>
    /// <para>
    /// The configuration section is identified by the <paramref name="sectionName"/> parameter. If the section
    /// does not exist, an empty instance of <typeparamref name="T"/> will be returned.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// First, define your configuration class:
    /// </para>
    /// <code>
    /// public class AppSettings
    /// {
    ///     public string ApiKey { get; set; }
    ///     public ConnectionStrings ConnectionStrings { get; set; }
    /// }
    /// 
    /// public class ConnectionStrings
    /// {
    ///     public string DefaultConnection { get; set; }
    /// }
    /// </code>
    /// <para>
    /// Then, use the method to get the typed configuration:
    /// </para>
    /// <code>
    /// // Get the AppSettings section
    /// var appSettings = configuration.GetTypedSection&lt;AppSettings&gt;("AppSettings");
    /// 
    /// // Use the strongly-typed configuration
    /// Console.WriteLine($"API Key: {appSettings.ApiKey}");
    /// Console.WriteLine($"Connection String: {appSettings.ConnectionStrings.DefaultConnection}");
    /// </code>
    /// </example>
    public static T GetTypedSection<T>(this IConfiguration configuration, string sectionName) where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        var result = new T();
        section.Bind(result);
        return result;
    }

    /// <summary>
    /// Gets a typed configuration section after key vault substitutions have been applied.
    /// </summary>
    /// <typeparam name="T">The type of the configuration section.</typeparam>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The typed configuration section.</returns>
    /// <remarks>
    /// <para>
    /// This method binds the root configuration to a strongly-typed class after key vault substitutions
    /// have been applied. This provides type safety and IntelliSense support when working with configuration values.
    /// </para>
    /// <para>
    /// This overload is useful when you want to bind the entire configuration to a single class, rather than
    /// just a specific section.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// First, define your configuration class:
    /// </para>
    /// <code>
    /// public class AppSettings
    /// {
    ///     public string ApiKey { get; set; }
    ///     public ConnectionStrings ConnectionStrings { get; set; }
    /// }
    /// 
    /// public class ConnectionStrings
    /// {
    ///     public string DefaultConnection { get; set; }
    /// }
    /// </code>
    /// <para>
    /// Then, use the method to get the typed configuration:
    /// </para>
    /// <code>
    /// // Get the entire configuration
    /// var appSettings = configuration.GetTypedSection&lt;AppSettings&gt;();
    /// 
    /// // Use the strongly-typed configuration
    /// Console.WriteLine($"API Key: {appSettings.ApiKey}");
    /// Console.WriteLine($"Connection String: {appSettings.ConnectionStrings.DefaultConnection}");
    /// </code>
    /// </example>
    public static T GetTypedSection<T>(this IConfiguration configuration) where T : class, new()
    {
        var result = new T();
        configuration.Bind(result);
        return result;
    }

    /// <summary>
    /// Adds a typed configuration section to the service collection after key vault substitutions have been applied.
    /// </summary>
    /// <typeparam name="T">The type of the configuration section.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The name of the configuration section.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>
    /// <para>
    /// This method registers a configuration section as a strongly-typed options object with the dependency injection
    /// container. The configuration section is identified by the <paramref name="sectionName"/> parameter.
    /// </para>
    /// <para>
    /// After registering, you can inject the options using the <c>IOptions&lt;T&gt;</c> pattern in your services.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// First, define your configuration class:
    /// </para>
    /// <code>
    /// public class AppSettings
    /// {
    ///     public string ApiKey { get; set; }
    ///     public ConnectionStrings ConnectionStrings { get; set; }
    /// }
    /// 
    /// public class ConnectionStrings
    /// {
    ///     public string DefaultConnection { get; set; }
    /// }
    /// </code>
    /// <para>
    /// Then, register the typed configuration:
    /// </para>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// services.AddTypedConfiguration&lt;AppSettings&gt;(configuration, "AppSettings");
    /// </code>
    /// <para>
    /// Finally, inject and use the typed configuration in your services:
    /// </para>
    /// <code>
    /// public class MyService
    /// {
    ///     private readonly AppSettings _settings;
    ///     
    ///     public MyService(IOptions&lt;AppSettings&gt; options)
    ///     {
    ///         _settings = options.Value;
    ///     }
    ///     
    ///     public void DoSomething()
    ///     {
    ///         Console.WriteLine($"API Key: {_settings.ApiKey}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddTypedConfiguration<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName) where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        services.Configure<T>(section);
        return services;
    }

    /// <summary>
    /// Adds a typed configuration section to the service collection after key vault substitutions have been applied
    /// and returns the bound instance for immediate use.
    /// </summary>
    /// <typeparam name="T">The type of the configuration section.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The name of the configuration section.</param>
    /// <returns>The bound configuration instance.</returns>
    /// <remarks>
    /// <para>
    /// This method registers a configuration section as a strongly-typed options object with the dependency injection
    /// container and returns the bound instance for immediate use. The configuration section is identified by the
    /// <paramref name="sectionName"/> parameter.
    /// </para>
    /// <para>
    /// This is useful when you need to access the configuration values during application startup, before the
    /// dependency injection container is built.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// First, define your configuration class:
    /// </para>
    /// <code>
    /// public class AppSettings
    /// {
    ///     public string ApiKey { get; set; }
    ///     public ConnectionStrings ConnectionStrings { get; set; }
    /// }
    /// 
    /// public class ConnectionStrings
    /// {
    ///     public string DefaultConnection { get; set; }
    /// }
    /// </code>
    /// <para>
    /// Then, register the typed configuration and get the bound instance:
    /// </para>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// var appSettings = services.AddAndGetTypedConfiguration&lt;AppSettings&gt;(configuration, "AppSettings");
    /// 
    /// // Use the settings immediately
    /// Console.WriteLine($"API Key: {appSettings.ApiKey}");
    /// 
    /// // The settings are also registered with the DI container for injection
    /// </code>
    /// </example>
    public static T AddAndGetTypedConfiguration<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName) where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        var result = new T();
        section.Bind(result);
        services.Configure<T>(section);
        return result;
    }

    private class HostingEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private class NullFileProvider : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath) => new NullDirectoryContents();
        public IFileInfo GetFileInfo(string subpath) => new NullFileInfo();
        public Microsoft.Extensions.Primitives.IChangeToken Watch(string filter) => new NullChangeToken();

        private class NullDirectoryContents : IDirectoryContents
        {
            public bool Exists => false;
            public IEnumerator<IFileInfo> GetEnumerator() => Enumerable.Empty<IFileInfo>().GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class NullFileInfo : IFileInfo
        {
            public bool Exists => false;
            public long Length => 0;
            public string? PhysicalPath => null;
            public string Name => string.Empty;
            public DateTimeOffset LastModified => DateTimeOffset.MinValue;
            public bool IsDirectory => false;
            public Stream CreateReadStream() => Stream.Null;
        }

        private class NullChangeToken : Microsoft.Extensions.Primitives.IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new NullDisposable();

            private class NullDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}