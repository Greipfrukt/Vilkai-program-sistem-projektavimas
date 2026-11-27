using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Synq.Common.Errors;
using Synq.Common.Http;
using Synq.Common.Json;

namespace Synq.TestKit;

/// <summary>
/// Paleidžia servisą atmintyje (be Docker image) prieš tikrą Postgres/Redis.
/// RabbitMQ išjungtas – įvykiai lieka outbox lentelėje, o testai tikrina būtent ją.
/// </summary>
public sealed class ServiceFactory<TProgram>(InfrastructureFixture infra) : WebApplicationFactory<TProgram> where TProgram : class
{
    public const string InternalToken = "test-internal-token";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Postgres", infra.Postgres.GetConnectionString());
        builder.UseSetting("Redis:Connection", infra.Redis.GetConnectionString());
        builder.UseSetting("RabbitMq:Enabled", "false");
        builder.UseSetting("Internal:Token", InternalToken);
        builder.UseSetting("Cookie:Secure", "false");
        builder.UseSetting("Auth:PasswordIterations", "1000");
    }

    /// <summary>Klientas, tarsi užklausa atėjo per gateway jau patikrinta sesija.</summary>
    public HttpClient CreateClientAs(Guid userId, Guid? sessionId = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add(SynqHeaders.UserId, userId.ToString());
        client.DefaultRequestHeaders.Add(SynqHeaders.SessionId, (sessionId ?? Guid.NewGuid()).ToString());
        return client;
    }

    public HttpClient CreateInternalClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add(SynqHeaders.InternalToken, InternalToken);
        return client;
    }
}

public static class HttpTestExtensions
{
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(SynqJson.Options))!;

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static async Task<ApiError> ReadErrorAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ApiError>(SynqJson.Options))!;

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object body) =>
        client.PostAsJsonAsync(url, body, SynqJson.Options);

    public static Task<HttpResponseMessage> PatchJsonAsync(this HttpClient client, string url, object body) =>
        client.PatchAsJsonAsync(url, body, SynqJson.Options);

    public static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@example.com";
}
