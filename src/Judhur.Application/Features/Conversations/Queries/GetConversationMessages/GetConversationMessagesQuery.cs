using Judhur.Application.Features.Conversations.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Conversations.Queries.GetConversationMessages;

public sealed record GetConversationMessagesQuery(Guid ConversationId, Guid? Before, int Limit) : IRequest<Result<MessagesPageDto>>;
