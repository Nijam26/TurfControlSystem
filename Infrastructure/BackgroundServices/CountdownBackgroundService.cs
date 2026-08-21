namespace TurfControlSystem.Infrastructure.BackgroundServices;

/// <summary>
/// The engine behind the whole feature: ticks once a second, syncs booking statuses,
/// recomputes every turf's remaining time, and pushes the result (plus any newly-due
/// announcements) to every connected screen over SignalR.
/// </summary>
public class CountdownBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CountdownBackgroundService> _logger;

    public CountdownBackgroundService(IServiceScopeFactory scopeFactory, ILogger<CountdownBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // A new DI scope per tick: DbContext is scoped, this hosted service is a singleton.
                using var scope = _scopeFactory.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
                var timerService = scope.ServiceProvider.GetRequiredService<TurfTimerService>();
                var announcementService = scope.ServiceProvider.GetRequiredService<AnnouncementService>();
                var broadcaster = scope.ServiceProvider.GetRequiredService<ITimerBroadcaster>();

                await bookingService.SyncBookingStatusesAsync();

                var statuses = await timerService.GetTurfStatusesAsync();
                await broadcaster.BroadcastStatusesAsync(statuses);

                var announcements = await announcementService.CheckAnnouncementsAsync(statuses);
                foreach (var announcement in announcements)
                    await broadcaster.BroadcastAnnouncementAsync(announcement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Countdown tick failed.");
            }
        }
    }
}
