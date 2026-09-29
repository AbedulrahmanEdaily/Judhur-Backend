using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.SetMainPropertyImage;

public sealed class SetMainPropertyImageCommandHandler(
    IUser user,
    IAppDbContext context,
    ILogger<SetMainPropertyImageCommandHandler> logger) : IRequestHandler<SetMainPropertyImageCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<SetMainPropertyImageCommandHandler> _logger = logger;

    public async Task<Result<Updated>> Handle(SetMainPropertyImageCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Set main property image rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var property = await _context.Properties
            .Include(p => p.PropertyImages)
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Set main property image rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }
        var result = property.SetMainImage(request.ImageId);
        if (result.IsError)
        {
            _logger.LogWarning("Set main image {ImageId} on property {PropertyId} failed: {ErrorCode}", request.ImageId, property.Id, result.TopError.Code);
            return result.Errors;
        }
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Image {ImageId} set as main image of property {PropertyId} by seller {SellerId}", request.ImageId, property.Id, sellerId);
        return Result.Updated;
    }
}