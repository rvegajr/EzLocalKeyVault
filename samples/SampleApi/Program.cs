using Noctusoft.EzLocalKeyVault.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add local key vault to configuration
builder.Configuration.AddEzLocalKeyVault();

// Add local key vault services
builder.Services.AddEzLocalKeyVault(builder.Configuration);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Add an endpoint to show the configuration values
app.MapGet("/config", (IConfiguration configuration) =>
{
    var config = new
    {
        ElasticSearch = new
        {
            Environment = configuration["ElasticSearch:Environment"],
            Uri = configuration["ElasticSearch:Uri"],
            Username = configuration["ElasticSearch:Username"],
            // Don't expose the password in the response
            Password = "***REDACTED***"
        },
        JwtSettings = new
        {
            UsernameClaimName = configuration["JwtSettings:UsernameClaimName"],
            Authority = configuration["JwtSettings:Authority"],
            MetadataAddress = configuration["JwtSettings:MetadataAddress"],
            ValidIssuer = configuration["JwtSettings:ValidIssuer"]
        },
        ConnectionStrings = new
        {
            // Don't expose the full connection string in the response
            MobileDB = "***REDACTED***"
        }
    };
    
    return config;
})
.WithName("GetConfig")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
