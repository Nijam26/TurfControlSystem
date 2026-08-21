using TurfControlSystem.Infrastructure.Data;

namespace TurfControlSystem.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;
    public BookingRepository(AppDbContext db) => _db = db;

    public async Task<Booking?> GetByIdAsync(int id) =>
        await _db.Bookings
            .Include(b => b.Team)
            .Include(b => b.Turf)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Booking?> GetActiveForTurfAsync(int turfId, DateTime now) =>
        await _db.Bookings
            .Include(b => b.Team)
            .Where(b => b.TurfId == turfId
                        && (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.Running)
                        && b.StartTime <= now && b.EndTime > now)
            .OrderBy(b => b.StartTime)
            .FirstOrDefaultAsync();

    public async Task<Booking?> GetNextForTurfAsync(int turfId, DateTime afterTime) =>
        await _db.Bookings
            .Include(b => b.Team)
            .Where(b => b.TurfId == turfId
                        && (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.Running)
                        && b.StartTime > afterTime)
            .OrderBy(b => b.StartTime)
            .FirstOrDefaultAsync();

    public async Task<bool> HasOverlapAsync(int turfId, DateTime start, DateTime end, int? excludeBookingId = null) =>
        await _db.Bookings.AnyAsync(b =>
            b.TurfId == turfId
            && b.Id != (excludeBookingId ?? -1)
            && (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.Running)
            && b.StartTime < end && start < b.EndTime);

    public async Task<List<Booking>> GetTodayAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        return await _db.Bookings
            .Include(b => b.Team)
            .Include(b => b.Turf)
            .Include(b => b.Payments)
            .Where(b => b.StartTime >= start && b.StartTime < end)
            .OrderBy(b => b.StartTime)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1); // exclusive upper bound, so endDate itself is fully included

        return await _db.Bookings
            .Include(b => b.Team)
            .Include(b => b.Turf)
            .Include(b => b.Payments)
            .Where(b => b.StartTime >= start && b.StartTime < end)
            .OrderBy(b => b.StartTime)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetBookingsNeedingStatusSyncAsync(DateTime now) =>
        await _db.Bookings.Where(b =>
                (b.Status == BookingStatus.Scheduled && b.StartTime <= now) ||
                ((b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.Running)
                 && b.EndTime <= now && b.PausedAt == null))
            .ToListAsync();

    public async Task<Booking> AddAsync(Booking booking)
    {
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking;
    }

    public async Task UpdateAsync(Booking booking)
    {
        _db.Bookings.Update(booking);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking is null) return;

        // Payments cascade-delete automatically (see AppDbContext); AnnouncementLog rows
        // for this booking are also configured to cascade so no orphaned log rows remain.
        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();
    }
}
