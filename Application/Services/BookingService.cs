namespace TurfControlSystem.Application.Services;

/// <summary>
/// Owns the booking lifecycle: create → run → pause/resume → extend → stop/cancel,
/// plus the automatic Scheduled → Running → Completed status sync as time passes.
/// </summary>
public class BookingService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly ITurfRepository _turfRepo;
    private readonly IPaymentRepository _paymentRepo;

    public BookingService(
        IBookingRepository bookingRepo,
        ITeamRepository teamRepo,
        ITurfRepository turfRepo,
        IPaymentRepository paymentRepo)
    {
        _bookingRepo = bookingRepo;
        _teamRepo = teamRepo;
        _turfRepo = turfRepo;
        _paymentRepo = paymentRepo;
    }

    public async Task<(bool Success, string Message, Booking? Booking)> CreateBookingAsync(CreateBookingRequest request)
    {
        var turf = await _turfRepo.GetByIdAsync(request.TurfId);
        if (turf is null)
            return (false, "Please select a valid turf.", null);

        var end = request.StartTime.AddMinutes(request.DurationMinutes);

        if (await _bookingRepo.HasOverlapAsync(request.TurfId, request.StartTime, end))
            return (false, "This turf is already booked for the selected time slot.", null);

        var team = await _teamRepo.FindByMobileAsync(request.MobileNumber);
        if (team is null)
        {
            team = new Team
            {
                Name = request.TeamName,
                CaptainName = request.CaptainName,
                MobileNumber = request.MobileNumber
            };
            team = await _teamRepo.AddAsync(team);
        }

        var isPeak = IsPeakHour(request.StartTime);
        var rate = isPeak ? turf.PeakHourPrice : turf.HourlyPrice;
        var totalAmount = Math.Round(rate * request.DurationMinutes / 60m, 2);

        var booking = new Booking
        {
            TurfId = turf.Id,
            TeamId = team.Id,
            NumberOfPlayers = request.NumberOfPlayers,
            StartTime = request.StartTime,
            EndTime = end,
            OriginalEndTime = end,
            TotalAmount = totalAmount,
            AdvancePayment = request.AdvancePayment,
            Status = BookingStatus.Scheduled
        };

        booking = await _bookingRepo.AddAsync(booking);
        return (true, "Booking created successfully.", booking);
    }

    /// <summary>Pauses a running session on first call, resumes (shifting EndTime forward) on the next.</summary>
    public async Task<(bool Success, string Message)> TogglePauseAsync(int bookingId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking is null)
            return (false, "Booking not found.");

        if (booking.PausedAt is null)
        {
            booking.PausedAt = DateTime.Now;
            await _bookingRepo.UpdateAsync(booking);
            return (true, $"{booking.Team?.Name} — session paused.");
        }

        var pausedDuration = DateTime.Now - booking.PausedAt.Value;
        booking.EndTime = booking.EndTime.Add(pausedDuration);
        booking.PausedAt = null;
        await _bookingRepo.UpdateAsync(booking);
        return (true, $"{booking.Team?.Name} — session resumed.");
    }

    public async Task<(bool Success, string Message)> ExtendBookingAsync(int bookingId, int additionalMinutes)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking is null)
            return (false, "Booking not found.");

        var newEnd = booking.EndTime.AddMinutes(additionalMinutes);

        if (await _bookingRepo.HasOverlapAsync(booking.TurfId, booking.EndTime, newEnd, booking.Id))
            return (false, "Cannot extend — the next booking starts too soon.");

        booking.EndTime = newEnd;
        await _bookingRepo.UpdateAsync(booking);
        return (true, $"{booking.Team?.Name} — extended by {additionalMinutes} minutes.");
    }

    public async Task<(bool Success, string Message)> StopBookingAsync(int bookingId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking is null)
            return (false, "Booking not found.");

        booking.EndTime = DateTime.Now;
        booking.PausedAt = null;
        booking.Status = BookingStatus.Completed;
        await _bookingRepo.UpdateAsync(booking);
        return (true, $"{booking.Team?.Name} — session stopped.");
    }

    public async Task<(bool Success, string Message)> CancelBookingAsync(int bookingId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking is null)
            return (false, "Booking not found.");

        booking.Status = BookingStatus.Cancelled;
        await _bookingRepo.UpdateAsync(booking);
        return (true, "Booking cancelled.");
    }

    /// <summary>
    /// Edits turf/time/players/advance on a booking that hasn't started yet. Team identity
    /// isn't editable here — that's a Team record shared across bookings, not a per-booking
    /// field. Once a session is Running, use Pause/Extend/Stop on the Dashboard instead.
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateBookingAsync(UpdateBookingRequest request)
    {
        var booking = await _bookingRepo.GetByIdAsync(request.BookingId);
        if (booking is null)
            return (false, "Booking not found.");

        if (booking.Status != BookingStatus.Scheduled)
            return (false, "Only upcoming (not yet started) bookings can be edited. " +
                            "Use the Dashboard to pause, extend, or stop a live session.");

        var turf = await _turfRepo.GetByIdAsync(request.TurfId);
        if (turf is null)
            return (false, "Please select a valid turf.");

        var newEnd = request.StartTime.AddMinutes(request.DurationMinutes);

        if (await _bookingRepo.HasOverlapAsync(request.TurfId, request.StartTime, newEnd, booking.Id))
            return (false, "This turf is already booked for the selected time slot.");

        var isPeak = IsPeakHour(request.StartTime);
        var rate = isPeak ? turf.PeakHourPrice : turf.HourlyPrice;
        var totalAmount = Math.Round(rate * request.DurationMinutes / 60m, 2);

        booking.TurfId = turf.Id;
        booking.StartTime = request.StartTime;
        booking.EndTime = newEnd;
        booking.OriginalEndTime = newEnd;
        booking.NumberOfPlayers = request.NumberOfPlayers;
        booking.TotalAmount = totalAmount;
        booking.AdvancePayment = request.AdvancePayment;

        await _bookingRepo.UpdateAsync(booking);
        return (true, "Booking updated.");
    }

    /// <summary>
    /// Hard-deletes a booking only if it never actually occupied the turf (Scheduled, still
    /// in the future) or was already called off (Cancelled). A Running session must be
    /// stopped first, and Completed sessions stay as real history for the Reports page.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteBookingAsync(int bookingId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking is null)
            return (false, "Booking not found.");

        if (booking.Status is BookingStatus.Running)
            return (false, "Can't delete a session that's currently running. Stop it first from the Dashboard.");

        if (booking.Status is BookingStatus.Completed)
            return (false, "Can't delete a completed booking — it's part of the day's history used in Reports.");

        await _bookingRepo.DeleteAsync(bookingId);
        return (true, "Booking deleted.");
    }

    public async Task RecordPaymentAsync(int bookingId, decimal amount, string method)
    {
        await _paymentRepo.AddAsync(new Payment { BookingId = bookingId, Amount = amount, Method = method });
    }

    /// <summary>Called once per tick by the background service to keep booking.Status honest.</summary>
    public async Task SyncBookingStatusesAsync()
    {
        var now = DateTime.Now;
        var bookings = await _bookingRepo.GetBookingsNeedingStatusSyncAsync(now);

        foreach (var booking in bookings)
        {
            if (booking.EndTime <= now)
                booking.Status = BookingStatus.Completed;
            else if (booking.StartTime <= now)
                booking.Status = BookingStatus.Running;

            await _bookingRepo.UpdateAsync(booking);
        }
    }

    /// <summary>Simple evening peak-hour rule (5 PM – 10 PM). Adjust to match real pricing policy.</summary>
    private static bool IsPeakHour(DateTime start) => start.Hour is >= 17 and < 22;
}
