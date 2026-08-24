using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services;

namespace PADocGenerator.Api.Controllers;

[ApiController]
[Route("api/microsoft-auth")]
[Authorize]
public sealed class MicrosoftAuthController : ControllerBase
{
    private readonly MicrosoftEntraOptions _options;

    public MicrosoftAuthController(
        IOptions<MicrosoftEntraOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet("status")]
    public ActionResult<MicrosoftConnectionStatusDto> GetStatus()
    {
        var configured =
            !string.IsNullOrWhiteSpace(_options.TenantId) &&
            !string.IsNullOrWhiteSpace(_options.ClientId);

        var message = configured
            ? "Microsoft Entra ID est configuré."
            : "Microsoft Entra ID n'est pas configuré.";

        return Ok(new MicrosoftConnectionStatusDto(
            Configured: configured,
            Connected: false,
            Message: message
        ));
    }
}