namespace TurfControlSystem.Domain.Entities;

/// <summary>A confirmed playing slot for a team on a turf, with live pause/extend support.</summary>
public class Booking : BaseEntity
{
    public int TurfId { get; set; }
    public Turf? Turf { get; set; }

    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public int NumberOfPlayers { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime OriginalEndTime { get; set; }

    /// <summary>Set while the operator has paused the session; the countdown freezes here.</summary>
    public DateTime? PausedAt { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Scheduled;

    public decimal TotalAmount { get; set; }
    public decimal AdvancePayment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Payment> Payments { get; set; } = new();

    [NotMapped]
    public bool IsPaused => PausedAt is not null;

    [NotMapped]
    public decimal TotalPaid => AdvancePayment + Payments.Sum(p => p.Amount);

    [NotMapped]
    public decimal DueAmount => Math.Max(0, TotalAmount - TotalPaid);

    [NotMapped]
    public PaymentStatus PaymentStatus =>
        TotalPaid <= 0 ? PaymentStatus.Unpaid :
        TotalPaid < TotalAmount ? PaymentStatus.PartiallyPaid : PaymentStatus.Paid;
}
