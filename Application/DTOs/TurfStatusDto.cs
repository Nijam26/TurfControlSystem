namespace TurfControlSystem.Application.DTOs;

/// <summary>Live snapshot of one turf, pushed to the dashboard and TV display every second.</summary>
public class TurfStatusDto
{
    public int TurfId { get; set; }
    public string TurfName { get; set; } = string.Empty;

    public bool HasActiveBooking { get; set; }
    public int? BookingId { get; set; }
    public string? TeamName { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int RemainingSeconds { get; set; }
    public bool IsPaused { get; set; }
    public TurfLightStatus LightStatus { get; set; } = TurfLightStatus.Idle;

    public string? NextTeamName { get; set; }
    public DateTime? NextStartTime { get; set; }
}
