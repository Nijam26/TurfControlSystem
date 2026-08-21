namespace TurfControlSystem.Application.Services;

/// <summary>
/// The single source of truth for "can this role see this page" — used by both
/// PagePermissionGuard (to gate page content) and NavMenu (to hide links the current
/// user can't open anyway). The "Admin" role always passes, regardless of what's in the
/// RolePagePermission table, so a permission-matrix mistake can never lock every admin out.
/// </summary>
public class PermissionService
{
    public const string AdminRoleName = "Admin";

    private readonly IAppPageRepository _pageRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IRolePermissionRepository _permissionRepo;

    public PermissionService(IAppPageRepository pageRepo, IRoleRepository roleRepo, IRolePermissionRepository permissionRepo)
    {
        _pageRepo = pageRepo;
        _roleRepo = roleRepo;
        _permissionRepo = permissionRepo;
    }

    public async Task<bool> CanAccessAsync(string? roleName, string pageKey)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return false;

        if (roleName == AdminRoleName)
            return true;

        var role = await _roleRepo.GetByNameAsync(roleName);
        if (role is null)
            return false;

        var allowed = await _permissionRepo.GetForRoleAsync(role.Id);
        return allowed.Any(p => p.Page?.Key == pageKey);
    }

    /// <summary>Pages this role can see, in nav order — Admin gets every page automatically.</summary>
    public async Task<List<AppPage>> GetAllowedPagesAsync(string? roleName)
    {
        var allPages = await _pageRepo.GetAllAsync();
        if (string.IsNullOrWhiteSpace(roleName))
            return new List<AppPage>();

        if (roleName == AdminRoleName)
            return allPages;

        var role = await _roleRepo.GetByNameAsync(roleName);
        if (role is null)
            return new List<AppPage>();

        var allowed = await _permissionRepo.GetForRoleAsync(role.Id);
        var allowedKeys = allowed.Select(p => p.Page?.Key).ToHashSet();
        return allPages.Where(p => allowedKeys.Contains(p.Key)).ToList();
    }
}
