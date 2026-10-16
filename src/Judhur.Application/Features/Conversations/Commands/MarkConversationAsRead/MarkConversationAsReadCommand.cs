using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Conversations.Commands.MarkConversationAsRead;

public sealed record MarkConversationAsReadCommand(Guid ConversationId, Guid? UpToMessageId = null) : IRequest<Result<Updated>>;
