using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Application.Features.Properties.Mapper;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Queries.GetPropertyById;

public sealed class GetPropertyByIdQueryHandler(ILogger<GetPropertyByIdQueryHandler> logger, IAppDbContext context, IIdentityService identityService) : IRequestHandler<GetPropertyByIdQuery, Result<PropertyDto>>
{
    private readonly ILogger<GetPropertyByIdQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly IIdentityService _identityService = identityService;


    public async Task<Result<PropertyDto>> Handle(GetPropertyByIdQuery request, CancellationToken cancellationToken)
    {
        var property = await _context.Properties
        .AsNoTracking()
        .FirstOrDefaultAsync(
            p => p.Id == request.PropertyId
                && p.ModerationStatus == ModerationStatus.Approved
                && p.IsActive,
            cancellationToken);
        if (property is null)
        {
            _logger.LogWarning("Property with id {PropertyId} was not found", request.PropertyId);
            return PropertyErrors.NotFound;
        }
        var userInfo = await _identityService.GetUserInfoAsync(property.SellerId.ToString(), cancellationToken);
        if (userInfo.IsError)
        {
            return userInfo.Errors;
        }
        return property.ToDto(userInfo.Value);
    }
}