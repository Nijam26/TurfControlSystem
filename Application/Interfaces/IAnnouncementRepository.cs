namespace TurfControlSystem.Application.Interfaces;

public interface IAnnouncementRepository
{
    Task<bool> ExistsAsync(int bookingId, int thresholdMinutes);
    Task AddAsync(AnnouncementLog log);
}
