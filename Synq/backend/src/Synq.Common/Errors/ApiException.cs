namespace Synq.Common.Errors;

/// <summary>
/// Numatyta (verslo) klaida. Endpoint'ai ją meta, o <see cref="ExceptionMiddleware"/> paverčia
/// <see cref="ApiError"/> atsakymu su teisingu HTTP statusu iš katalogo.
/// </summary>
public sealed class ApiException : Exception
{
    public ApiException(string code, string? message = null, IReadOnlyList<FieldError>? fieldErrors = null, int? retryAfterSeconds = null)
        : base(message ?? ErrorCodes.MessageOf(code))
    {
        Code = code;
        Status = ErrorCodes.StatusOf(code);
        FieldErrors = fieldErrors;
        RetryAfterSeconds = retryAfterSeconds;
    }

    public string Code { get; }
    public int Status { get; }
    public IReadOnlyList<FieldError>? FieldErrors { get; }
    public int? RetryAfterSeconds { get; }

    public static ApiException BadRequest(string? message = null) => new(ErrorCodes.BadRequest, message);
    public static ApiException AuthRequired() => new(ErrorCodes.AuthRequired);
    public static ApiException Forbidden() => new(ErrorCodes.Forbidden);
    public static ApiException NotFound() => new(ErrorCodes.NotFound);
    public static ApiException DependencyUnavailable(int retryAfterSeconds = 5) =>
        new(ErrorCodes.DependencyUnavailable, retryAfterSeconds: retryAfterSeconds);

    public static ApiException Validation(IReadOnlyList<FieldError> fieldErrors) =>
        new(ErrorCodes.ValidationFailed, fieldErrors: fieldErrors);

    public static ApiException Validation(string field, string code, string message) =>
        Validation([new FieldError(field, code, message)]);
}
