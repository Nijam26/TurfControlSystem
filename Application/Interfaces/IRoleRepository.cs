namespace TurfControlSystem.Application.Interfaces;

public interface IRoleRepository
{
    Task<List<AppRole>> GetAllAsync();
    Task<AppRole?> GetByIdAsync(int id);
    Task<AppRole?> GetByNameAsync(string name);
    Task<bool> NameExistsAsync(string name, int? excludingId = null);
    Task<AppRole> AddAsync(AppRole role);
    Task UpdateAsync(AppRole role);
    Task<bool> HasUsersAsync(int roleId);
    Task DeleteAsync(int id);
}
