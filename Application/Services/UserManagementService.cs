namespace TurfControlSystem.Application.Services;

/// <summary>Business rules for the Admin → Users page: create users, assign roles, reset passwords.</summary>
public class UserManagementService
{
    private readonly IUserRepository _userRepo;
    private readonly AuthService _authService;

    public UserManagementService(IUserRepository userRepo, AuthService authService)
    {
        _userRepo = userRepo;
        _authService = authService;
    }

    public async Task<(bool Success, string Message, AppUser? User)> CreateUserAsync(
        string username, string displayName, string password, int roleId)
    {
        username = username.Trim();
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Username is required.", null);
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "Password must be at least 6 characters.", null);
        if (await _userRepo.UsernameExistsAsync(username))
            return (false, $"Username \"{username}\" is already taken.", null);

        var user = new AppUser
        {
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName.Trim(),
            RoleId = roleId,
            IsActive = true
        };
        user.PasswordHash = _authService.HashPassword(user, password);

        var created = await _userRepo.AddAsync(user);
        return (true, $"User \"{created.Username}\" created.", created);
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(
        int userId, string displayName, int roleId, bool isActive)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return (false, "User not found.");

        user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? user.Username : displayName.Trim();
        user.RoleId = roleId;
        user.IsActive = isActive;
        await _userRepo.UpdateAsync(user);
        return (true, "User updated.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPassword)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return (false, "User not found.");
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Password must be at least 6 characters.");

        user.PasswordHash = _authService.HashPassword(user, newPassword);
        await _userRepo.UpdateAsync(user);
        return (true, $"Password reset for \"{user.Username}\".");
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(int userId, int currentUserId)
    {
        if (userId == currentUserId)
            return (false, "You can't delete your own account while logged in as it.");

        await _userRepo.DeleteAsync(userId);
        return (true, "User deleted.");
    }
}
