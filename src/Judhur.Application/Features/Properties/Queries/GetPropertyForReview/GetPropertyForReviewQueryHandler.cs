using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Queries.GetPropertyForReview;

public sealed class GetPropertyForReviewQueryHandler(
    IAppDbContext context,
    IIdentityService identityService,
    IFileStorage fileStorage,
    TimeProvider timeProvider,
    ILogger<GetPropertyForReviewQueryHandler> logger) : IRequestHandler<GetPropertyForReviewQuery, Result<PropertyForReviewDto>>
{
    private static readonly TimeSpan DocumentLinkLifetime = TimeSpan.FromMinutes(10);

    private readonly IAppDbContext _context = context;
    private readonly IIdentityService _identityService = identityService;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<GetPropertyForReviewQueryHandler> _logger = logger;

    public async Task<Result<PropertyForReviewDto>> Handle(GetPropertyForReviewQuery request, CancellationToken cancellationToken)
    {
        var result = await _context.Properties
            .AsNoTracking()
            .Where(p => p.Id == request.PropertyId)
            .Select(p => new
            {
                Property = new PropertyForReviewDto
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
                    ModerationStatus = p.ModerationStatus,
                    RejectionReason = p.RejectionReason,
                    ReviewedAtUtc = p.ReviewedAtUtc,
                    IsActive = p.IsActive,
                    CreatedAtUtc = p.CreatedAtUtc,
                    Images = p.PropertyImages
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new PropertyImageDto(i.Id, i.FileUrl, i.DisplayOrder, i.IsMainImage))
                        .ToList()
                },
                p.SellerId,
                p.OwnershipDocumentPublicId
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (result is null)
        {
            _logger.LogWarning("Get property for review rejected: property {PropertyId} not found", request.PropertyId);
            return PropertyErrors.NotFound;
        }
        var property = result.Property;
        var sellerResult = await _identityService.GetUserInfoAsync(result.SellerId.ToString(), cancellationToken);
        if (sellerResult.IsError)
        {
            _logger.LogWarning("Failed to load seller {SellerId} for property {PropertyId}: {ErrorCode}", result.SellerId, property.Id, sellerResult.TopError.Code);
        }
        else
        {
            property.Seller = sellerResult.Value;
        }
        if (result.OwnershipDocumentPublicId is not null)
        {
            var expiresAtUtc = _timeProvider.GetUtcNow().Add(DocumentLinkLifetime);
            property.OwnershipDocumentUrl = _fileStorage.GetPrivateDocumentUrl(result.OwnershipDocumentPublicId, expiresAtUtc);
            property.OwnershipDocumentExpiresAtUtc = expiresAtUtc;
        }
        _logger.LogInformation("Property {PropertyId} retrieved for review", property.Id);
        return property;
    }
}