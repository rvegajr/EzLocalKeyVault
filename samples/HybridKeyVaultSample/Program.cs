using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Noctusoft.EzAzureLocalKeyVault.Core;
using Noctusoft.EzAzureLocalKeyVault.Extensions;
using System.Text.Json;
using HybridKeyVaultSample.Models;

var builder = WebApplication.CreateBuilder(args);

// Add the hybrid key vault to the configuration
builder.Configuration.AddEzAzureLocalKeyVault();

// Register the hybrid key vault services
builder.Services.AddEzAzureLocalKeyVault(builder.Configuration);

// Register typed configuration
builder.Services.AddTypedConfiguration<AppSettings>(builder.Configuration, "");

var app = builder.Build();

app.MapGet("/", async (HttpContext context, IHybridKeyVault keyVault, IConfiguration config) =>
{
    var response = new
    {
        IsUsingLocalKeyVault = keyVault.IsUsingLocalKeyVault,
        IsUsingAzureKeyVault = keyVault.IsUsingAzureKeyVault,
        Environment = app.Environment.EnvironmentName,
        Secrets = new
        {
            ApiKey = keyVault.GetSecret("ApiKey"),
            ConnectionString = config.GetConnectionString("DefaultConnection"),
            ServiceUrl = config["ServiceUrl"]
        },
        Configuration = new
        {
            AzureKeyVaultUri = config["EzAzureLocalKeyVault:AzureKeyVaultUri"],
            UseLocalKeyVault = config["EzAzureLocalKeyVault:UseLocalKeyVault"],
            LocalKeyVaultEnvironments = config["EzAzureLocalKeyVault:LocalKeyVaultEnvironments"],
            FallbackToLocalKeyVault = config["EzAzureLocalKeyVault:FallbackToLocalKeyVault"],
            CacheAzureKeyVaultSecrets = config["EzAzureLocalKeyVault:CacheAzureKeyVaultSecrets"],
            CacheDurationMinutes = config["EzAzureLocalKeyVault:CacheDurationMinutes"]
        }
    };

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
    {
        WriteIndented = true
    }));
});

app.MapGet("/typed-config", async (HttpContext context, IOptions<AppSettings> options, IConfiguration config) =>
{
    // Get typed configuration using IOptions pattern
    var appSettings = options.Value;
    
    // Or get typed configuration directly from IConfiguration
    var directAppSettings = config.GetTypedSection<AppSettings>("");
    
    var response = new
    {
        FromIOptions = appSettings,
        FromDirectBinding = directAppSettings
    };
    
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
    {
        WriteIndented = true
    }));
});

app.MapGet("/connection-strings", async (HttpContext context, IConfiguration config) =>
{
    // Get just the connection strings section
    var connectionStrings = config.GetTypedSection<ConnectionStrings>("ConnectionStrings");
    
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(connectionStrings, new JsonSerializerOptions
    {
        WriteIndented = true
    }));
});

app.MapGet("/diagnostics", async (HttpContext context, IConfiguration config) =>
{
    var report = config.GenerateHybridKeyVaultDiagnosticReport();
    
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync(report);
});

app.MapGet("/refresh-cache", async (HttpContext context, IHybridKeyVault keyVault) =>
{
    await keyVault.RefreshCacheAsync();
    
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("Cache refreshed successfully");
});

app.Run();
