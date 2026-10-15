namespace Judhur.Application.Features.Users.Dto;

public sealed record AdminUsersFilter(string? Search, AdminUserStatus Status, int Page, int PageSize);
