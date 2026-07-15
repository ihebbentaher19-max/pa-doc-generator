using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Models.FlowSchema;

namespace PADocGenerator.Api.Services.Interfaces;

/// <summary>
/// Module de génération de documentation (section 6) : construit le prompt,
/// interroge le modèle d'IA (Azure OpenAI) et produit le résumé fonctionnel,
/// la description de chaque étape, les dépendances et les étapes importantes.
/// </summary>
public interface IAiDocumentationService
{
    Task<DocumentationContentDto> GenerateAsync(ParsedFlow parsedFlow, CancellationToken cancellationToken = default);
}
