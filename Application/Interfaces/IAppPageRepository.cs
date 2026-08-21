namespace TurfControlSystem.Application.Interfaces;

public interface IAppPageRepository
{
    Task<List<AppPage>> GetAllAsync();
    Task<AppPage?> GetByKeyAsync(string key);
}
