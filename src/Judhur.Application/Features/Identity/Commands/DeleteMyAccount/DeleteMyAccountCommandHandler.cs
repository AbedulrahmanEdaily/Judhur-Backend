using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.DeleteMyAccount;

public sealed class DeleteMyAccountCommandHandler(
    IUser user,
    IAppDbContext context,
    IIdentityService identityService,
    IFileStorage fileStorage,
    TimeProvider timeProvider,
    IDeferredDispatcher dispatcher,
    ILogger<DeleteMyAccountCommandHandler> logger) : IRequestHandler<DeleteMyAccountCommand, Result<Deleted>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly IIdentityService _identityService = identityService;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly ILogger<DeleteMyAccountCommandHandler> _logger = logger;

    public async Task<Result<Deleted>> Handle(DeleteMyAccountCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Delete account rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var deleteResult = await _identityService.DeleteAccountAsync(userId, request.Password, nowUtc, cancellationToken);
        if (deleteResult.IsError)
        {
            _logger.LogWarning("Delete account failed for user {UserId}: {ErrorCode}", userId, deleteResult.TopError.Code);
            return deleteResult.Errors;
        }

        await _context.RefreshTokens.Where(r => r.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _context.Favorites.Where(f => f.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _context.Notifications.Where(n => n.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        var listings = await _context.Properties
            .Where(p => p.SellerId == userId)
            .ToListAsync(cancellationToken);
        foreach (var listing in listings)
        {
            listing.Delete(nowUtc);
        }
        await _context.SaveChangesAsync(cancellationToken);

        if (deleteResult.Value.ProfileImagePublicId is { } publicId)
        {
            _dispatcher.Defer(async ct =>
            {
                var removed = await _fileStorage.DeleteFileAsync(publicId, ct);
                if (removed.IsError)
                {
                    _logger.LogWarning("Profile image {PublicId} of deleted user {UserId} could not be removed: {ErrorCode}", publicId, userId, removed.TopError.Code);
                }
            });
        }

        _logger.LogInformation("Account {UserId} deleted by its owner: {DeletedListings} listings removed", userId, listings.Count);
        return Result.Deleted;
    }
}
