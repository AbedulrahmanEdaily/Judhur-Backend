using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.MarkPropertyAsSold;

public sealed class MarkPropertyAsSoldCommandHandler(IUser user, IAppDbContext context, ILogger<MarkPropertyAsSoldCommandHandler> logger) : IRequestHandler<MarkPropertyAsSoldCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<MarkPropertyAsSoldCommandHandler> _logger = logger;

    public async Task<Result<Updated>> Handle(MarkPropertyAsSoldCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Mark property as sold rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Mark property as sold rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }

        var result = property.MarkAsSold();
        if (result.IsError)
        {
            _logger.LogWarning("Mark property as sold {PropertyId} failed: {ErrorCode}", request.PropertyId, result.TopError.Code);
            return result.Errors;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Property {PropertyId} marked as sold by seller {SellerId}", property.Id, sellerId);
        return Result.Updated;
    }
}
