namespace TurfControlSystem.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment> AddAsync(Payment payment);
    Task<List<Payment>> GetForBookingAsync(int bookingId);
}
