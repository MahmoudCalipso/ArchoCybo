namespace ArchoCybo.Application.DTOs;

public record CreateUserDto(string Username, string Email, string Password);
public record UpdateUserDto(Guid Id, string? Email, bool? IsActive);
