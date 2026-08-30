using System.Text;
using System.Threading.RateLimiting;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using DevAssist.Api.Services;
using DevAssist.Application.Interfaces.Auth;
using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

// Load .env file from workspace root before building configuration so that
// Azure OpenAI / Azure Search / Blob Storage credentials are picked up
// without needing to set system-level environment variables manually.
// Real environment variables always take precedence over .env values.
DotEnvLoader.Load(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

// Azure Key Vault — add as early config provider so all subsequent reads benefit.
var kvUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(kvUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(kvUri), new Azure.Identity.DefaultAzureCredential());
}

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// CORS — origins from config; falls back to localhost dev defaults.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "https://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Rate limiting — fixed-window per remote IP.
var generalPermits = builder.Configuration.GetValue<int>("RateLimiting:GeneralPermits", 100);
var aiPermits = builder.Configuration.GetValue<int>("RateLimiting:AiPermits", 20);
var windowSeconds = builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);

builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opt.AddFixedWindowLimiter("general", limiterOpt =>
    {
        limiterOpt.PermitLimit = generalPermits;
        limiterOpt.Window = TimeSpan.FromSeconds(windowSeconds);
        limiterOpt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOpt.QueueLimit = 0;
    });

    opt.AddFixedWindowLimiter("ai", limiterOpt =>
    {
        limiterOpt.PermitLimit = aiPermits;
        limiterOpt.Window = TimeSpan.FromSeconds(windowSeconds);
        limiterOpt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOpt.QueueLimit = 0;
    });
});

// Application Insights — registered when connection string is present.
var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
        options.ConnectionString = aiConnectionString);
}

// ── Local JWT authentication ──────────────────────────────────────────────────
// When Jwt:Secret is present, enable local username/password JWT auth.
// Microsoft Entra ID is kept as a secondary scheme when AzureAd:TenantId is set.
var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtEnabled = !string.IsNullOrWhiteSpace(jwtSecret);
var tenantId = builder.Configuration["AzureAd:TenantId"];
var entraEnabled = !string.IsNullOrWhiteSpace(tenantId);

if (jwtEnabled || entraEnabled)
{
    var authBuilder = builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });

    if (jwtEnabled)
    {
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "devassist";
        var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "devassist";

        authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),
                NameClaimType = "unique_name",
                RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            };
        });
    }
    else if (entraEnabled)
    {
        authBuilder.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
    }

    builder.Services.AddAuthorization();
}

var authEnabled = jwtEnabled || entraEnabled;

builder.Services.AddControllers(options =>
    {
        if (authEnabled)
            options.Filters.Add(new AuthorizeFilter(
                new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build()));
    })
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await TryMigrateAndSeedAsync(app, jwtEnabled);

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
// CORS must come before HTTPS redirect — browsers do not follow 307 redirects for CORS preflight.
app.UseCors("Frontend");
// Only redirect HTTP → HTTPS in production. In development the Vite proxy talks HTTP to the
// local API and a 307 redirect breaks the proxy chain, causing CORS errors in the browser.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health").RequireCors("Frontend");

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

// Runs EF migrations and admin seeding without crashing the host if the database
// is temporarily unavailable (e.g. Docker not started yet).
static async Task TryMigrateAndSeedAsync(WebApplication app, bool jwtEnabled)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var dbContext = sp.GetRequiredService<DevAssistDbContext>();
        await dbContext.Database.MigrateAsync();

        if (!jwtEnabled) return;

        var userRepo = sp.GetRequiredService<IUserRepository>();
        var hasher  = sp.GetRequiredService<IPasswordHasher>();

        if (await userRepo.AnyAsync(CancellationToken.None)) return;

        var defaultPassword = app.Configuration["Jwt:DefaultAdminPassword"] ?? "Admin@123!";
        var (hash, salt) = hasher.HashPassword(defaultPassword);
        await userRepo.CreateAsync(new AppUser
        {
            Id          = Guid.NewGuid(),
            Username    = "admin",
            DisplayName = "Administrator",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role        = UserRole.Admin,
            IsActive    = true,
            CreatedAt   = DateTimeOffset.UtcNow,
        }, CancellationToken.None);

        app.Logger.LogWarning(
            "Default admin created — username: admin  password: {Password}  Change it immediately.",
            defaultPassword);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(
            "Database unavailable at startup — migrations and seeding skipped: {Message}", ex.Message);
    }
}

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
