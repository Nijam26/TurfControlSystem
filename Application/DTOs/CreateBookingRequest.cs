namespace TurfControlSystem.Application.DTOs;

/// <summary>Input model for the "New Booking" form.</summary>
public class CreateBookingRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Please select a turf.")]
    public int TurfId { get; set; }

    [Required(ErrorMessage = "Team name is required.")]
    public string TeamName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Captain name is required.")]
    public string CaptainName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [Phone(ErrorMessage = "Enter a valid mobile number.")]
    public string MobileNumber { get; set; } = string.Empty;

    [Range(1, 30, ErrorMessage = "Number of players must be between 1 and 30.")]
    public int NumberOfPlayers { get; set; } = 5;

    public DateTime StartTime { get; set; } = DateTime.Now;

    [Range(15, 240, ErrorMessage = "Duration must be between 15 and 240 minutes.")]
    public int DurationMinutes { get; set; } = 60;

    [Range(0, double.MaxValue, ErrorMessage = "Advance payment cannot be negative.")]
    public decimal AdvancePayment { get; set; }
}
