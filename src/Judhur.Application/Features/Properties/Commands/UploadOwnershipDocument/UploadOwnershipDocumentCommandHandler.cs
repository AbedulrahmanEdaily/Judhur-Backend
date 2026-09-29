using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.UploadOwnershipDocument;

public sealed class UploadOwnershipDocumentCommandHandler(IUser user, IAppDbContext context, IFileStorage fileStorage, ILogger<UploadOwnershipDocumentCommandHandler> logger) : IRequestHandler<UploadOwnershipDocumentCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly ILogger<UploadOwnershipDocumentCommandHandler> _logger = logger;

    public async Task<Result<Updated>> Handle(UploadOwnershipDocumentCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Upload ownership document rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Upload ownership document rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }
        var oldPublicId = property.OwnershipDocumentPublicId;
        var uploadResult = await _fileStorage.UploadPrivateDocumentAsync(
            request.Content,
            request.FileName,
            $"judhur/ownership-documents/{property.Id}",
            cancellationToken);
        if (uploadResult.IsError)
        {
            return uploadResult.Errors;
        }
        var newPublicId = uploadResult.Value;
        var setResult = property.SetOwnershipDocument(newPublicId);
        if (setResult.IsError)
        {
            _logger.LogWarning("Upload ownership document for property {PropertyId} failed: {ErrorCode}", property.Id, setResult.TopError.Code);
            await _fileStorage.DeletePrivateDocumentAsync(newPublicId, CancellationToken.None);
            return setResult.Errors;
        }
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _fileStorage.DeletePrivateDocumentAsync(newPublicId, CancellationToken.None);
            throw;
        }
        if (oldPublicId is not null)
        {
            await _fileStorage.DeletePrivateDocumentAsync(oldPublicId, CancellationToken.None);
        }
        _logger.LogInformation("Ownership document uploaded for property {PropertyId} by seller {SellerId}", property.Id, sellerId);
        return Result.Updated;
    }
}
