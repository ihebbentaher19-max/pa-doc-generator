using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

public class PowerPlatformFlowServiceTests
{
    [Fact]
    public async Task GetFlowDefinitionAsync_UsesOnlyAccessibleEnvironmentAndReturnsDataverseDefinition()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!.ToString();

            if (uri.Contains("environmentmanagement/environments", StringComparison.Ordinal))
            {
                Assert.Equal("Bearer power-platform-token", request.Headers.Authorization!.ToString());
                return Json("""
                    { "value": [{ "id": "env-1", "displayName": "Production", "url": "https://contoso.crm.dynamics.com/", "tenantId": "tenant-1" }] }
                    """);
            }

            if (uri.Contains("cloudFlows", StringComparison.Ordinal))
            {
                Assert.Equal("Bearer power-platform-token", request.Headers.Authorization!.ToString());
                return Json("""
                    { "value": [{ "workflowId": "9b4325dc-0a92-4f99-8386-eae4d9f0f29f", "displayName": "Notifier une demande", "state": "Started" }] }
                    """);
            }

            Assert.Equal("Bearer dataverse-token", request.Headers.Authorization!.ToString());
            Assert.Equal("https://contoso.crm.dynamics.com/api/data/v9.2/workflows(9b4325dc-0a92-4f99-8386-eae4d9f0f29f)?$select=clientdata,name,workflowid", uri);
            return Json("""
                { "name": "Notifier une demande", "clientdata": "{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}},\"actions\":{\"send\":{\"type\":\"ApiConnection\"}}}}" }
                """);
        });
        using var httpClient = new HttpClient(handler);
        var service = new PowerPlatformFlowService(httpClient, NullLogger<PowerPlatformFlowService>.Instance);

        var result = await service.GetFlowDefinitionAsync(
            "power-platform-token",
            "dataverse-token",
            "env-1",
            "9b4325dc-0a92-4f99-8386-eae4d9f0f29f",
            CancellationToken.None);

        Assert.Equal("Notifier une demande", result.DisplayName);
        Assert.Equal("env-1", result.EnvironmentId);
        Assert.Equal("tenant-1", result.TenantId);
        Assert.Contains("\"definition\"", result.DefinitionJson, StringComparison.Ordinal);
        Assert.Contains("\"actions\"", result.DefinitionJson, StringComparison.Ordinal);
    }

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) =>
            _responseFactory = responseFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responseFactory(request));
    }
}
