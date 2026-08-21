namespace TurfControlSystem.Application.Services;

/// <summary>Business rules for the Admin → Roles page: create/rename/delete roles.</summary>
public class RoleManagementService
{
    private readonly IRoleRepository _roleRepo;

    public RoleManagementService(IRoleRepository roleRepo)
    {
        _roleRepo = roleRepo;
    }

    public async Task<(bool Success, string Message, AppRole? Role)> CreateRoleAsync(string name, string description)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Role name is required.", null);

        if (await _roleRepo.NameExistsAsync(name))
            return (false, $"A role named \"{name}\" already exists.", null);

        var role = new AppRole { Name = name, Description = description.Trim(), IsSystemRole = false };
        var created = await _roleRepo.AddAsync(role);
        return (true, $"Role \"{created.Name}\" created. Set its page access from Admin → Permissions.", created);
    }

    public async Task<(bool Success, string Message)> UpdateRoleAsync(int roleId, string name, string description)
    {
        var role = await _roleRepo.GetByIdAsync(roleId);
        if (role is null)
            return (false, "Role not found.");

        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Role name is required.");

        if (role.IsSystemRole && name != role.Name)
            return (false, "The Admin role can't be renamed.");

        if (await _roleRepo.NameExistsAsync(name, excludingId: roleId))
            return (false, $"A role named \"{name}\" already exists.");

        role.Name = name;
        role.Description = description.Trim();
        await _roleRepo.UpdateAsync(role);
        return (true, "Role updated.");
    }

    public async Task<(bool Success, string Message)> DeleteRoleAsync(int roleId)
    {
        var role = await _roleRepo.GetByIdAsync(roleId);
        if (role is null)
            return (false, "Role not found.");

        if (role.IsSystemRole)
            return (false, "The Admin role can't be deleted.");

        if (await _roleRepo.HasUsersAsync(roleId))
            return (false, "This role still has users assigned to it. Move them to another role first.");

        await _roleRepo.DeleteAsync(roleId);
        return (true, $"Role \"{role.Name}\" deleted.");
    }
}
