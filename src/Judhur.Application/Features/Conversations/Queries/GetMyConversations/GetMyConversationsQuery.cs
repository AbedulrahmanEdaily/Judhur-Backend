using Judhur.Application.Common.Models;
using Judhur.Application.Features.Conversations.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Conversations.Queries.GetMyConversations;

public sealed record GetMyConversationsQuery(int Page, int PageSize) : IRequest<Result<PaginatedList<ConversationSummaryDto>>>;
