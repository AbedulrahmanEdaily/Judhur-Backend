
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Queries.GetMyProfile;


public sealed record GetMyProfileQuery() : IRequest<Result<MyProfileDto>>;