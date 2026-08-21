using TurfControlSystem.Infrastructure.Data;

namespace TurfControlSystem.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _db;
    public RoleRepository(AppDbContext db) => _db = db;

    public async Task<List<AppRole>> GetAllAsync() =>
        await _db.AppRoles.OrderBy(r => r.Name).ToListAsync();

    public async Task<AppRole?> GetByIdAsync(int id) =>
        await _db.AppRoles.FindAsync(id);

    public async Task<AppRole?> GetByNameAsync(string name) =>
        await _db.AppRoles.FirstOrDefaultAsync(r => r.Name == name);

    public async Task<bool> NameExistsAsync(string name, int? excludingId = null) =>
        await _db.AppRoles.AnyAsync(r => r.Name == name && r.Id != (excludingId ?? 0));

    public async Task<AppRole> AddAsync(AppRole role)
    {
        _db.AppRoles.Add(role);
        await _db.SaveChangesAsync();
        return role;
    }

    public async Task UpdateAsync(AppRole role)
    {
        var tracked = _db.ChangeTracker.Entries<AppRole>()
            .FirstOrDefault(e => e.Entity.Id == role.Id && !ReferenceEquals(e.Entity, role));

        if (tracked is not null)
            tracked.CurrentValues.SetValues(role);
        else
            _db.AppRoles.Update(role);

        await _db.SaveChangesAsync();
    }

    public async Task<bool> HasUsersAsync(int roleId) =>
        await _db.AppUsers.AnyAsync(u => u.RoleId == roleId);

    public async Task DeleteAsync(int id)
    {
        var role = await _db.AppRoles.FindAsync(id);
        if (role is null) return;

        // Clean up its permission rows first so nothing orphaned lingers in the matrix.
        var perms = _db.RolePagePermissions.Where(p => p.RoleId == id);
        _db.RolePagePermissions.RemoveRange(perms);

        _db.AppRoles.Remove(role);
        await _db.SaveChangesAsync();
    }
}
