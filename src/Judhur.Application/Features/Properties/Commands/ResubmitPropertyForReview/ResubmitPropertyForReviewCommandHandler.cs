using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.ResubmitPropertyForReview;

public sealed class ResubmitPropertyForReviewCommandHandler(IUser user, IAppDbContext context, ILogger<ResubmitPropertyForReviewCommandHandler> logger) : IRequestHandler<ResubmitPropertyForReviewCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<ResubmitPropertyForReviewCommandHandler> _logger = logger;

    public async Task<Result<Updated>> Handle(ResubmitPropertyForReviewCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Resubmit property rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Resubmit property rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }
        var result = property.ResubmitForReview();
        if (result.IsError)
        {
            _logger.LogWarning("Resubmit property {PropertyId} failed: {ErrorCode}", request.PropertyId, result.TopError.Code);
            return result.Errors;
        }
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Property {PropertyId} resubmitted for review by seller {SellerId}", property.Id, sellerId);
        return Result.Updated;
    }
}
