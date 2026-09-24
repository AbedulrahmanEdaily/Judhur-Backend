using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Properties.Queries.GetMyProperties;

public sealed record GetMyPropertiesQuery : IRequest<Result<List<MyPropertyDto>>>;
