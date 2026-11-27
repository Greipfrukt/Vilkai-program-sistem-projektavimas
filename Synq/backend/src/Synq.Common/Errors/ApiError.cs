using System.Text.Json.Serialization;

namespace Synq.Common.Errors;

/// <summary>
/// Klaidos atsakymo kūnas pagal Synq.yaml schemą <c>Error</c> (P-02).
/// Kiekvienas ne-2xx atsakymas iš bet kurio serviso turi būti būtent tokios formos.
/// </summary>
public sealed record ApiError(
    string Code,
    string Message,
    Guid CorrelationId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<FieldError>? FieldErrors = null);

/// <summary>Vieno lauko klaida (tik 422 VALIDATION_FAILED).</summary>
public sealed record FieldError(string Field, string Code, string Message);

/// <summary>FieldError.code reikšmės iš yaml enum.</summary>
public static class FieldErrorCodes
{
    public const string Required = "REQUIRED";
    public const string TooShort = "TOO_SHORT";
    public const string TooLong = "TOO_LONG";
    public const string InvalidFormat = "INVALID_FORMAT";
    public const string OutOfRange = "OUT_OF_RANGE";
    public const string NotAllowed = "NOT_ALLOWED";
}
