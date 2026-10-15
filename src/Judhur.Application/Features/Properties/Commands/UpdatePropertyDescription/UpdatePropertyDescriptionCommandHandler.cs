using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.UpdatePropertyDescription;

public sealed class UpdatePropertyDescriptionCommandHandler(IAppDbContext context, ILogger<UpdatePropertyDescriptionCommandHandler> logger, IUser user, ReviewQueue reviewQueue) : IRequestHandler<UpdatePropertyDescriptionCommand, Result<Updated>>
{
    private readonly IAppDbContext _context = context;
    private readonly ReviewQueue _reviewQueue = reviewQueue;
    private readonly ILogger<UpdatePropertyDescriptionCommandHandler> _logger = logger;
    private readonly IUser _user = user;

    public async Task<Result<Updated>> Handle(UpdatePropertyDescriptionCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Update property description rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Update property description rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }
        var wasQueued = await _reviewQueue.ContainsAsync(property.Id, cancellationToken);
        var result = property.UpdateDescription(request.Description);
        if (result.IsError)
        {
            _logger.LogWarning("Update description for property {PropertyId} failed: {ErrorCode}", request.PropertyId, result.TopError.Code);
            return result.Errors;
        }
        await _context.SaveChangesAsync(cancellationToken);
        await _reviewQueue.PublishIfEnteredAsync(property.Id, property.Title, wasQueued, CancellationToken.None);
        _logger.LogInformation("Property {PropertyId} description updated by seller {SellerId}", property.Id, sellerId);
        return Result.Updated;
    }
}