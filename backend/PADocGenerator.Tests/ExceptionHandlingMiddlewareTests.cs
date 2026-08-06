using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Middleware;
using Xunit;

namespace PADocGenerator.Tests;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenBusinessExceptionThrown_ReturnsClearMessage()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new BusinessException("Flux importé invalide.");
        var middleware = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Contain("application/json");

        var body = await ReadBodyAsync(context.Response);
        var payload = JsonDocument.Parse(body).RootElement.GetProperty("message").GetString();
        payload.Should().Be("Flux importé invalide.");
    }

    [Fact]
    public async Task InvokeAsync_WhenNotFoundExceptionThrown_ReturnsBusinessMessage()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new KeyNotFoundException("Documentation introuvable : 123");
        var middleware = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        context.Response.ContentType.Should().Contain("application/json");

        var body = await ReadBodyAsync(context.Response);
        var payload = JsonDocument.Parse(body).RootElement.GetProperty("message").GetString();
        payload.Should().Be(UserMessages.DocumentationNotFound);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedExceptionThrown_ReturnsGenericMessage()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new InvalidOperationException("Détail technique interne");
        var middleware = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Contain("application/json");

        var body = await ReadBodyAsync(context.Response);
        var payload = JsonDocument.Parse(body).RootElement.GetProperty("message").GetString();
        payload.Should().Be("Une erreur interne est survenue.");
    }

    private static async Task<string> ReadBodyAsync(HttpResponse response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
