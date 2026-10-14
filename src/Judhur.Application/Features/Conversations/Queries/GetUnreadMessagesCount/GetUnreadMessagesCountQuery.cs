using Judhur.Application.Features.Conversations.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Conversations.Queries.GetUnreadMessagesCount;

public sealed record GetUnreadMessagesCountQuery : IRequest<Result<UnreadMessagesCountDto>>;
