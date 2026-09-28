using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Queries.GetMyProperties;

public sealed class GetMyPropertiesQueryHandler(IAppDbContext context, IUser user, ILogger<GetMyPropertiesQueryHandler> logger) : IRequestHandler<GetMyPropertiesQuery, Result<List<MyPropertyDto>>>
{
    private readonly IAppDbContext _context = context;
    private readonly IUser _user = user;
    private readonly ILogger<GetMyPropertiesQueryHandler> _logger = logger;

    public async Task<Result<List<MyPropertyDto>>> Handle(GetMyPropertiesQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } sellerId)
        {
            _logger.LogWarning("Get my properties rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var properties = await _context.Properties
            .AsNoTracking()
            .Where(p => p.SellerId == sellerId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new MyPropertyDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                PaymentType = p.PaymentType,
                PropertyType = p.PropertyType,
                PropertyStatus = p.PropertyStatus,
                Area = p.Area,
                City = p.City,
                Region = p.Region,
                ModerationStatus = p.ModerationStatus,
                RejectionReason = p.RejectionReason,
                IsActive = p.IsActive,
                CreatedAtUtc = p.CreatedAtUtc,
                MainImageUrl = p.PropertyImages
                    .Where(i => i.IsMainImage)
                    .Select(i => i.FileUrl)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        _logger.LogInformation("Retrieved {Count} properties for seller {SellerId}", properties.Count, sellerId);
        return properties;
    }
}
