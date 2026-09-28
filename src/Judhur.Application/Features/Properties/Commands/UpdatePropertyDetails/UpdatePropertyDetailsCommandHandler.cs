using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.UpdatePropertyDetails;

public sealed class UpdatePropertyDetailsCommandHandler(IAppDbContext context, ILogger<UpdatePropertyDetailsCommandHandler> logger, IUser user) : IRequestHandler<UpdatePropertyDetailsCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<UpdatePropertyDetailsCommandHandler> _logger = logger;
    private readonly IUser _user = user;

    public async Task<Result<Success>> Handle(UpdatePropertyDetailsCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Update property rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Update property rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }
        var result = property.UpdateDetails(
            request.Title,
            request.Description,
            request.Price,
            request.PaymentType,
            request.PropertyType,
            request.Area,
            request.City,
            request.Region,
            request.FullAddress,
            request.Latitude,
            request.Longitude,
            request.LandClassification,
            request.LegalStatus);
        if (result.IsError)
        {
            _logger.LogWarning("Update property {PropertyId} failed: {ErrorCode}", request.PropertyId, result.TopError.Code);
            return result.Errors;
        }
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Property {PropertyId} updated by seller {SellerId}", property.Id, sellerId);
        return Result.Success;
    }
}
