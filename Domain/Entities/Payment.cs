namespace TurfControlSystem.Domain.Entities;

/// <summary>An additional payment recorded against a booking (on top of the advance).</summary>
public class Payment : BaseEntity
{
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaidOn { get; set; } = DateTime.Now;
    public string Method { get; set; } = "Cash";
}
