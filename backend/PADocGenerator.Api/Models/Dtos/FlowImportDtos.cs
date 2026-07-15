namespace PADocGenerator.Api.Models.Dtos;

public record FlowImportRequestDto(string FileName, string JsonContent);

public record FlowImportResultDto(
    Guid FlowImportId,
    string Name,
    bool IsValid,
    string? ValidationError,
    int ActionsCount,
    DateTime ImportedAtUtc
);
