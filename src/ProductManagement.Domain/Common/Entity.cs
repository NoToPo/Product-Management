namespace ProductManagement.Domain.Common;

/// <summary>
/// Base class for aggregate roots / entities. Provides identity, audit fields
/// and an optimistic-concurrency token (<see cref="Version"/>) which is mapped
/// to PostgreSQL's system <c>xmin</c> column in the persistence layer.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    /// <summary>
    /// Concurrency token (PostgreSQL <c>xmin</c>). Surfaced to clients as an ETag
    /// so updates can be guarded with <c>If-Match</c> (optimistic concurrency).
    /// </summary>
    public uint Version { get; protected set; }

    protected void Touch(string? by = null)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = by;
    }

    protected void Stamp(string? by = null)
    {
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = by;
    }
}
