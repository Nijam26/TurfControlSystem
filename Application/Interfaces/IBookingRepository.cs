namespace TurfControlSystem.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int id);

    /// <summary>The single booking currently occupying a turf at the given moment, if any.</summary>
    Task<Booking?> GetActiveForTurfAsync(int turfId, DateTime now);

    /// <summary>The next upcoming booking for a turf after the given moment.</summary>
    Task<Booking?> GetNextForTurfAsync(int turfId, DateTime afterTime);

    Task<bool> HasOverlapAsync(int turfId, DateTime start, DateTime end, int? excludeBookingId = null);

    Task<List<Booking>> GetTodayAsync(DateTime date);

    /// <summary>All bookings starting anywhere in the inclusive [startDate, endDate] window, for the Reports page.</summary>
    Task<List<Booking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>Bookings whose status needs to auto-flip (Scheduled→Running→Completed) as time passes.</summary>
    Task<List<Booking>> GetBookingsNeedingStatusSyncAsync(DateTime now);

    Task<Booking> AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task DeleteAsync(int id);
}
