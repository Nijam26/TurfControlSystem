namespace TurfControlSystem.Application.DTOs;

/// <summary>Aggregated numbers for the Reports page, covering a single day or an inclusive date range.</summary>
public class DailyReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal DuePayments { get; set; }
    public double AverageDurationMinutes { get; set; }
    public List<Booking> Bookings { get; set; } = new();

    /// <summary>True when StartDate and EndDate are the same calendar day (single-date mode).</summary>
    public bool IsSingleDay => StartDate.Date == EndDate.Date;
}
