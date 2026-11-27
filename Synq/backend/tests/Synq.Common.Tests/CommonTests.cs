using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Synq.Common.Contracts;
using Synq.Common.Errors;
using Synq.Common.Http;
using Synq.Common.Json;
using Synq.Common.Security;

namespace Synq.Common.Tests;

public class ValidatorTests
{
    [Fact]
    public void Text_counts_unicode_code_points_after_trim()
    {
        var v = new Validator();
        // 2 emoji = 2 kodo taškai (bet 4 UTF-16 simboliai) → tinka min 2.
        var value = v.Text("name", "  😀😀  ", min: 2, max: 2);
        Assert.True(v.IsValid);
        Assert.Equal("😀😀", value);
    }

    [Fact]
    public void Collects_all_field_errors_into_one_422()
    {
        var v = new Validator();
        v.Email("email", "not-an-email");
        v.Text("displayName", "", min: 1, max: 50);
        v.Password("password", "short", min: 8, max: 128);

        var ex = Assert.Throws<ApiException>(v.ThrowIfInvalid);
        Assert.Equal(ErrorCodes.ValidationFailed, ex.Code);
        Assert.Equal(422, ex.Status);
        Assert.Equal(["email", "displayName", "password"], ex.FieldErrors!.Select(e => e.Field));
        Assert.Equal([FieldErrorCodes.InvalidFormat, FieldErrorCodes.Required, FieldErrorCodes.TooShort], ex.FieldErrors!.Select(e => e.Code));
    }

    [Fact]
    public void Email_is_normalized_to_lowercase()
    {
        var v = new Validator();
        Assert.Equal("alice@example.com", v.Email("email", " Alice@Example.COM "));
        Assert.True(v.IsValid);
    }
}

public class PagingTests
{
    private sealed record Row(Guid Id, string Name, int Age);

    private static readonly SortMap<Row> Sorts = new SortMap<Row>(r => r.Id).Add("name", r => r.Name).Add("age", r => r.Age);

    [Fact]
    public void Sort_supports_descending_and_multiple_fields()
    {
        var rows = new[] { new Row(Guid.NewGuid(), "b", 1), new Row(Guid.NewGuid(), "a", 1), new Row(Guid.NewGuid(), "c", 2) }.AsQueryable();
        var sorted = Sorts.Apply(rows, "-age,name", "name").Select(r => r.Name).ToList();
        Assert.Equal(["c", "a", "b"], sorted);
    }

    [Fact]
    public void Unknown_sort_field_is_400()
    {
        var ex = Assert.Throws<ApiException>(() => Sorts.Apply(Array.Empty<Row>().AsQueryable(), "password", "name"));
        Assert.Equal(ErrorCodes.BadRequest, ex.Code);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, 101)]
    public void Invalid_offset_or_limit_is_400(int offset, int limit) =>
        Assert.Equal(400, Assert.Throws<ApiException>(() => PageRequest.From(offset, limit)).Status);

    [Fact]
    public void Defaults_are_offset_0_limit_50() => Assert.Equal(new PageRequest(0, 50), PageRequest.From(null, null));
}

public class JsonTests
{
    private sealed record Sample(DateTime At, WorkspaceRole Role, MessageKind Kind);

    [Fact]
    public void Dates_have_milliseconds_and_Z_and_enums_are_upper_snake_case()
    {
        var json = JsonSerializer.Serialize(
            new Sample(new DateTime(2026, 11, 6, 9, 15, 30, 123, DateTimeKind.Utc), WorkspaceRole.Administrator, MessageKind.DocumentCard),
            SynqJson.Options);
        Assert.Equal("""{"at":"2026-11-06T09:15:30.123Z","role":"ADMINISTRATOR","kind":"DOCUMENT_CARD"}""", json);
    }

    [Fact]
    public void Enums_are_read_back_from_contract_form()
    {
        var sample = JsonSerializer.Deserialize<Sample>("""{"at":"2026-11-06T09:15:30.123Z","role":"OWNER","kind":"USER"}""", SynqJson.Options)!;
        Assert.Equal(WorkspaceRole.Owner, sample.Role);
        Assert.Equal(DateTimeKind.Utc, sample.At.Kind);
    }
}

public class ErrorMiddlewareTests
{
    private static async Task<(int Status, JsonElement Body)> RunAsync(RequestDelegate endpoint)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var correlation = new CorrelationIdMiddleware(new ExceptionMiddleware(endpoint, NullLogger<ExceptionMiddleware>.Instance).InvokeAsync);
        await correlation.InvokeAsync(context);
        context.Response.Body.Position = 0;
        return (context.Response.StatusCode, await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body));
    }

    [Fact]
    public async Task Unexpected_exception_becomes_500_INTERNAL_ERROR_without_details()
    {
        var (status, body) = await RunAsync(_ => throw new InvalidOperationException("secret connection string"));
        Assert.Equal(500, status);
        Assert.Equal("INTERNAL_ERROR", body.GetProperty("code").GetString());
        Assert.DoesNotContain("secret", body.GetRawText());
        Assert.True(Guid.TryParse(body.GetProperty("correlationId").GetString(), out _));
    }

    [Fact]
    public async Task Validation_error_is_422_with_fieldErrors()
    {
        var (status, body) = await RunAsync(_ => throw ApiException.Validation("name", FieldErrorCodes.TooLong, "At most 64 characters."));
        Assert.Equal(422, status);
        Assert.Equal("VALIDATION_FAILED", body.GetProperty("code").GetString());
        var fieldError = body.GetProperty("fieldErrors")[0];
        Assert.Equal("name", fieldError.GetProperty("field").GetString());
        Assert.Equal("TOO_LONG", fieldError.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Non_validation_errors_have_no_fieldErrors_property()
    {
        var (status, body) = await RunAsync(_ => throw ApiException.Forbidden());
        Assert.Equal(403, status);
        Assert.False(body.TryGetProperty("fieldErrors", out _));
    }
}

public class SecurityTests
{
    [Fact]
    public void Tokens_are_43_char_base64url_and_unique()
    {
        var a = SecureTokens.Generate();
        var b = SecureTokens.Generate();
        Assert.Matches("^[A-Za-z0-9_-]{43}$", a);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Invitation_token_in_path_is_masked_for_logs()
    {
        var token = SecureTokens.Generate();
        Assert.Equal("/api/v1/invitations/***", LogSanitizer.Path(new PathString($"/api/v1/invitations/{token}")));
        Assert.Equal("/api/v1/workspaces", LogSanitizer.Path(new PathString("/api/v1/workspaces")));
    }
}
