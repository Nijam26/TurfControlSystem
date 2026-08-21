using TurfControlSystem.Infrastructure.Data;

namespace TurfControlSystem.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;
    public PaymentRepository(AppDbContext db) => _db = db;

    public async Task<Payment> AddAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task<List<Payment>> GetForBookingAsync(int bookingId) =>
        await _db.Payments.Where(p => p.BookingId == bookingId).ToListAsync();
}
