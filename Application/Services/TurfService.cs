namespace TurfControlSystem.Application.Services;

/// <summary>Owns turf CRUD business rules, kept separate from BookingService (which owns bookings).</summary>
public class TurfService
{
    private readonly ITurfRepository _turfRepo;

    public TurfService(ITurfRepository turfRepo)
    {
        _turfRepo = turfRepo;
    }

    public async Task<(bool Success, string Message, Turf? Turf)> CreateTurfAsync(Turf turf)
    {
        var validation = Validate(turf);
        if (validation is not null)
            return (false, validation, null);

        turf.Id = 0; // guard against an edit-form leftover ID being reused for "add"
        var created = await _turfRepo.AddAsync(turf);
        return (true, $"\"{created.Name}\" added.", created);
    }

    public async Task<(bool Success, string Message)> UpdateTurfAsync(Turf turf)
    {
        var validation = Validate(turf);
        if (validation is not null)
            return (false, validation);

        var existing = await _turfRepo.GetByIdAsync(turf.Id);
        if (existing is null)
            return (false, "Turf not found.");

        // Copy the edited values onto the already-tracked instance instead of handing a
        // second, detached object (the form) to the repository. Blazor Server keeps the
        // same DbContext alive for the whole session, so attaching a different object with
        // the same key here would throw ("instance ... already being tracked").
        existing.Name = turf.Name;
        existing.Type = turf.Type;
        existing.IsIndoor = turf.IsIndoor;
        existing.HourlyPrice = turf.HourlyPrice;
        existing.PeakHourPrice = turf.PeakHourPrice;
        existing.OpeningTime = turf.OpeningTime;
        existing.ClosingTime = turf.ClosingTime;
        existing.IsActive = turf.IsActive;

        await _turfRepo.UpdateAsync(existing);
        return (true, $"\"{existing.Name}\" updated.");
    }

    /// <summary>
    /// Hard-deletes a turf only if it has no booking history at all, since a booking's
    /// TurfId foreign key would otherwise dangle and past reports would break. If the
    /// turf has any bookings, the caller is told to deactivate it instead.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteTurfAsync(int turfId)
    {
        var turf = await _turfRepo.GetByIdAsync(turfId);
        if (turf is null)
            return (false, "Turf not found.");

        if (await _turfRepo.HasBookingsAsync(turfId))
            return (false, $"Can't delete \"{turf.Name}\" — it has booking history. " +
                            "Deactivate it instead to hide it from new bookings while keeping past reports intact.");

        await _turfRepo.DeleteAsync(turfId);
        return (true, $"\"{turf.Name}\" deleted.");
    }

    public async Task<(bool Success, string Message)> ToggleActiveAsync(int turfId)
    {
        var turf = await _turfRepo.GetByIdAsync(turfId);
        if (turf is null)
            return (false, "Turf not found.");

        turf.IsActive = !turf.IsActive;
        await _turfRepo.UpdateAsync(turf);
        return (true, turf.IsActive ? $"\"{turf.Name}\" activated." : $"\"{turf.Name}\" deactivated.");
    }

    private static string? Validate(Turf turf)
    {
        if (string.IsNullOrWhiteSpace(turf.Name))
            return "Turf name is required.";
        if (turf.HourlyPrice <= 0)
            return "Hourly price must be greater than zero.";
        if (turf.PeakHourPrice < turf.HourlyPrice)
            return "Peak-hour price should not be lower than the regular hourly price.";
        return null;
    }
}
