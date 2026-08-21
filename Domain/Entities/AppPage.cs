namespace TurfControlSystem.Domain.Entities;

/// <summary>
/// One entry in the catalog of pages that can be shown/hidden per role (the rows of the
/// Admin → Permissions matrix). <see cref="Key"/> is the stable identifier every page
/// component checks itself against via &lt;PagePermissionGuard PageKey="..."&gt; — it never
/// changes even if <see cref="DisplayName"/> or <see cref="Route"/> is edited.
/// </summary>
public class AppPage : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public List<RolePagePermission> Permissions { get; set; } = new();
}
