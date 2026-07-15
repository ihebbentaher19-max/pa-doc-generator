using System.Net;
using System.Text.Json;

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
                ArgumentException => HttpStatusCode.BadRequest,
                InvalidOperationException => HttpStatusCode.UnprocessableEntity,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var payload = JsonSerializer.Serialize(new
            {
                message = statusCode == HttpStatusCode.InternalServerError
                    ? "Une erreur interne est survenue."
                    : ex.Message
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
