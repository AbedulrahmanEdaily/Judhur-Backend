using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Properties.Queries.GetMyPropertyById;

public sealed record GetMyPropertyByIdQuery(Guid PropertyId) : IRequest<Result<MyPropertyDetailsDto>>;
