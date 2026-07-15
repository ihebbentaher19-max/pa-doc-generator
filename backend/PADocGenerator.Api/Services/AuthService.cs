using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PADocGenerator.Api.Data;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Models.Entities;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PADocGenerator";
    public string Audience { get; set; } = "PADocGenerator";
    public int ExpirationMinutes { get; set; } = 480;
}

/// <summary>
/// Implémentation du module de gestion des rôles (section 6) côté authentification :
/// connexion + inscription, émission d'un jeton JWT porteur du rôle
/// (administrateur / utilisateur) utilisé ensuite par les Controllers pour
/// protéger les fonctions sensibles ([Authorize(Roles = "Administrateur")]).
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwtOptions;

    public AuthService(AppDbContext db, IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, ct);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return null;

        return GenerateLoginResponse(user);
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterUserDto request, CancellationToken ct = default)
    {
        var alreadyExists = await _db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (alreadyExists)
            throw new InvalidOperationException("Un compte existe déjà avec cet e-mail.");

        // Le premier compte créé sur la plateforme devient automatiquement
        // Administrateur ; les suivants sont Utilisateur par défaut (un
        // administrateur pourra ensuite promouvoir d'autres comptes).
        var isFirstUser = !await _db.Users.AnyAsync(ct);

        var user = new ApplicationUser
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = isFirstUser ? UserRole.Administrateur : UserRole.Utilisateur
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return GenerateLoginResponse(user);
    }

    private LoginResponseDto GenerateLoginResponse(ApplicationUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponseDto(tokenString, expiresAt, user.FullName, user.Role.ToString());
    }

    // PBKDF2 - évite d'ajouter une dépendance supplémentaire type BCrypt.
    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}
