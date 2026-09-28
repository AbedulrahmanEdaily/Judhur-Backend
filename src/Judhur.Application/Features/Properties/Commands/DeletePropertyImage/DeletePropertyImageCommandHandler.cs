using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.DeletePropertyImage;

public sealed class DeletePropertyImageCommandHandler(IAppDbContext context, ILogger<DeletePropertyImageCommandHandler> logger, IUser user, IFileStorage fileStorage)
    : IRequestHandler<DeletePropertyImageCommand, Result<Deleted>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<DeletePropertyImageCommandHandler> _logger = logger;
    private readonly IUser _user = user;
    private readonly IFileStorage _fileStorage = fileStorage;

    public async Task<Result<Deleted>> Handle(DeletePropertyImageCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Delete property image rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var property = await _context.Properties
            .Include(p => p.PropertyImages)
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Delete property image rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }
        var image = property.PropertyImages.FirstOrDefault(i => i.Id == request.ImageId);
        if (image is null)
        {
            _logger.LogWarning("Delete property image rejected: image {ImageId} not found on property {PropertyId}", request.ImageId, property.Id);
            return PropertyErrors.ImageNotFound;
        }
        // Kept before RemoveImage: the file is deleted from storage only after the database save.
        var publicId = image.PublicId;
        var result = property.RemoveImage(request.ImageId);
        if (result.IsError)
        {
            _logger.LogWarning("Delete image {ImageId} from property {PropertyId} failed: {ErrorCode}", request.ImageId, property.Id, result.TopError.Code);
            return result.Errors;
        }
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Image {ImageId} deleted from property {PropertyId} by seller {SellerId}", request.ImageId, property.Id, sellerId);
        // Database first, storage second: a failure here only leaves an unused file,
        // never a listing pointing at a missing image.
        await _fileStorage.DeleteFileAsync(publicId, CancellationToken.None);
        return Result.Deleted;
    }
}