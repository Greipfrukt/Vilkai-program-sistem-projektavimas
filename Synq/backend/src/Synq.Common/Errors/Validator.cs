using System.Net.Mail;

namespace Synq.Common.Errors;

/// <summary>
/// Surenka visas laukų klaidas ir meta vieną 422 VALIDATION_FAILED su <c>fieldErrors</c>.
/// Ilgiai skaičiuojami Unicode kodo taškais po trim (yaml „Common REST conventions“ → Format).
/// </summary>
/// <example>
/// var v = new Validator();
/// var name = v.Text("name", req.Name, min: 2, max: 64);
/// v.ThrowIfInvalid();
/// </example>
public sealed class Validator
{
    private readonly List<FieldError> _errors = [];

    public bool IsValid => _errors.Count == 0;

    public void Add(string field, string code, string message) => _errors.Add(new FieldError(field, code, message));

    /// <summary>Privalomas tekstas; grąžina apkarpytą reikšmę (arba tuščią eilutę, jei klaida).</summary>
    public string Text(string field, string? value, int min, int max)
    {
        var trimmed = value?.Trim() ?? "";
        var length = CodePointLength(trimmed);
        if (length == 0 && min > 0)
            Add(field, FieldErrorCodes.Required, "This field is required.");
        else if (length < min)
            Add(field, FieldErrorCodes.TooShort, $"At least {min} characters.");
        else if (length > max)
            Add(field, FieldErrorCodes.TooLong, $"At most {max} characters.");
        return trimmed;
    }

    /// <summary>El. paštas: privalomas, ≤ 254, normalizuojamas į mažąsias raides.</summary>
    public string Email(string field, string? value)
    {
        var email = value?.Trim().ToLowerInvariant() ?? "";
        if (email.Length == 0)
            Add(field, FieldErrorCodes.Required, "This field is required.");
        else if (email.Length > 254)
            Add(field, FieldErrorCodes.TooLong, "At most 254 characters.");
        else if (!IsEmail(email))
            Add(field, FieldErrorCodes.InvalidFormat, "Enter a valid email address.");
        return email;
    }

    /// <summary>Slaptažodis netrim'inamas – tarpai yra jo dalis.</summary>
    public string Password(string field, string? value, int min, int max)
    {
        var password = value ?? "";
        var length = CodePointLength(password);
        if (length == 0)
            Add(field, FieldErrorCodes.Required, "This field is required.");
        else if (length < min)
            Add(field, FieldErrorCodes.TooShort, $"At least {min} characters.");
        else if (length > max)
            Add(field, FieldErrorCodes.TooLong, $"At most {max} characters.");
        return password;
    }

    public T Required<T>(string field, T? value) where T : struct
    {
        if (value is null)
        {
            Add(field, FieldErrorCodes.Required, "This field is required.");
            return default;
        }
        return value.Value;
    }

    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw ApiException.Validation(_errors);
    }

    public static int CodePointLength(string s) => s.EnumerateRunes().Count();

    private static bool IsEmail(string email) =>
        !email.Any(char.IsWhiteSpace)
        && MailAddress.TryCreate(email, out var address)
        && address.Address == email
        && address.Host.Contains('.');
}
