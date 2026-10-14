using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Conversations.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Conversations;
using Judhur.Domain.Properties.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Conversations.Queries.GetConversationById;

public sealed class GetConversationByIdQueryHandler(IUser user, IAppDbContext context, ILogger<GetConversationByIdQueryHandler> logger, IIdentityService identityService)
    : IRequestHandler<GetConversationByIdQuery, Result<ConversationDetailsDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetConversationByIdQueryHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;

    public async Task<Result<ConversationDetailsDto>> Handle(GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get conversation rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var row = await _context.Conversations
            .AsNoTracking()
            .Where(c => c.Id == request.ConversationId && (c.BuyerId == userId || c.SellerId == userId))
            .Join(
                _context.Properties.IgnoreQueryFilters(),
                c => c.PropertyId,
                p => p.Id,
                (c, p) => new
                {
                    c.Id,
                    c.BuyerId,
                    c.SellerId,
                    c.CreatedAtUtc,
                    Property = new ConversationPropertyDto(
                        p.Id,
                        p.Title,
                        p.PropertyImages
                            .Where(i => i.IsMainImage)
                            .Select(i => i.FileUrl)
                            .FirstOrDefault(),
                        p.ModerationStatus == ModerationStatus.Approved && p.IsActive && !p.IsDeleted)
                })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            _logger.LogWarning("Get conversation rejected: conversation {ConversationId} not found for user {UserId}", request.ConversationId, userId);
            return ConversationErrors.NotFound;
        }

        var otherPartyId = row.BuyerId == userId ? row.SellerId : row.BuyerId;
        var participants = await _identityService.GetUsersInfoAsync([otherPartyId], cancellationToken);
        var otherParty = participants.TryGetValue(otherPartyId, out var info)
            ? new ChatParticipantDto(info.Id, info.FullName, info.ProfileImageUrl)
            : null;

        _logger.LogInformation("Conversation {ConversationId} retrieved for user {UserId}", row.Id, userId);
        return new ConversationDetailsDto(
            row.Id,
            row.Property,
            otherParty,
            row.BuyerId == userId ? ConversationRole.Buyer : ConversationRole.Seller,
            row.CreatedAtUtc);
    }
}
