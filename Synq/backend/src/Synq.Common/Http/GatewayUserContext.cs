using Microsoft.AspNetCore.Http;
using Synq.Common.Errors;

namespace Synq.Common.Http;

/// <summary>
/// Prisijungusio vartotojo tapatybė, kurią gateway nustato po sesijos patikrinimo (P-02).
/// Servisai patys sesijų netikrina: jie pasitiki X-User-Id / X-Session-Id, nes
/// (1) gateway pašalina kliento atsiųstas šių antraščių kopijas, (2) servisai pasiekiami tik privačiame Docker tinkle.
/// </summary>
public static class GatewayUserContext
{
    public static Guid? FindUserId(this HttpContext context) => ParseGuid(context, SynqHeaders.UserId);

    public static Guid? FindSessionId(this HttpContext context) => ParseGuid(context, SynqHeaders.SessionId);

    /// <summary>Vartotojo id arba 401 AUTH_REQUIRED.</summary>
    public static Guid RequireUserId(this HttpContext context) =>
        context.FindUserId() ?? throw ApiException.AuthRequired();

    public static Guid RequireSessionId(this HttpContext context) =>
        context.FindSessionId() ?? throw ApiException.AuthRequired();

    private static Guid? ParseGuid(HttpContext context, string header) =>
        Guid.TryParse(context.Request.Headers[header], out var id) && id != Guid.Empty ? id : null;
}
