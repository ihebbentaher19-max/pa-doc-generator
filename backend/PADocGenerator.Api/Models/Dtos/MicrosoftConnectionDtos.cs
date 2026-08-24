namespace PADocGenerator.Api.Models.Dtos;

public sealed record MicrosoftConnectionStatusDto(
    bool Configured,
    bool Connected,
    string? Message
);