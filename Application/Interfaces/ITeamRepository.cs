namespace TurfControlSystem.Application.Interfaces;

public interface ITeamRepository
{
    Task<Team?> FindByMobileAsync(string mobile);
    Task<Team> AddAsync(Team team);
}
