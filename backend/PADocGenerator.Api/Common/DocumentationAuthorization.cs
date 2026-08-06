using System.Security.Claims;

namespace PADocGenerator.Api.Common;

/// <summary>
/// Module de gestion des rôles (section 6 du cahier des charges) : « protège les
/// fonctions sensibles [...] tout en permettant aux utilisateurs de modifier et
/// d'exporter leurs propres documentations générées ». Un Administrateur peut agir
/// sur toutes les documentations ; un Utilisateur uniquement sur celles qu'il a
/// créées.
/// </summary>
public static class DocumentAuthorization
{
    public static bool CanModify(this ClaimsPrincipal user, Guid documentCreatedByUserId)
    {
        if (user.IsInRole("Administrateur"))
            return true;

        return user.GetUserId() == documentCreatedByUserId;
    }
}