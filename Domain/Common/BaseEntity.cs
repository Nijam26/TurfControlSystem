namespace TurfControlSystem.Domain.Common;

/// <summary>Shared identity for every persisted entity in the system.</summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
