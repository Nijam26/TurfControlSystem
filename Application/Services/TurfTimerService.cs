namespace TurfControlSystem.Application.Services;

/// <summary>
/// The heart of the "live countdown" idea: for every active turf, works out who is
/// playing, how much time is left, what color/status that maps to, and who is next.
/// </summary>
public class TurfTimerService
{
    private readonly ITurfRepository _turfRepo;
    private readonly IBookingRepository _bookingRepo;

    public TurfTimerService(ITurfRepository turfRepo, IBookingRepository bookingRepo)
    {
        _turfRepo = turfRepo;
        _bookingRepo = bookingRepo;
    }

    public async Task<List<TurfStatusDto>> GetTurfStatusesAsync()
    {
        var now = DateTime.Now;
        var turfs = await _turfRepo.GetActiveAsync();
        var result = new List<TurfStatusDto>();

        foreach (var turf in turfs)
        {
            var dto = new TurfStatusDto { TurfId = turf.Id, TurfName = turf.Name };

            var active = await _bookingRepo.GetActiveForTurfAsync(turf.Id, now);
            if (active is not null)
            {
                // While paused, the countdown freezes at the moment it was paused.
                var referenceNow = active.PausedAt ?? now;
                var remaining = active.EndTime - referenceNow;
                if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

                dto.HasActiveBooking = true;
                dto.BookingId = active.Id;
                dto.TeamName = active.Team?.Name;
                dto.StartTime = active.StartTime;
                dto.EndTime = active.EndTime;
                dto.RemainingSeconds = (int)remaining.TotalSeconds;
                dto.IsPaused = active.IsPaused;
                dto.LightStatus = active.IsPaused ? TurfLightStatus.Idle : ComputeLightStatus(remaining);
            }

            var next = await _bookingRepo.GetNextForTurfAsync(turf.Id, now);
            if (next is not null)
            {
                dto.NextTeamName = next.Team?.Name;
                dto.NextStartTime = next.StartTime;
            }

            result.Add(dto);
        }

        return result;
    }

    public static TurfLightStatus ComputeLightStatus(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero) return TurfLightStatus.TimeUp;
        if (remaining <= TimeSpan.FromMinutes(5)) return TurfLightStatus.AlmostUp;
        if (remaining <= TimeSpan.FromMinutes(15)) return TurfLightStatus.EndingSoon;
        return TurfLightStatus.Running;
    }
}
