namespace WorkItems.Api.Domain.Enums;

public enum UserRole
{
    /// <summary>Read-only access — can view work items but cannot create or modify them.</summary>
    Viewer,

    /// <summary>Can create work items and edit their own items.</summary>
    Member,

    /// <summary>Full access — can create, edit, and delete any work item.</summary>
    Admin
}
