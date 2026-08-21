using TurfControlSystem.Infrastructure.Data;

namespace TurfControlSystem.Infrastructure.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly AppDbContext _db;
    public TeamRepository(AppDbContext db) => _db = db;

    public async Task<Team?> FindByMobileAsync(string mobile) =>
        await _db.Teams.FirstOrDefaultAsync(t => t.MobileNumber == mobile);

    public async Task<Team> AddAsync(Team team)
    {
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();
        return team;
    }
}
