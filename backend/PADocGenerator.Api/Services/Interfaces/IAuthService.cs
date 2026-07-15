using PADocGenerator.Api.Models.Dtos;

namespace PADocGenerator.Api.Services.Interfaces;

/// <summary>
/// Module de gestion des rôles (section 6) : authentification et émission
/// de jetons JWT porteurs du rôle (administrateur / utilisateur).
/// </summary>
public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<LoginResponseDto> RegisterAsync(RegisterUserDto request, CancellationToken ct = default);
}
