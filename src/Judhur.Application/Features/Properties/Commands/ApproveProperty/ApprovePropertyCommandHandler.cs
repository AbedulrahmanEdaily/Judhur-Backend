using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Events;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.ApproveProperty;

public sealed class ApprovePropertyCommandHandler(IUser user, IAppDbContext context, ILogger<ApprovePropertyCommandHandler> logger, TimeProvider timeProvider, IDeferredDispatcher dispatcher, IPublisher publisher) : IRequestHandler<ApprovePropertyCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<ApprovePropertyCommandHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<Updated>> Handle(ApprovePropertyCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } adminId)
        {
            _logger.LogWarning("Approve property rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var property = await _context.Properties.Include(p => p.PropertyImages).FirstOrDefaultAsync(p => p.Id == request.PropertyId, cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Approve property rejected: property {PropertyId} not found", request.PropertyId);
            return PropertyErrors.NotFound;
        }
        var result = property.Approve(adminId, _timeProvider.GetUtcNow());
        if (result.IsError)
        {
            _logger.LogWarning("Approve property {PropertyId} failed: {Error}", request.PropertyId, result.TopError.Code);
            return result.Errors;
        }
        await _context.SaveChangesAsync(cancellationToken);
        _dispatcher.Defer(ct => _publisher.Publish(new PropertyApproved(property.Id, property.SellerId, property.Title), ct));
        _logger.LogInformation("Property {PropertyId} approved by admin {AdminId}", property.Id, adminId);
        return Result.Updated;
    }
}