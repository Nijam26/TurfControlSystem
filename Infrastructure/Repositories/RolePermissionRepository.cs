using TurfControlSystem.Infrastructure.Data;

namespace TurfControlSystem.Infrastructure.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly AppDbContext _db;
    public RolePermissionRepository(AppDbContext db) => _db = db;

    public async Task<List<RolePagePermission>> GetForRoleAsync(int roleId) =>
        await _db.RolePagePermissions
            .Include(p => p.Page)
            .Where(p => p.RoleId == roleId && p.CanView)
            .ToListAsync();

    public async Task<List<RolePagePermission>> GetAllAsync() =>
        await _db.RolePagePermissions
            .Include(p => p.Role)
            .Include(p => p.Page)
            .ToListAsync();

    public async Task SetRolePermissionsAsync(int roleId, IEnumerable<int> allowedPageIds)
    {
        var allowedSet = allowedPageIds.ToHashSet();

        var existing = await _db.RolePagePermissions.Where(p => p.RoleId == roleId).ToListAsync();
        _db.RolePagePermissions.RemoveRange(existing);

        foreach (var pageId in allowedSet)
        {
            _db.RolePagePermissions.Add(new RolePagePermission
            {
                RoleId = roleId,
                PageId = pageId,
                CanView = true
            });
        }

        await _db.SaveChangesAsync();
    }
}
