namespace ProductManagement.Domain.Common;

/// <summary>
/// Classifies a domain/application error so the API layer can map it to the
/// correct HTTP status code without leaking exceptions for control flow.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Failure
}

/// <summary>
/// A structured, serialisable error. Carries a stable machine-readable
/// <see cref="Code"/> plus a human-readable <see cref="Message"/>.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
