namespace DotnetMinimalApi.Models.Entities;

/// <summary>
/// Base class for all domain entities providing identification and audit timestamps.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
