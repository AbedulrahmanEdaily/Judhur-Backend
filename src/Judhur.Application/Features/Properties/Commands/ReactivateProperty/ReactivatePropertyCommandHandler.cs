using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.ReactivateProperty;

public sealed class ReactivatePropertyCommandHandler(IUser user, IAppDbContext context, ILogger<ReactivatePropertyCommandHandler> logger) : IRequestHandler<ReactivatePropertyCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<ReactivatePropertyCommandHandler> _logger = logger;

    public async Task<Result<Updated>> Handle(ReactivatePropertyCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Reactivate property rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Reactivate property rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }

        var result = property.Reactivate();
        if (result.IsError)
        {
            _logger.LogWarning("Reactivate property {PropertyId} failed: {ErrorCode}", request.PropertyId, result.TopError.Code);
            return result.Errors;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Property {PropertyId} reactivated by seller {SellerId}", property.Id, sellerId);
        return Result.Updated;
    }
}
