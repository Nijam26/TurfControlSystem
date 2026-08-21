using TurfControlSystem.Infrastructure.Data;

namespace TurfControlSystem.Infrastructure.Repositories;

public class AppPageRepository : IAppPageRepository
{
    private readonly AppDbContext _db;
    public AppPageRepository(AppDbContext db) => _db = db;

    public async Task<List<AppPage>> GetAllAsync() =>
        await _db.AppPages.OrderBy(p => p.SortOrder).ToListAsync();

    public async Task<AppPage?> GetByKeyAsync(string key) =>
        await _db.AppPages.FirstOrDefaultAsync(p => p.Key == key);
}
