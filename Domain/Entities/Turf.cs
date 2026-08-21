namespace TurfControlSystem.Domain.Entities;

/// <summary>A physical playing field/pitch that can be booked.</summary>
public class Turf : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public TurfType Type { get; set; }
    public bool IsIndoor { get; set; }
    public decimal HourlyPrice { get; set; }
    public decimal PeakHourPrice { get; set; }
    public TimeSpan OpeningTime { get; set; } = new TimeSpan(6, 0, 0);
    public TimeSpan ClosingTime { get; set; } = new TimeSpan(23, 0, 0);
    public bool IsActive { get; set; } = true;

    public List<Booking> Bookings { get; set; } = new();
}
