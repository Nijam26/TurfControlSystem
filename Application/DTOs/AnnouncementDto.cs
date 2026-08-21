namespace TurfControlSystem.Application.DTOs;

/// <summary>A single "X minutes remaining" / "time's up" announcement event.</summary>
public class AnnouncementDto
{
    public int TurfId { get; set; }
    public string TurfName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int ThresholdMinutes { get; set; }
    public string Message { get; set; } = string.Empty;
}
