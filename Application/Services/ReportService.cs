namespace TurfControlSystem.Application.Services;

/// <summary>Read-only aggregation for the Reports page.</summary>
public class ReportService
{
    private readonly IBookingRepository _bookingRepo;

    public ReportService(IBookingRepository bookingRepo)
    {
        _bookingRepo = bookingRepo;
    }

    /// <summary>Single-day report. Kept for backward compatibility; delegates to <see cref="GetReportAsync"/>.</summary>
    public Task<DailyReportDto> GetDailyReportAsync(DateTime date) => GetReportAsync(date, date);

    /// <summary>Report over an inclusive date range. Pass the same date for both to get a single-day report.</summary>
    public async Task<DailyReportDto> GetReportAsync(DateTime startDate, DateTime endDate)
    {
        // Be forgiving if the caller (e.g. a swapped date picker) sends them reversed.
        if (endDate.Date < startDate.Date)
            (startDate, endDate) = (endDate, startDate);

        var bookings = await _bookingRepo.GetByDateRangeAsync(startDate, endDate);
        var counted = bookings.Where(b => b.Status != BookingStatus.Cancelled).ToList();

        return new DailyReportDto
        {
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            TotalBookings = bookings.Count,
            CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled),
            TotalRevenue = counted.Sum(b => b.TotalPaid),
            DuePayments = counted.Sum(b => b.DueAmount),
            AverageDurationMinutes = counted.Count > 0
                ? counted.Average(b => (b.EndTime - b.StartTime).TotalMinutes)
                : 0,
            Bookings = bookings
        };
    }
}
