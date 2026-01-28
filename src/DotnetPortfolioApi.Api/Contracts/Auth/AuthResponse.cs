namespace DotnetPortfolioApi.Api.Contracts.Auth;

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
