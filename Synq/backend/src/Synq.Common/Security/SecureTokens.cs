using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Synq.Common.Security;

/// <summary>
/// Atsitiktiniai neprognozuojami tokenai (sesijos, kvietimai): 32 baitai → 43 simbolių base64url.
/// DB saugomas tik SHA-256 – nutekėjus DB, tokenais pasinaudoti neįmanoma.
/// Lėto hash (kaip slaptažodžiams) nereikia: 256 bitų atsitiktinumo neįmanoma perrinkti.
/// </summary>
public static class SecureTokens
{
    public const int ByteLength = 32;

    public static string Generate() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(ByteLength));

    public static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    public static string HashHex(string token) => Convert.ToHexStringLower(Hash(token));
}

/// <summary>
/// Bendras gateway ir Auth susitarimas dėl sesijų cache Redis'e.
/// Raktas sudarytas iš tokeno hash (ne paties tokeno), todėl Redis turinys taip pat nėra slaptas.
/// </summary>
public static class SessionCache
{
    /// <summary>Kiek laiko gateway laiko patvirtintą sesiją (atsijungus Auth ištrina įrašą iškart).</summary>
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    public static string KeyForTokenHash(byte[] tokenHash) => $"synq:session:{Convert.ToHexStringLower(tokenHash)}";

    public static string KeyForToken(string token) => KeyForTokenHash(SecureTokens.Hash(token));
}
