namespace TurfControlSystem.Domain.Entities;

/// <summary>
/// One cell of the Admin → Permissions matrix: whether <see cref="RoleId"/> can see
/// <see cref="PageId"/>. A missing row for a (role, page) pair means "not allowed" —
/// rows only need to exist for grants, which keeps a brand-new role locked down by
/// default until the admin explicitly ticks pages on for it.
/// </summary>
public class RolePagePermission : BaseEntity
{
    public int RoleId { get; set; }
    public AppRole? Role { get; set; }

    public int PageId { get; set; }
    public AppPage? Page { get; set; }

    public bool CanView { get; set; } = true;
}
