using PADocGenerator.Api.Models.Dtos;

namespace PADocGenerator.Api.Services.Interfaces;

/// <summary>
/// Module de mise en forme (section 6) : reçoit le contenu brut produit par
/// l'IA et l'organise en sections/titres/sous-titres/tableaux afin d'améliorer
/// sa lisibilité avant affichage dans l'interface éditable.
/// </summary>
public interface IDocumentFormattingService
{
    DocumentationContentDto Format(DocumentationContentDto rawContent);
}
