namespace TurfControlSystem.Application.Interfaces;

public interface ITurfRepository
{
    Task<List<Turf>> GetAllAsync();
    Task<List<Turf>> GetActiveAsync();
    Task<Turf?> GetByIdAsync(int id);
    Task<Turf> AddAsync(Turf turf);
    Task UpdateAsync(Turf turf);

    /// <summary>True if any booking (past or present) references this turf.</summary>
    Task<bool> HasBookingsAsync(int turfId);

    Task DeleteAsync(int id);
}
