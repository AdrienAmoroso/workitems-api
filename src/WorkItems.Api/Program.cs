using System.Text;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Formatting.Json;
using WorkItems.Api.Data;
using WorkItems.Api.Middleware;
using WorkItems.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Serilog: structured JSON logging — replaces the default ASP.NET Core logger.
// JsonFormatter writes one JSON object per log event, suitable for log aggregators.
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(new JsonFormatter()));

// Database configuration: PostgreSQL for production, SQLite for development
if (!builder.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (builder.Environment.IsProduction())
    {
        // Use PostgreSQL in production.
        // Render provides DATABASE_URL in URI format; Azure provides the connection string directly.
        string npgsqlConnectionString;
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(databaseUrl))
        {
            npgsqlConnectionString = ConvertPostgresUrlToConnectionString(databaseUrl);
        }
        else
        {
            npgsqlConnectionString = connectionString!;
        }
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(npgsqlConnectionString)
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
    }
    else
    {
        // Use SQLite in development
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));
    }
}

// Helper function to convert Render's DATABASE_URL to Npgsql connection string
static string ConvertPostgresUrlToConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');
    
    // Add connection pooling and timeout settings for Render free tier
    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=20;Connection Idle Lifetime=300;Connection Pruning Interval=10;Timeout=30;Command Timeout=30";
}

builder.Services.AddScoped<IWorkItemService, WorkItemService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Application Insights: telemetry (requests, exceptions, dependencies) sent to Azure Monitor.
// Connection string is injected via APPLICATIONINSIGHTS_CONNECTION_STRING in Azure App Settings.
// Skipped in Test environment — the background telemetry worker breaks WebApplicationFactory.
// Skipped when the connection string is absent — avoids hard startup crash on missing config.
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
if (!builder.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase)
    && !string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? "TestSecretKeyForJWTThatIsAtLeast32CharactersLong123456";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    // ADR-01: Authorization via named Policies, not raw role strings.
    // CanManageWorkItems — Members and Admins can create and edit work items.
    options.AddPolicy("CanManageWorkItems", policy =>
        policy.RequireRole("Member", "Admin"));

    // CanDeleteWorkItems — Admins only can delete work items.
    options.AddPolicy("CanDeleteWorkItems", policy =>
        policy.RequireRole("Admin"));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Health checks: /health (liveness) and /health/ready (readiness + DB probe)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(name: "database", tags: ["ready"]);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Work Items API",
        Version     = "v1",
        Description = """
            A lightweight work items manager (Jira-like) built with ASP.NET Core 10.

            ## Roles & Permissions

            | Role    | GET | POST (create) | PUT (update) | DELETE |
            |---------|-----|---------------|--------------|--------|
            | Viewer  | ✅  | ❌            | ❌           | ❌     |
            | Member  | ✅  | ✅            | ✅           | ❌     |
            | Admin   | ✅  | ✅            | ✅           | ✅     |

            ## Demo Accounts

            | Email              | Password     | Role   |
            |--------------------|--------------|--------|
            | admin@demo.com     | Admin1234!   | Admin  |
            | viewer@demo.com    | Viewer1234!  | Viewer |

            Login via `POST /api/auth/login`, then click **Authorize** and paste the returned token.
            """
    });

    // Declare the JWT Bearer security scheme so the Swagger UI "Authorize" button works
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Paste the JWT token returned by /api/auth/login. Example: `eyJhbGci...`"
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });

    // Include XML doc comments generated from /// summaries on controllers
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// Add CORS for Angular frontend
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ADR-06: Global exception middleware — logs via Serilog, returns ProblemDetails (RFC 7807).
// Must be first in the pipeline so every downstream exception is caught.
app.UseMiddleware<GlobalExceptionMiddleware>();

// Apply migrations automatically (skip in Test environment)
if (!app.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    if (app.Environment.IsProduction())
    {
        // For PostgreSQL: Use EnsureCreated to create tables with correct UUID types
        // This bypasses SQLite migrations which create TEXT columns for GUIDs
        try
        {
            var dbCreated = dbContext.Database.EnsureCreated();
            if (dbCreated)
            {
                app.Logger.LogInformation("PostgreSQL database created with proper UUID columns");
            }
            else
            {
                app.Logger.LogInformation("PostgreSQL database already exists");
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Failed to initialize PostgreSQL database on startup. The app will continue but may be unhealthy until the database is reachable.");
        }
    }
    else
    {
        // For SQLite in development: use migrations
        dbContext.Database.Migrate();
    }

    // Seed demo accounts (admin@demo.com + viewer@demo.com) — idempotent, safe on every boot
    try
    {
        await DatabaseSeeder.SeedAsync(dbContext);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to seed database on startup.");
    }
}

// CORS must be first to handle preflight requests
app.UseCors("AllowAngularApp");

// Enable Swagger in all environments for demo purposes
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Work Items API v1");
    options.RoutePrefix = "swagger";
});

// Skip HTTPS redirection in production (Azure App Service handles SSL termination at the reverse proxy)
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Liveness: fast ping, no external dependency check.
// Readiness: includes the DB probe — tagged "ready" — used by load balancers before routing traffic.
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
