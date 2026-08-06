using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PADocGenerator.Api.Common;

namespace PADocGenerator.Api.Middleware;

/// <summary>
/// Convertit toute exception non gérée en réponse JSON exploitable par le
/// frontend, plutôt que de laisser fuiter une trace serveur brute.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception non gérée sur {Path}", context.Request.Path);

            var statusCode = ex switch
            {
                KeyNotFoundException => HttpStatusCode.NotFound,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                BusinessException => HttpStatusCode.BadRequest,
                ArgumentException => HttpStatusCode.BadRequest,
                InvalidOperationException => ex.Message.Contains("Version actuelle", StringComparison.OrdinalIgnoreCase)
                    ? HttpStatusCode.UnprocessableEntity
                    : HttpStatusCode.InternalServerError,
                DbUpdateException => HttpStatusCode.UnprocessableEntity,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var payload = JsonSerializer.Serialize(new
            {
                message = GetUserFacingMessage(ex, statusCode)
            });

            await context.Response.WriteAsync(payload);
        }
    }

    private static string GetUserFacingMessage(Exception ex, HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.InternalServerError => UserMessages.InternalError,
            _ when ex is DbUpdateException => "La donnée envoyée n'est pas valide et n'a pas pu être enregistrée.",
            _ when ex is BusinessException => ex.Message,
            _ when ex is KeyNotFoundException => MapKnownNotFoundMessage(ex.Message),
            _ when ex is ArgumentException => ex.Message.Contains("Statut", StringComparison.OrdinalIgnoreCase)
                ? UserMessages.InvalidStatus
                : ex.Message,
            _ when ex is InvalidOperationException => ex.Message.Contains("Version actuelle", StringComparison.OrdinalIgnoreCase)
                ? UserMessages.ActiveVersionNotFound
                : UserMessages.InternalError,
            _ => UserMessages.InternalError
        };
    }

    private static string MapKnownNotFoundMessage(string message)
    {
        if (message.Contains("Version", StringComparison.OrdinalIgnoreCase))
        {
            return UserMessages.VersionNotFound;
        }

        if (message.Contains("Flux", StringComparison.OrdinalIgnoreCase))
        {
            return UserMessages.FlowImportNotFound;
        }

        return UserMessages.DocumentationNotFound;
    }
}