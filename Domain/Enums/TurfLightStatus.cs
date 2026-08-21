namespace TurfControlSystem.Domain.Enums;

/// <summary>Drives the traffic-light color and label on the dashboard / TV display.</summary>
public enum TurfLightStatus
{
    Idle,
    Running,
    EndingSoon,
    AlmostUp,
    TimeUp
}
