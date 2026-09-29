using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Queries.GetMyPropertyById;

public sealed class GetMyPropertyByIdQueryHandler(IUser user, IAppDbContext context, ILogger<GetMyPropertyByIdQueryHandler> logger) : IRequestHandler<GetMyPropertyByIdQuery, Result<MyPropertyDetailsDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetMyPropertyByIdQueryHandler> _logger = logger;

    public async Task<Result<MyPropertyDetailsDto>> Handle(GetMyPropertyByIdQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Get my property rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var property = await _context.Properties
            .AsNoTracking()
            .Where(p => p.Id == request.PropertyId && p.SellerId == sellerId)
            .Select(p => new MyPropertyDetailsDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                PaymentType = p.PaymentType,
                PropertyType = p.PropertyType,
                PropertyStatus = p.PropertyStatus,
                Area = p.Area,
                City = p.City,
                Region = p.Region,
                FullAddress = p.FullAddress,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                LandClassification = p.LandClassification,
                LegalStatus = p.LegalStatus,
                HasOwnershipDocument = p.OwnershipDocumentPublicId != null,
                ModerationStatus = p.ModerationStatus,
                RejectionReason = p.RejectionReason,
                ReviewedAtUtc = p.ReviewedAtUtc,
                IsActive = p.IsActive,
                CreatedAtUtc = p.CreatedAtUtc,
                Images = p.PropertyImages
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new PropertyImageDto(i.Id, i.FileUrl, i.DisplayOrder, i.IsMainImage))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Get my property rejected: property {PropertyId} not found for seller {SellerId}", request.PropertyId, sellerId);
            return PropertyErrors.NotFound;
        }

        _logger.LogInformation("Property {PropertyId} retrieved by its seller {SellerId}", property.Id, sellerId);
        return property;
    }
}
