using Judhur.Application.Features.Conversations.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Conversations.Commands.StartConversation;

public sealed record StartConversationCommand(Guid PropertyId, string Message) : IRequest<Result<StartConversationResultDto>>;
