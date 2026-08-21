using TurfControlSystem.Infrastructure.Data;

namespace TurfControlSystem.Infrastructure.Repositories;

public class TurfRepository : ITurfRepository
{
    private readonly AppDbContext _db;
    public TurfRepository(AppDbContext db) => _db = db;

    public async Task<List<Turf>> GetAllAsync() =>
        await _db.Turfs.OrderBy(t => t.Name).ToListAsync();

    public async Task<List<Turf>> GetActiveAsync() =>
        await _db.Turfs.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();

    public async Task<Turf?> GetByIdAsync(int id) =>
        await _db.Turfs.FindAsync(id);

    public async Task<Turf> AddAsync(Turf turf)
    {
        _db.Turfs.Add(turf);
        await _db.SaveChangesAsync();
        return turf;
    }

    public async Task UpdateAsync(Turf turf)
    {
        // Guard against a second, detached instance with the same key already being
        // tracked (common in Blazor Server, where the DbContext lives for the whole
        // circuit) — copy values onto the tracked instance instead of letting
        // _db.Turfs.Update() attach a duplicate, which EF Core would reject.
        var tracked = _db.ChangeTracker.Entries<Turf>()
            .FirstOrDefault(e => e.Entity.Id == turf.Id && !ReferenceEquals(e.Entity, turf));

        if (tracked is not null)
            tracked.CurrentValues.SetValues(turf);
        else
            _db.Turfs.Update(turf);

        await _db.SaveChangesAsync();
    }

    public async Task<bool> HasBookingsAsync(int turfId) =>
        await _db.Bookings.AnyAsync(b => b.TurfId == turfId);

    public async Task DeleteAsync(int id)
    {
        var turf = await _db.Turfs.FindAsync(id);
        if (turf is null) return;

        _db.Turfs.Remove(turf);
        await _db.SaveChangesAsync();
    }
}
