namespace PADocGenerator.Api.Models.Entities;

/// <summary>
/// Une version figée du contenu de la documentation. Créée automatiquement
/// à chaque enregistrement (génération initiale ou modification manuelle
/// avant enregistrement, cf. section 4 : "Possibilité de modifier la
/// documentation générée avant enregistrement" + "Conservation des ...
/// historique de versions pour chaque flux.").
/// </summary>
public class DocumentationVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentationId { get; set; }
    public Documentation? Documentation { get; set; }

    public int VersionNumber { get; set; }

    /// <summary>Résumé fonctionnel en langage naturel.</summary>
    public string FunctionalSummary { get; set; } = string.Empty;

    /// <summary>Contenu structuré (sections/titres/sous-titres/tableaux) sérialisé en JSON,
    /// produit par le module de mise en forme.</summary>
    public string StructuredContentJson { get; set; } = string.Empty;

    /// <summary>true si cette version a été modifiée manuellement par l'utilisateur
    /// par rapport à la sortie brute de l'IA.</summary>
    public bool IsManuallyEdited { get; set; }

    public Guid EditedByUserId { get; set; }
    public ApplicationUser? EditedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Commentaire optionnel décrivant la modification (utile pour l'historique).</summary>
    public string? ChangeNote { get; set; }
}
