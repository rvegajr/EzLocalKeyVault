using System.CommandLine;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Noctusoft.EzLocalKeyVault.Core;
using Noctusoft.EzLocalKeyVault.Extensions;
using Noctusoft.EzLocalKeyVault.Options;

namespace Noctusoft.EzLocalKeyVault.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Define command-line options
        var templateOption = new Option<FileInfo>(
            name: "--template",
            description: "The template file to process (JSON format)")
        {
            IsRequired = true
        };
        templateOption.AddAlias("-t");

        var vaultOption = new Option<FileInfo>(
            name: "--vault",
            description: "The key vault file containing secrets (JSON format)")
        {
            IsRequired = true
        };
        vaultOption.AddAlias("-v");

        var outputOption = new Option<string?>(
            name: "--output",
            description:
            "The output file path. If just a filename, writes to template directory. If full path, writes to that path. If omitted, outputs to console.");
        outputOption.AddAlias("-o");

        var rootCommand = new RootCommand("Process templates with EzLocalKeyVault substitutions");
        rootCommand.AddOption(templateOption);
        rootCommand.AddOption(vaultOption);
        rootCommand.AddOption(outputOption);

        rootCommand.SetHandler(async (template, vault, output) =>
        {
            try
            {
                await ProcessTemplate(template, vault, output);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, templateOption, vaultOption, outputOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task ProcessTemplate(FileInfo templateFile, FileInfo vaultFile, string? outputPath)
    {
        if (!templateFile.Exists)
        {
            throw new FileNotFoundException($"Template file not found: {templateFile.FullName}");
        }

        if (!vaultFile.Exists)
        {
            throw new FileNotFoundException($"Vault file not found: {vaultFile.FullName}");
        }

        // Set up logging
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<LocalKeyVault>();

        // Create configuration
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile(templateFile.FullName, optional: false, reloadOnChange: false);

        // Configure local key vault options
        var options = new LocalKeyVaultOptions
        {
            VaultFilePath = vaultFile.FullName,
            ReloadOnChange = false
        };

        // Create local key vault
        var keyVault = new LocalKeyVault(options, logger);

        // Apply substitutions
        var result = keyVault.ApplySubstitutions(configBuilder);
        var configuration = configBuilder.Build();

        // Convert configuration to JSON
        var json = SerializeConfigurationToJson(configuration);

        // Determine output destination
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            string fullOutputPath;

            // Check if outputPath is just a filename or a full path
            if (Path.IsPathRooted(outputPath))
            {
                // It's a full path, use as is
                fullOutputPath = outputPath;
            }
            else
            {
                // It's just a filename, put it in the same directory as the template
                fullOutputPath = Path.Combine(templateFile.DirectoryName!, outputPath);
            }

            // Create directory if it doesn't exist
            var outputDir = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Write to file
            await File.WriteAllTextAsync(fullOutputPath, json);
            Console.WriteLine($"Output written to {fullOutputPath}");
        }
        else
        {
            // Output to console
            Console.WriteLine(json);
        }

        // Log substitution summary
        Console.WriteLine(
            $"Substitution summary: {result.TotalTokens} tokens found, {result.SubstitutedTokens} substituted, {result.PropertyOverrides} property overrides");
    }

    private static string SerializeConfigurationToJson(IConfiguration configuration)
    {
        var sb = new StringBuilder();
        SerializeSection(configuration, sb, 0);
        return sb.ToString();
    }

    private static void SerializeSection(IConfiguration configuration, StringBuilder sb, int depth)
    {
        sb.AppendLine("{");

        var children = configuration.GetChildren().ToList();
        for (int i = 0; i < children.Count; i++)
        {
            var section = children[i];
            var isLast = i == children.Count - 1;

            // Indent
            sb.Append(new string(' ', (depth + 1) * 2));

            // Key
            sb.Append($"\"{section.Key}\": ");

            // Value or nested section
            var sectionChildren = section.GetChildren().ToList();
            if (sectionChildren.Any())
            {
                // Nested section
                SerializeSection(section, sb, depth + 1);
            }
            else
            {
                // Value
                var value = section.Value;
                sb.Append(value != null ? $"\"{value}\"" : "null");
            }

            // Comma if not last
            if (!isLast)
            {
                sb.Append(",");
            }

            sb.AppendLine();
        }

        // Closing brace
        sb.Append(new string(' ', depth * 2));
        sb.Append("}");

        // Don't add comma after the closing brace
    }
}