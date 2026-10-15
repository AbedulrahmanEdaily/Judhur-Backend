using Judhur.Application.Common.Models;
using Judhur.Application.Features.Users.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Users.Queries.GetUsers;

public sealed record GetUsersQuery(string? Search, AdminUserStatus Status, int Page, int PageSize) : IRequest<Result<PaginatedList<AdminUserDto>>>;
