using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.DeleteProperty;

public sealed class DeletePropertyCommandHandler(IUser user, IAppDbContext context, TimeProvider timeProvider, ILogger<DeletePropertyCommandHandler> logger) : IRequestHandler<DeletePropertyCommand, Result<Deleted>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<DeletePropertyCommandHandler> _logger = logger;

    public async Task<Result<Deleted>> Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Delete property rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Delete property rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }

        var result = property.Delete(_timeProvider.GetUtcNow());
        if (result.IsError)
        {
            _logger.LogWarning("Delete property {PropertyId} failed: {ErrorCode}", request.PropertyId, result.TopError.Code);
            return result.Errors;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Property {PropertyId} deleted by seller {SellerId}", property.Id, sellerId);
        return Result.Deleted;
    }
}
