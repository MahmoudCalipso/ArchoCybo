using ArchoCybo.Domain.Entities.Security;

namespace ArchoCybo.Application.DTOs;

public record UserListData(Guid Id, string Username, string Email, bool IsActive, IEnumerable<string> Roles, IEnumerable<string> Permissions);

public record UserFilterQuery(string? Search, int Page = 1, int PageSize = 20);
