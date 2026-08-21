namespace TurfControlSystem.Domain.Entities;

/// <summary>A team/customer that books a turf. Looked up by mobile number on repeat visits.</summary>
public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string CaptainName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;

    public List<Booking> Bookings { get; set; } = new();
}
