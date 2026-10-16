using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Conversations.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Conversations.Queries.GetMyConversations;

public sealed class GetMyConversationsQueryHandler(IUser user, IAppDbContext context, ILogger<GetMyConversationsQueryHandler> logger, IIdentityService identityService)
    : IRequestHandler<GetMyConversationsQuery, Result<PaginatedList<ConversationSummaryDto>>>
{
    private const int PreviewLength = 100;

    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetMyConversationsQueryHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;

    public async Task<Result<PaginatedList<ConversationSummaryDto>>> Handle(GetMyConversationsQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get my conversations rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        if (!Pagination.IsPageValid(request.Page))
        {
            _logger.LogWarning("Get my conversations rejected: invalid page {Page}", request.Page);
            return ApplicationError.PageInvalid;
        }
        if (request.PageSize is <= 0 or > Pagination.MaxPageSize)
        {
            _logger.LogWarning("Get my conversations rejected: invalid page size {PageSize}", request.PageSize);
            return ApplicationError.PageSizeInvalid;
        }

        var query = _context.Conversations
            .AsNoTracking()
            .Where(c => (c.BuyerId == userId || c.SellerId == userId) && c.LastMessageAtUtc != null);
        var count = await query.CountAsync(cancellationToken);
        var rows = await query
            .Join(
                _context.Properties.IgnoreQueryFilters(),
                c => c.PropertyId,
                p => p.Id,
                (c, p) => new { Conversation = c, Property = p })
            .OrderByDescending(x => x.Conversation.LastMessageAtUtc)
            .ThenBy(x => x.Conversation.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Conversation.Id,
                x.Conversation.BuyerId,
                x.Conversation.SellerId,
                x.Conversation.LastMessageAtUtc,
                Property = new ConversationPropertyDto(
                    x.Property.Id,
                    x.Property.Title,
                    x.Property.PropertyImages
                        .Where(i => i.IsMainImage)
                        .Select(i => i.FileUrl)
                        .FirstOrDefault(),
                    x.Property.ModerationStatus == ModerationStatus.Approved && x.Property.IsActive && !x.Property.IsDeleted),
                LastMessage = x.Conversation.Messages
                    .OrderByDescending(m => m.SentAtUtc)
                    .ThenByDescending(m => m.Id)
                    .Select(m => new { m.Content, m.SenderId })
                    .FirstOrDefault(),
                UnreadCount = x.Conversation.Messages
                    .Count(m => m.SenderId != userId && m.ReadAtUtc == null)
            })
            .ToListAsync(cancellationToken);

        var otherPartyIds = rows
            .Select(r => r.BuyerId == userId ? r.SellerId : r.BuyerId)
            .Distinct()
            .ToList();
        var participants = await _identityService.GetUsersInfoAsync(otherPartyIds, cancellationToken);

        var items = rows
            .Select(r =>
            {
                var otherPartyId = r.BuyerId == userId ? r.SellerId : r.BuyerId;
                var otherParty = participants.TryGetValue(otherPartyId, out var info)
                    ? new ChatParticipantDto(info.Id, info.FullName, info.ProfileImageUrl)
                    : null;
                return new ConversationSummaryDto(
                    r.Id,
                    r.Property,
                    otherParty,
                    r.BuyerId == userId ? ConversationRole.Buyer : ConversationRole.Seller,
                    r.LastMessage is null ? null : Preview(r.LastMessage.Content),
                    r.LastMessage?.SenderId == userId,
                    r.LastMessageAtUtc,
                    r.UnreadCount);
            })
            .ToList();

        _logger.LogInformation("Retrieved {Count} of {TotalCount} conversations for user {UserId} (page {Page}, size {PageSize})", items.Count, count, userId, request.Page, request.PageSize);
        return new PaginatedList<ConversationSummaryDto>
        {
            Items = items,
            PageNumber = request.Page,
            PageSize = request.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)request.PageSize)
        };
    }

    private static string Preview(string content)
        => content.Length <= PreviewLength ? content : string.Concat(content.AsSpan(0, PreviewLength), "…");
}
