namespace Synq.Common.Http;

/// <summary>Antraščių pavadinimai, kuriais keičiasi gateway ir servisai.</summary>
public static class SynqHeaders
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string UserId = "X-User-Id";
    public const string SessionId = "X-Session-Id";
    public const string InternalToken = "X-Internal-Token";

    /// <summary>Antraštės, kurias gali nustatyti tik gateway – kliento atsiųstos kopijos pašalinamos.</summary>
    public static readonly string[] TrustedOnly = [UserId, SessionId, InternalToken];
}
