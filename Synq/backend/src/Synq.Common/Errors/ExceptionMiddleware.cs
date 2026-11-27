using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Synq.Common.Http;
using Synq.Common.Json;

namespace Synq.Common.Errors;

/// <summary>
/// Globalus klaidų gaudytojas (P-02):
/// <list type="bullet">
/// <item><see cref="ApiException"/> → jos kodas ir statusas;</item>
/// <item>blogas JSON / parametras → 400 BAD_REQUEST;</item>
/// <item>visa kita → 500 INTERNAL_ERROR (detalės tik loguose, ne atsakyme).</item>
/// </list>
/// </summary>
public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Klientas nutraukė užklausą – tai ne serverio klaida.
        }
        catch (ApiException ex)
        {
            await WriteAsync(context, ex.Code, ex.Message, ex.FieldErrors, ex.RetryAfterSeconds);
        }
        catch (BadHttpRequestException ex)
        {
            // Minimal API meta šią klaidą dėl blogo JSON ar neparsinamų parametrų (ThrowOnBadRequest = true).
            var code = ex.StatusCode == StatusCodes.Status413PayloadTooLarge ? ErrorCodes.PayloadTooLarge : ErrorCodes.BadRequest;
            await WriteAsync(context, code, ErrorCodes.MessageOf(code));
        }
        catch (JsonException)
        {
            await WriteAsync(context, ErrorCodes.BadRequest, ErrorCodes.MessageOf(ErrorCodes.BadRequest));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, LogSanitizer.Path(context.Request.Path));
            await WriteAsync(context, ErrorCodes.InternalError, ErrorCodes.MessageOf(ErrorCodes.InternalError));
        }
    }

    public static async Task WriteAsync(
        HttpContext context, string code, string message,
        IReadOnlyList<FieldError>? fieldErrors = null, int? retryAfterSeconds = null)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = ErrorCodes.StatusOf(code);
        context.Response.Headers.CacheControl = "no-store";
        if (retryAfterSeconds is { } seconds)
            context.Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var body = new ApiError(code, message, context.GetCorrelationId(), FieldErrors: fieldErrors);
        await context.Response.WriteAsJsonAsync(body, SynqJson.Options);
    }
}
