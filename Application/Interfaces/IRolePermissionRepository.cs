namespace TurfControlSystem.Application.Interfaces;

public interface IRolePermissionRepository
{
    /// <summary>Every permission row for a role, keyed for fast page lookups.</summary>
    Task<List<RolePagePermission>> GetForRoleAsync(int roleId);

    /// <summary>The whole matrix at once — used to render the Admin → Permissions grid.</summary>
    Task<List<RolePagePermission>> GetAllAsync();

    /// <summary>Replaces every permission row for a role with the given set of allowed page IDs.</summary>
    Task SetRolePermissionsAsync(int roleId, IEnumerable<int> allowedPageIds);
}
