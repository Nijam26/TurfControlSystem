namespace TurfControlSystem.Application.Services;

/// <summary>
/// Watches the live turf statuses and fires "X minutes remaining" / "time's up"
/// announcements exactly once per threshold per booking (10 / 5 / 1 / 0 minutes).
/// </summary>
public class AnnouncementService
{
    private static readonly int[] Thresholds = { 10, 5, 1, 0 };

    private readonly IAnnouncementRepository _logRepo;

    public AnnouncementService(IAnnouncementRepository logRepo)
    {
        _logRepo = logRepo;
    }

    public async Task<List<AnnouncementDto>> CheckAnnouncementsAsync(List<TurfStatusDto> statuses)
    {
        var announcements = new List<AnnouncementDto>();

        foreach (var status in statuses.Where(s => s.HasActiveBooking && !s.IsPaused && s.BookingId.HasValue))
        {
            foreach (var threshold in Thresholds)
            {
                if (status.RemainingSeconds > threshold * 60)
                    continue;

                if (await _logRepo.ExistsAsync(status.BookingId!.Value, threshold))
                    continue;

                await _logRepo.AddAsync(new AnnouncementLog
                {
                    BookingId = status.BookingId.Value,
                    ThresholdMinutes = threshold
                });

                var message = threshold == 0
                    ? $"{status.TeamName}, your playing time has ended. Thank you for playing."
                    : $"{status.TeamName} has {threshold} minute{(threshold == 1 ? "" : "s")} remaining.";

                announcements.Add(new AnnouncementDto
                {
                    TurfId = status.TurfId,
                    TurfName = status.TurfName,
                    TeamName = status.TeamName ?? "Team",
                    ThresholdMinutes = threshold,
                    Message = message
                });
            }
        }

        return announcements;
    }
}
