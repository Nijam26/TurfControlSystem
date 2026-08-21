namespace TurfControlSystem.Domain.Entities;

/// <summary>
/// Records that a countdown announcement (10 / 5 / 1 / 0 minutes) has already fired for a
/// booking, so the background timer never repeats the same announcement twice.
/// </summary>
public class AnnouncementLog : BaseEntity
{
    public int BookingId { get; set; }
    public int ThresholdMinutes { get; set; }
    public DateTime AnnouncedAt { get; set; } = DateTime.Now;
}
