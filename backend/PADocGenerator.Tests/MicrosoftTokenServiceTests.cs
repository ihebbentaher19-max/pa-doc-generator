using Microsoft.Extensions.Options;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests.Services;

public class MicrosoftTokenServiceTests
{
    [Fact]
    public async Task GetPowerPlatformTokenAsync_WhenNotConfigured_ThrowsBusinessException()
    {
        var options = Options.Create(new MicrosoftEntraOptions
        {
            TenantId = "",
            ClientId = ""
        });

        var service = new MicrosoftTokenService(options);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetPowerPlatformTokenAsync(
                "user-id",
                CancellationToken.None));
    }

    [Fact]
    public async Task GetDataverseTokenAsync_WhenEnvironmentUrlIsEmpty_ThrowsBusinessException()
    {
        var options = Options.Create(new MicrosoftEntraOptions
        {
            TenantId = "tenant-id",
            ClientId = "client-id"
        });

        var service = new MicrosoftTokenService(options);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetDataverseTokenAsync(
                "user-id",
                "",
                CancellationToken.None));
    }
}