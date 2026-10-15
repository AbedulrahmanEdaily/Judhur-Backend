using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Users.Events;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Users.Commands.BanUser;

public sealed class BanUserCommandHandler(
    IUser user,
    IAppDbContext context,
    ILogger<BanUserCommandHandler> logger,
    IIdentityService identityService,
    TimeProvider timeProvider,
    IDeferredDispatcher dispatcher,
    IPublisher publisher) : IRequestHandler<BanUserCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<BanUserCommandHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<Updated>> Handle(BanUserCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } adminId)
        {
            _logger.LogWarning("Ban user rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        if (request.UserId == adminId)
        {
            _logger.LogWarning("Ban user rejected: admin {AdminId} tried to ban their own account", adminId);
            return ApplicationError.CannotBanSelf;
        }

        var banResult = await _identityService.BanUserAsync(request.UserId, request.Reason, _timeProvider.GetUtcNow(), cancellationToken);
        if (banResult.IsError)
        {
            _logger.LogWarning("Ban user {UserId} by admin {AdminId} failed: {ErrorCode}", request.UserId, adminId, banResult.TopError.Code);
            return banResult.Errors;
        }

        var revokedSessions = await _context.RefreshTokens
            .Where(r => r.UserId == request.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        var activeListings = await _context.Properties
            .Where(p => p.SellerId == request.UserId && p.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var listing in activeListings)
        {
            listing.Deactivate();
        }
        await _context.SaveChangesAsync(cancellationToken);

        var banned = new UserBanned(request.UserId, request.Reason.Trim(), activeListings.Count);
        _dispatcher.Defer(ct => _publisher.Publish(banned, ct));
        _logger.LogInformation(
            "User {UserId} banned by admin {AdminId}: {RevokedSessions} sessions revoked, {DeactivatedListings} listings deactivated",
            request.UserId,
            adminId,
            revokedSessions,
            activeListings.Count);
        return Result.Updated;
    }
}
