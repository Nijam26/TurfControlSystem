using Microsoft.AspNetCore.SignalR;

namespace TurfControlSystem.Infrastructure.Realtime;

/// <summary>
/// Thin SignalR hub. All screens (dashboard, TV display, or an external kiosk/mobile
/// client) connect here and receive "ReceiveTurfStatuses" / "ReceiveAnnouncement" pushes
/// from <see cref="TurfControlSystem.Infrastructure.BackgroundServices.CountdownBackgroundService"/>.
/// </summary>
public class TurfTimerHub : Hub
{
    public Task JoinDisplayGroup() => Groups.AddToGroupAsync(Context.ConnectionId, "Displays");
}
