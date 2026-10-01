using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Events;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.RejectProperty;

public sealed class RejectPropertyCommandHandler(IUser user, IAppDbContext context, ILogger<RejectPropertyCommandHandler> logger, TimeProvider timeProvider, IDeferredDispatcher dispatcher, IPublisher publisher) : IRequestHandler<RejectPropertyCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<RejectPropertyCommandHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<Updated>> Handle(RejectPropertyCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } adminId)
        {
            _logger.LogWarning("Reject property rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == request.PropertyId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Reject property rejected: property {PropertyId} not found", request.PropertyId);
            return PropertyErrors.NotFound;
        }
        var result = property.Reject(adminId, _timeProvider.GetUtcNow(), request.RejectionReason);
        if (result.IsError)
        {
            _logger.LogWarning("Reject property {PropertyId} failed: {Error}", request.PropertyId, result.TopError.Code);
            return result.Errors;
        }
        await _context.SaveChangesAsync(cancellationToken);
        _dispatcher.Defer(ct => _publisher.Publish(new PropertyRejected(property.Id, property.SellerId, property.Title, property.RejectionReason!), ct));
        _logger.LogInformation("Property {PropertyId} rejected by admin {AdminId}", property.Id, adminId);
        return Result.Updated;
    }
}