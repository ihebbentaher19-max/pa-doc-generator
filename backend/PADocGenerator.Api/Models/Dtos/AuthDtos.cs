namespace PADocGenerator.Api.Models.Dtos;

public record LoginRequestDto(string Email, string Password);
public record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string FullName, string Role);
public record RegisterUserDto(string FullName, string Email, string Password);
