namespace PADocGenerator.Api.Models.Entities;

/// <summary>
/// Statut de la documentation, cf. section 6 - Module de gestion documentaire :
/// "Conserve les métadonnées du flux (... statut de la documentation :
/// brouillon, validé, archivé)."
/// </summary>
public enum DocumentationStatus
{
    Brouillon = 0,
    Valide = 1,
    Archive = 2
}
