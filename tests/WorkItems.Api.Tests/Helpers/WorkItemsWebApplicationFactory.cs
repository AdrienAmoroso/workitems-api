namespace WorkItems.Api.Tests.Helpers;

using System.Text;
using WorkItems.Api.Data;
using WorkItems.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Shared WebApplicationFactory for integration tests.
/// Uses an in-memory SQLite database and a predictable JWT configuration
/// so tests never depend on real secrets or a real database.
/// </summary>
public class WorkItemsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "TestDatabase_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"]        = JwtTestHelper.SecretKey,
                ["Jwt:Issuer"]           = JwtTestHelper.Issuer,
                ["Jwt:Audience"]         = JwtTestHelper.Audience,
                ["Jwt:ExpirationHours"]  = "24"
            }!);
        });

        builder.ConfigureServices(services =>
        {
            // Replace the real DbContext with an isolated in-memory instance per factory
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Replace the real event publisher with a spy so tests can assert published events
            // without requiring a real Azure Service Bus connection.
            services.AddSingleton<SpyEventPublisher>();
            services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<SpyEventPublisher>());

            // Override JWT validation to use the test key
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = JwtTestHelper.Issuer,
                    ValidAudience            = JwtTestHelper.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                  Encoding.UTF8.GetBytes(JwtTestHelper.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
