using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Sellers.Dto;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Sellers.Queries.GetSellerProfile;

public sealed record GetSellerProfileQuery(Guid SellerId) : ICachedQuery<Result<SellerProfileDto>>
{
    public string CacheKey => $"sellers:{SellerId}";

    public string[] Tags => ["properties"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
