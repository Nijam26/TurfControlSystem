namespace TurfControlSystem.Application.Interfaces;

/// <summary>
/// Abstraction over the real-time push channel (SignalR in this project). Keeps the
/// Application layer free of any dependency on the transport technology.
/// </summary>
public interface ITimerBroadcaster
{
    Task BroadcastStatusesAsync(List<TurfStatusDto> statuses);
    Task BroadcastAnnouncementAsync(AnnouncementDto announcement);
}
