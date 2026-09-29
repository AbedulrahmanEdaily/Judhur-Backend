using Judhur.Application.Common.Models;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Properties.Queries.GetPendingProperties;

public sealed record GetPendingPropertiesQuery(int Page, int PageSize) : IRequest<Result<PaginatedList<PendingPropertyDto>>>;