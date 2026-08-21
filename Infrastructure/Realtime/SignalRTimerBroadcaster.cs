using Microsoft.AspNetCore.SignalR;

namespace TurfControlSystem.Infrastructure.Realtime;

public class SignalRTimerBroadcaster : ITimerBroadcaster
{
    private readonly IHubContext<TurfTimerHub> _hub;
    public SignalRTimerBroadcaster(IHubContext<TurfTimerHub> hub) => _hub = hub;

    public Task BroadcastStatusesAsync(List<TurfStatusDto> statuses) =>
        _hub.Clients.All.SendAsync("ReceiveTurfStatuses", statuses);

    public Task BroadcastAnnouncementAsync(AnnouncementDto announcement) =>
        _hub.Clients.All.SendAsync("ReceiveAnnouncement", announcement);
}
