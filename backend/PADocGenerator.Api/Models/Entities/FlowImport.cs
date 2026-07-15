using Microsoft.EntityFrameworkCore;

namespace PADocGenerator.Api.Models.Entities;

/// <summary>
/// Représente un flux Power Automate importé (module d'importation, section 6).
/// Le JSON brut est conservé tel quel (colonne JSONB PostgreSQL, cf. section 5 :
/// "PostgreSQL ... offrant le type JSONB pour stocker et interroger les flux JSON
/// sans imposer de schéma spécifique.").
/// </summary>
public class FlowImport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>Contenu JSON brut du flux Power Automate exporté.</summary>
    public string RawJson { get; set; } = string.Empty;

    /// <summary>Nombre d'actions détectées lors de la lecture (aperçu rapide).</summary>
    public int ActionsCount { get; set; }

    public Guid ImportedByUserId { get; set; }
    public ApplicationUser? ImportedByUser { get; set; }

    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>true si le JSON a passé la validation de format (module d'importation).</summary>
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }

    public ICollection<Documentation> Documentations { get; set; } = new List<Documentation>();
}
