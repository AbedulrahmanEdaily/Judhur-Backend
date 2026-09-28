using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.AddPropertyImage;

public sealed class AddPropertyImageCommandHandler(IUser user, IAppDbContext context, IFileStorage fileStorage, ILogger<AddPropertyImageCommandHandler> logger) : IRequestHandler<AddPropertyImageCommand, Result<PropertyImageDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly ILogger<AddPropertyImageCommandHandler> _logger = logger;

    public async Task<Result<PropertyImageDto>> Handle(AddPropertyImageCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Add property image rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var property = await _context.Properties
            .Include(p => p.PropertyImages)
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Add property image rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }
        if (property.PropertyImages.Count >= Property.MaxImages)
        {
            _logger.LogWarning("Add property image rejected: property {PropertyId} already has the maximum number of images", property.Id);
            return PropertyErrors.MaxImagesReached;
        }
        var uploadResult = await _fileStorage.UploadImageAsync(request.Content,
            request.FileName,
            $"judhur/properties/{property.Id}",
            cancellationToken
        );
        if (uploadResult.IsError)
        {
            return uploadResult.Errors;
        }
        var storedFile = uploadResult.Value;
        var imageId = Guid.CreateVersion7();
        var addResult = property.AddImage(imageId, storedFile.Url, storedFile.PublicId, request.IsMainImage);
        if (addResult.IsError)
        {
            _logger.LogWarning("Add property image {PropertyId} failed: {ErrorCode}", property.Id, addResult.TopError.Code);
            await _fileStorage.DeleteFileAsync(storedFile.PublicId, cancellationToken);
            return addResult.Errors;
        }
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _fileStorage.DeleteFileAsync(storedFile.PublicId, CancellationToken.None);
            throw;
        }
        var image = property.PropertyImages.First(i => i.Id == imageId);
        _logger.LogInformation("Image {ImageId} added to property {PropertyId} by seller {SellerId}", image.Id, property.Id, sellerId);
        return new PropertyImageDto(image.Id, image.FileUrl, image.DisplayOrder, image.IsMainImage);
    }
}