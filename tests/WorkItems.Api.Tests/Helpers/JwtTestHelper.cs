namespace WorkItems.Api.Tests.Helpers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Generates test JWTs with a specific role without touching the database.
/// Use this helper to test authorization policies in integration tests.
/// </summary>
public static class JwtTestHelper
{
    public const string SecretKey = "TestSecretKeyForJWTThatIsAtLeast32CharactersLong123456";
    public const string Issuer    = "WorkItemsApi";
    public const string Audience  = "WorkItemsApiUsers";

    /// <summary>
    /// Creates a signed JWT bearing the given role claim (e.g. "Admin", "Member", "Viewer").
    /// The token uses the same key/issuer/audience as the test WebApplicationFactory.
    /// </summary>
    public static string CreateToken(string role)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, $"testuser_{role.ToLower()}"),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
