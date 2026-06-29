// Load .env file from workspace root before building configuration so that
// Azure OpenAI / Azure Search / Blob Storage credentials are picked up
// without needing to set system-level environment variables manually.
// Real environment variables always take precedence over .env values.
DotEnvLoader.Load(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DevAssistDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch{ }
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapControllers();
app.MapHealthChecks("/health");

// Development: Microsoft.AspNetCore.SpaProxy (launchSettings) starts Vite and proxies UI to this port.
// Production / publish: serve built SPA from wwwroot or dist folder.
if (!app.Environment.IsDevelopment())
{
    var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    var spaDistPath = Directory.Exists(wwwrootPath)
        ? wwwrootPath
        : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "frontend", "devassist-ui", "dist"));

    if (Directory.Exists(spaDistPath))
    {
        var spaFiles = new PhysicalFileProvider(spaDistPath);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = spaFiles });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = spaFiles });
        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = spaFiles });
    }
}

app.Run();

// Walks up the directory tree from startDir to find and load a .env file.
// Keys use ASP.NET Core double-underscore convention (e.g. AzureOpenAi__ApiKey).
// Existing environment variables are never overwritten.
static class DotEnvLoader
{
    public static void Load(string startDirectory)
    {
        var envFile = FindFile(new DirectoryInfo(startDirectory), ".env");
        if (envFile is null) return;

        foreach (var line in File.ReadAllLines(envFile))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
            var idx = trimmed.IndexOf('=');
            if (idx <= 0) continue;
            var key = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim();
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
        }
    }

    private static string? FindFile(DirectoryInfo? dir, string fileName)
    {
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
