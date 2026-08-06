using Microsoft.AspNetCore.Mvc;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Inscription d'un nouvel utilisateur. Le tout premier compte créé
    /// devient automatiquement Administrateur.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDto>> Register(RegisterUserDto request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, ct);
            return Ok(result);
        }
        catch (BusinessException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        if (result is null)
            return Unauthorized(new { message = UserMessages.InvalidCredentials });

        return Ok(result);
    }
}
