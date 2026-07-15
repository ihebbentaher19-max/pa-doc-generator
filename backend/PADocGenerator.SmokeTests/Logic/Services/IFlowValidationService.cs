namespace PADocGenerator.Api.Services.Interfaces;

public record FlowValidationResult(bool IsValid, string? Error);

/// <summary>
/// Module d'importation (section 6) : "Vérifie que le fichier JSON est valide
/// et conforme au format attendu."
/// </summary>
public interface IFlowValidationService
{
    FlowValidationResult Validate(string jsonContent);
}
