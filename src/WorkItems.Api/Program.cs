using System.Text;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Formatting.Json;
using WorkItems.Api.Data;
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
    
    if (builder.Environment.IsProduction() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL")))
    {
        // Use PostgreSQL in production (Render provides DATABASE_URL in URI format)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")!;
        var npgsqlConnectionString = ConvertPostgresUrlToConnectionString(databaseUrl);
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

// Global exception handler - logs errors and returns ProblemDetails
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;
        
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "Unhandled exception occurred: {Message}", exception?.Message);
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new 
        { 
            error = "Internal server error", 
            message = app.Environment.IsDevelopment() ? exception?.Message : "An unexpected error occurred"
        });
    });
});

// Apply migrations automatically (skip in Test environment)
if (!app.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    if (app.Environment.IsProduction())
    {
        // For PostgreSQL: Use EnsureCreated to create tables with correct UUID types
        // This bypasses SQLite migrations which create TEXT columns for GUIDs
        var dbCreated = dbContext.Database.EnsureCreated();
        if (dbCreated)
        {
            app.Logger.LogInformation("PostgreSQL database created with proper UUID columns");
        }
    }
    else
    {
        // For SQLite in development: use migrations
        dbContext.Database.Migrate();
    }

    // Seed demo accounts (admin@demo.com + viewer@demo.com) — idempotent, safe on every boot
    await DatabaseSeeder.SeedAsync(dbContext);
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

// Skip HTTPS redirection in production (Render handles SSL termination)
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
