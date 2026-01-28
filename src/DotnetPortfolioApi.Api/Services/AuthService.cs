namespace DotnetPortfolioApi.Api.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using DotnetPortfolioApi.Api.Contracts.Auth;
using DotnetPortfolioApi.Api.Data;
using DotnetPortfolioApi.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(string username, string email, string password)
    {
        // Check if username already exists
        if (await _dbContext.Users.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException("Username already exists");

        // Check if email already exists
        if (await _dbContext.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Email already exists");

        // Hash password with BCrypt
        var passwordHash = BCrypt.HashPassword(password);

        // Create user
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            ExpiresAt = expiresAt
        };
    }

    public async Task<AuthResponse> LoginAsync(string usernameOrEmail, string password)
    {
        // Find user by username or email
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid username/email or password");

        // Verify password
        if (!BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username/email or password");

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            ExpiresAt = expiresAt
        };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "DotNetPortfolioApi";
        var audience = jwtSettings["Audience"] ?? "DotNetPortfolioApiUsers";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetTokenExpirationHours()
    {
        var expirationHours = _configuration.GetValue<int?>("Jwt:ExpirationHours");
        return expirationHours ?? 24; // Default 24 hours
    }
}
