namespace WorkItems.Api.Data;

using BCrypt.Net;
using WorkItems.Api.Domain;
using WorkItems.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Seeds demo accounts on startup so the live demo is usable without manual setup.
/// Accounts are only inserted if they do not already exist — safe to run on every boot.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Demo credentials (shown in the README and Swagger description):
    /// - admin@demo.com  / Admin1234!  — Admin role (full access)
    /// - viewer@demo.com / Viewer1234! — Viewer role (read-only)
    /// </summary>
    public static async Task SeedAsync(AppDbContext dbContext)
    {
        await SeedUserAsync(dbContext,
            username: "admin",
            email: "admin@demo.com",
            password: "Admin1234!",
            role: UserRole.Admin);

        await SeedUserAsync(dbContext,
            username: "viewer",
            email: "viewer@demo.com",
            password: "Viewer1234!",
            role: UserRole.Viewer);
    }

    private static async Task SeedUserAsync(
        AppDbContext dbContext,
        string username,
        string email,
        string password,
        UserRole role)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is not null)
        {
            // Correct the role if the account exists with the wrong one — guards against stale
            // data from earlier deploys where the default UserRole.Member was persisted.
            if (user.Role != role)
            {
                user.Role = role;
                await dbContext.SaveChangesAsync();
            }
            return;
        }

        dbContext.Users.Add(new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.HashPassword(password),
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
}
