using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Properties.Queries.GetPropertyById;

public sealed record GetPropertyByIdQuery(Guid PropertyId) : ICachedQuery<Result<PropertyDto>>
{
    public string CacheKey => $"properties:{PropertyId}";

    public string[] Tags => ["properties"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}