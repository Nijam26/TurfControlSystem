namespace TurfControlSystem.Application.Interfaces;

public interface IUserRepository
{
    Task<List<AppUser>> GetAllAsync();
    Task<AppUser?> GetByIdAsync(int id);
    Task<AppUser?> GetByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username, int? excludingId = null);
    Task<AppUser> AddAsync(AppUser user);
    Task UpdateAsync(AppUser user);
    Task DeleteAsync(int id);
}
