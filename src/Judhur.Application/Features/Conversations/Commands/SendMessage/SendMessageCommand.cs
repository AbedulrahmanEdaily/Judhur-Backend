using System.Text.Json.Serialization;

using Judhur.Application.Features.Conversations.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Conversations.Commands.SendMessage;

public sealed record SendMessageCommand([property: JsonIgnore] Guid ConversationId, string Content) : IRequest<Result<MessageDto>>;
