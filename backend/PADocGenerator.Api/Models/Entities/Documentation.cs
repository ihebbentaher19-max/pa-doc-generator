namespace PADocGenerator.Api.Models.Entities;

/// <summary>
/// Documentation générée pour un flux (module de génération + module de mise en forme
/// + module de gestion documentaire, section 6). Chaque Documentation référence son
/// flux d'origine et conserve un historique de versions (DocumentationVersion).
/// </summary>
public class Documentation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FlowImportId { get; set; }
    public FlowImport? FlowImport { get; set; }

    public string Title { get; set; } = string.Empty;
    public DocumentationStatus Status { get; set; } = DocumentationStatus.Brouillon;

    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Numéro de la version actuellement affichée / active.</summary>
    public int CurrentVersionNumber { get; set; } = 1;

    public ICollection<DocumentationVersion> Versions { get; set; } = new List<DocumentationVersion>();
}
