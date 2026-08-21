namespace TurfControlSystem.Domain.Entities;

/// <summary>
/// A role that users are assigned (Admin, Operator, Viewer, or any custom role the admin
/// creates from the Admin → Roles page). Page-level access is controlled entirely through
/// <see cref="RolePagePermission"/> rows, not hard-coded per role — the one exception is
/// the "Admin" system role, which always has full access (see PermissionService) so an
/// admin can never accidentally lock themselves out via a permission-matrix mistake.
/// </summary>
public class AppRole : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>True only for the built-in "Admin" role — protects it from being renamed or deleted.</summary>
    public bool IsSystemRole { get; set; }

    public List<AppUser> Users { get; set; } = new();
    public List<RolePagePermission> Permissions { get; set; } = new();
}
