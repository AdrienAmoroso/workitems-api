namespace WorkItems.Api.Domain;

/// <summary>
/// Constants for ASP.NET Core authorization policy names.
/// Single source of truth shared by policy registration (Program.cs) and controller attributes.
/// </summary>
public static class AuthorizationPolicies
{
    public const string CanManageWorkItems = "CanManageWorkItems";
    public const string CanDeleteWorkItems = "CanDeleteWorkItems";
}
