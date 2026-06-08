using ProductManagement.Application.Abstractions;

namespace ProductManagement.Api.Common;

/// <summary>
/// Resolves the caller identity for audit fields. No auth in this assessment, so it
/// reads an optional <c>X-User-Id</c> header and defaults to "system".
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId
    {
        get
        {
            var header = accessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
            return string.IsNullOrWhiteSpace(header) ? "system" : header;
        }
    }
}
