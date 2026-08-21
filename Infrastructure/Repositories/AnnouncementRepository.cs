using TurfControlSystem.Infrastructure.Data;

namespace TurfControlSystem.Infrastructure.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly AppDbContext _db;
    public AnnouncementRepository(AppDbContext db) => _db = db;

    public async Task<bool> ExistsAsync(int bookingId, int thresholdMinutes) =>
        await _db.AnnouncementLogs.AnyAsync(a => a.BookingId == bookingId && a.ThresholdMinutes == thresholdMinutes);

    public async Task AddAsync(AnnouncementLog log)
    {
        _db.AnnouncementLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
