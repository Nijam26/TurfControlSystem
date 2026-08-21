namespace TurfControlSystem.Domain.Entities;

/// <summary>A login account for the system. One role per user, chosen by the admin.</summary>
public class AppUser : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int RoleId { get; set; }
    public AppRole? Role { get; set; }
}
