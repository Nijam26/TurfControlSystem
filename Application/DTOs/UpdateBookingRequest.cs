namespace TurfControlSystem.Application.DTOs;

/// <summary>
/// Input model for editing an existing (not-yet-started) booking. Deliberately doesn't
/// include team fields — changing the team's name/mobile is a Team edit, not a Booking
/// edit, since the same Team record may be shared across other bookings.
/// </summary>
public class UpdateBookingRequest
{
    public int BookingId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a turf.")]
    public int TurfId { get; set; }

    public DateTime StartTime { get; set; } = DateTime.Now;

    [Range(15, 240, ErrorMessage = "Duration must be between 15 and 240 minutes.")]
    public int DurationMinutes { get; set; } = 60;

    [Range(1, 30, ErrorMessage = "Number of players must be between 1 and 30.")]
    public int NumberOfPlayers { get; set; } = 5;

    [Range(0, double.MaxValue, ErrorMessage = "Advance payment cannot be negative.")]
    public decimal AdvancePayment { get; set; }
}
