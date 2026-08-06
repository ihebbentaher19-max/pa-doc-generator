using System.Security.Cryptography;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Hachage et vérification de mots de passe en PBKDF2 (évite d'ajouter une
/// dépendance supplémentaire type BCrypt). Extrait de <see cref="AuthService"/>
/// dans sa propre classe statique car cette logique ne dépend que de la
/// bibliothèque de base .NET (<c>System.Security.Cryptography</c>) et peut donc
/// être testée réellement même sans accès à NuGet (voir
/// <c>PADocGenerator.SmokeTests</c>).
/// </summary>
public static class PasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;

        byte[] salt, expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            expectedHash = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}
