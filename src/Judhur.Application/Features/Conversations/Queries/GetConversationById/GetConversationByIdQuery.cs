using Judhur.Application.Features.Conversations.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Conversations.Queries.GetConversationById;

public sealed record GetConversationByIdQuery(Guid ConversationId) : IRequest<Result<ConversationDetailsDto>>;
