using Microsoft.AspNetCore.Identity;

namespace TurfControlSystem.Application.Services;

/// <summary>
/// Validates logins and hashes passwords. Uses ASP.NET Core Identity's PasswordHasher
/// directly (PBKDF2 under the hood) rather than pulling in the full Identity system —
/// this app's users/roles are simple enough to model as plain entities managed from the
/// Admin pages, and the hasher is the one piece worth not hand-rolling.
/// </summary>
public class AuthService
{
    private readonly IUserRepository _userRepo;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public AuthService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public string HashPassword(AppUser user, string plainPassword) =>
        _hasher.HashPassword(user, plainPassword);

    /// <summary>Returns the user if the username/password are valid and the account is active, else null.</summary>
    public async Task<AppUser?> ValidateLoginAsync(string username, string password)
    {
        var user = await _userRepo.GetByUsernameAsync(username.Trim());
        if (user is null || !user.IsActive)
            return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }
}
