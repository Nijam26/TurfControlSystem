using TurfControlSystem.Infrastructure.Data;

namespace TurfControlSystem.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public async Task<List<AppUser>> GetAllAsync() =>
        await _db.AppUsers.Include(u => u.Role).OrderBy(u => u.Username).ToListAsync();

    public async Task<AppUser?> GetByIdAsync(int id) =>
        await _db.AppUsers.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<AppUser?> GetByUsernameAsync(string username) =>
        await _db.AppUsers.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);

    public async Task<bool> UsernameExistsAsync(string username, int? excludingId = null) =>
        await _db.AppUsers.AnyAsync(u => u.Username == username && u.Id != (excludingId ?? 0));

    public async Task<AppUser> AddAsync(AppUser user)
    {
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(AppUser user)
    {
        var tracked = _db.ChangeTracker.Entries<AppUser>()
            .FirstOrDefault(e => e.Entity.Id == user.Id && !ReferenceEquals(e.Entity, user));

        if (tracked is not null)
            tracked.CurrentValues.SetValues(user);
        else
            _db.AppUsers.Update(user);

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user is null) return;

        _db.AppUsers.Remove(user);
        await _db.SaveChangesAsync();
    }
}
