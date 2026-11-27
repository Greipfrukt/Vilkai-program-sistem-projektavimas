using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Synq.Common.Http;

/// <summary>
/// Pašalina slaptas reikšmes iš kelio prieš įrašant jį į logus (NFR-009).
/// Pvz. <c>GET /api/v1/invitations/{token}</c> – kvietimo tokenas (43 simb. base64url) yra slaptas.
/// Query string niekada nelogujamas.
/// </summary>
public static partial class LogSanitizer
{
    public static string Path(PathString path) => Token().Replace(path.Value ?? "", "***");

    [GeneratedRegex("(?<=/)[A-Za-z0-9_-]{43}(?=/|$)")]
    private static partial Regex Token();
}
