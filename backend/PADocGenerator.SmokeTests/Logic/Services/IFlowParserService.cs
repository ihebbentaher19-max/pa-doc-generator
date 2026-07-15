using PADocGenerator.Api.Models.FlowSchema;

namespace PADocGenerator.Api.Services.Interfaces;

/// <summary>
/// Module de lecture et préparation des données (section 6) : lit le JSON,
/// extrait actions/conditions/variables/connecteurs et prépare les données
/// pour la génération.
/// </summary>
public interface IFlowParserService
{
    ParsedFlow Parse(string jsonContent);
}
