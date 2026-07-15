namespace PADocGenerator.Api.Models.Entities;

/// <summary>
/// Section 4 / 6 du cahier des charges : "Gestion des rôles avec deux profils :
/// administrateur et utilisateur."
/// </summary>
public enum UserRole
{
    Utilisateur = 0,
    Administrateur = 1
}
