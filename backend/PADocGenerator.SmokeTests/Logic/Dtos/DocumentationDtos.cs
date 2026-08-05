namespace PADocGenerator.Api.Models.Dtos;

public record GenerateDocumentationRequestDto(Guid FlowImportId);

public record DocumentationStepDto(
    string StepName,
    string Description,
    bool IsImportant
);

public record DocumentationDependencyDto(
    string From,
    string To,
    string ExplanationText
);

/// <summary>
/// Sortie structurée produite par le module de génération puis organisée
/// par le module de mise en forme (sections, titres, sous-titres, tableaux).
/// </summary>
public record DocumentationContentDto(
    string FunctionalSummary,
    List<DocumentationStepDto> Steps,
    List<DocumentationDependencyDto> Dependencies,
    List<string> ImportantSteps
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
    string? ChangeNote
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
