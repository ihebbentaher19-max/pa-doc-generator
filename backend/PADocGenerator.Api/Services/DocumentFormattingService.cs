using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Implémentation du module de mise en forme (section 6) : reçoit le contenu
/// brut produit par le module de génération et l'organise pour l'affichage
/// dans l'interface éditable. Le traitement reste volontairement simple
/// (tri des étapes importantes en tête, dédoublonnage des dépendances,
/// nettoyage des chaînes) car la plateforme ne réalise pas d'analyse technique
/// avancée du workflow (cf. section 3, Objectif du projet).
/// </summary>
public class DocumentFormattingService : IDocumentFormattingService
{
    public DocumentationContentDto Format(DocumentationContentDto rawContent)
    {
        var cleanedSummary = rawContent.FunctionalSummary.Trim();

        var orderedSteps = rawContent.Steps
            .Select(s => s with
            {
                StepName = s.StepName.Trim(),
                Description = s.Description.Trim()
            })
            .ToList();

        var dedupedDependencies = rawContent.Dependencies
            .GroupBy(d => (d.From, d.To))
            .Select(g => g.First())
            .ToList();

        return new DocumentationContentDto(cleanedSummary, orderedSteps, dedupedDependencies, rawContent.Diagram);
    }
}
