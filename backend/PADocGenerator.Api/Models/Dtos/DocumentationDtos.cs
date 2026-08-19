namespace PADocGenerator.Api.Models.Dtos;

public record GenerateDocumentationRequestDto(Guid FlowImportId);

public record DocumentationVariableDto(
    string Name,
    string? Value,
    string Description
);

public record DocumentationStepDto(
    string StepId,
    string StepName,
    string StepType,
    string? Connector,
    string Description,
    string Purpose,
    List<DocumentationVariableDto> Variables,
    Dictionary<string, string> Inputs
);

public record DocumentationDependencyDto(
    string From,
    string To,
    string ExplanationText,
    string RelationshipType
);
public record DocumentationDiagramNodeDto(
    string Id,
    string Name,
    string Type,
    string NodeType
);

public record DocumentationDiagramEdgeDto(
    string SourceId,
    string TargetId,
    string? Label
);

public record DocumentationDiagramDto(
    List<DocumentationDiagramNodeDto> Nodes,
    List<DocumentationDiagramEdgeDto> Edges
);

/// <summary>
/// Sortie structurée produite par le module de génération puis organisée
/// par le module de mise en forme (sections, titres, sous-titres, tableaux).
/// </summary>
public record DocumentationContentDto(
    string FunctionalSummary,
    List<DocumentationStepDto> Steps,
    List<DocumentationDependencyDto> Dependencies,
    DocumentationDiagramDto Diagram
);

public record DocumentationSummaryDto(
    Guid Id,
    string Title,
    string FlowName,
    string Status,
    int CurrentVersionNumber,
    string CreatedByUserName,
    DateTime UpdatedAtUtc
);

public record DocumentationDetailDto(
    Guid Id,
    string Title,
    string FlowName,
    Guid FlowImportId,
    string Status,
    int CurrentVersionNumber,
    DocumentationContentDto Content,
    Guid CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record UpdateDocumentationDto(
    string Title,
    DocumentationContentDto Content,
    string? ChangeNote
);

public record ChangeStatusDto(string NewStatus);

public record DocumentationVersionSummaryDto(
    int VersionNumber,
    bool IsManuallyEdited,
    string EditedByFullName,
    DateTime CreatedAtUtc,
    string? ChangeNote,
    DocumentationContentDto Content
);

public record DocumentationVersionDetailDto(
    Guid DocumentationId,
    int VersionNumber,
    bool IsManuallyEdited,
    string EditedByFullName,
    DateTime CreatedAtUtc,
    string? ChangeNote,
    DocumentationContentDto Content
);

public record SearchDocumentationQueryDto(
    string? Keyword,
    string? Status,
    int Page = 1,
    int PageSize = 20
);

public record DashboardStatsDto(
    int TotalDocumentations,
    int TotalFlowsImported,
    int DraftCount,
    int ValidatedCount,
    int ArchivedCount,
    List<DocumentationSummaryDto> RecentDocumentations
);