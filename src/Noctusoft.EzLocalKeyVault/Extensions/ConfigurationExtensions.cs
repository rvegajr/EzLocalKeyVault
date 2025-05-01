using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Noctusoft.EzLocalKeyVault.Core;
using Noctusoft.EzLocalKeyVault.Options;

namespace Noctusoft.EzLocalKeyVault.Extensions;

/// <summary>
/// Extension methods for <see cref="IConfigurationBuilder"/> and <see cref="IServiceCollection"/> to add local key vault support.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds a local key vault configuration source to the configuration builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="vaultFilePath">The path to the local vault file. If not specified, defaults to ".local-vault.json".</param>
    /// <param name="reloadOnChange">Whether to reload the configuration when the vault file changes. Defaults to true.</param>
    /// <returns>The configuration builder.</returns>
    public static IConfigurationBuilder AddEzLocalKeyVault(
        this IConfigurationBuilder builder,
        string? vaultFilePath = null,
        bool reloadOnChange = true)
    {
        // Create a temporary logger for initial setup
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<LocalKeyVault>();

        var options = new LocalKeyVaultOptions
        {
            VaultFilePath = vaultFilePath ?? ".local-vault.json",
            ReloadOnChange = reloadOnChange
        };

        var keyVault = new LocalKeyVault(options, logger);

        // Apply substitutions to the configuration
        keyVault.ApplySubstitutions(builder);

        return builder;
    }

    /// <summary>
    /// Adds local key vault services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEzLocalKeyVault(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind options from configuration
        services.Configure<LocalKeyVaultOptions>(configuration.GetSection("EzLocalKeyVault"));

        // Register the local key vault as a singleton
        services.AddSingleton<ILocalKeyVault>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LocalKeyVaultOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<LocalKeyVault>>();
            return new LocalKeyVault(options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds local key vault services to the service collection with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">A delegate to configure the options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEzLocalKeyVault(
        this IServiceCollection services,
        Action<LocalKeyVaultOptions> configureOptions)
    {
        // Configure options
        services.Configure<LocalKeyVaultOptions>(configureOptions);

        // Register the local key vault as a singleton
        services.AddSingleton<ILocalKeyVault>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LocalKeyVaultOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<LocalKeyVault>>();
            return new LocalKeyVault(options, logger);
        });

        return services;
    }
}