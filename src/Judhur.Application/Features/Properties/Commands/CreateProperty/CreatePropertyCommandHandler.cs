using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Application.Features.Properties.Mapper;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Commands.CreateProperty;

public sealed class CreatePropertyCommandHandler(ILogger<CreatePropertyCommandHandler> logger, IAppDbContext context, IUser user) : IRequestHandler<CreatePropertyCommand, Result<PropertyDto>>
{
    private readonly ILogger<CreatePropertyCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly IUser _user = user;

    public async Task<Result<PropertyDto>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            return ApplicationError.Unauthenticated;
        }
        var propertyResult = Property.Create(
            Guid.CreateVersion7(),
            request.Title,
            request.Description,
            request.Price,
            request.PaymentType,
            request.PropertyType,
            request.PropertyStatus,
            request.Area,
            request.City,
            request.Region,
            request.FullAddress,
            request.Latitude,
            request.Longitude,
            request.LandClassification,
            request.LegalStatus,
            request.OwnershipDocumentUrl,
            sellerId);
        if (propertyResult.IsError)
        {
            _logger.LogError("Failed to create Property {Error}", propertyResult.TopError.Description);
            return propertyResult.Errors;
        }
        var property = propertyResult.Value;
        _context.Properties.Add(property);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Property with Id '{Id}' created successfully", property.Id);
        return property.ToDto();
    }
}