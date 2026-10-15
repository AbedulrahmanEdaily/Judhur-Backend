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
            .Include(p => p.PropertyImages)
            .Where(p => p.SellerId == userId)
            .ToListAsync(cancellationToken);
        var imagePublicIds = listings.SelectMany(p => p.PropertyImages).Select(i => i.PublicId).ToList();
        var documentPublicIds = listings.Select(p => p.OwnershipDocumentPublicId).OfType<string>().ToList();
        foreach (var listing in listings)
        {
            listing.Delete(nowUtc);
        }
        await _context.SaveChangesAsync(cancellationToken);

        if (deleteResult.Value.ProfileImagePublicId is { } profileImagePublicId)
        {
            imagePublicIds.Add(profileImagePublicId);
        }
        _dispatcher.Defer(ct => RemoveFilesAsync(userId, imagePublicIds, documentPublicIds, ct));

        _logger.LogInformation("Account {UserId} deleted by its owner: {DeletedListings} listings removed", userId, listings.Count);
        return Result.Deleted;
    }

    private async Task RemoveFilesAsync(Guid userId, List<string> imagePublicIds, List<string> documentPublicIds, CancellationToken cancellationToken)
    {
        var failed = 0;
        foreach (var publicId in imagePublicIds)
        {
            if ((await _fileStorage.DeleteFileAsync(publicId, cancellationToken)).IsError)
            {
                failed++;
            }
        }
        foreach (var publicId in documentPublicIds)
        {
            if ((await _fileStorage.DeletePrivateDocumentAsync(publicId, cancellationToken)).IsError)
            {
                failed++;
            }
        }
        if (failed > 0)
        {
            _logger.LogWarning("{Failed} of {Total} files of deleted user {UserId} could not be removed", failed, imagePublicIds.Count + documentPublicIds.Count, userId);
            return;
        }
        _logger.LogInformation("{Total} files of deleted user {UserId} removed", imagePublicIds.Count + documentPublicIds.Count, userId);
    }
}
