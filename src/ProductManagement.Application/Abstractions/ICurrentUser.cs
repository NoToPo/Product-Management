namespace ProductManagement.Application.Abstractions;

/// <summary>
/// Identity of the caller, used to populate audit fields (CreatedBy/UpdatedBy).
/// In this assessment there is no auth, so the API supplies an optional
/// <c>X-User-Id</c> header and falls back to "system".
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
}
