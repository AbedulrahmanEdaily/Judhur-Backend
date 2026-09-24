using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;

namespace Judhur.Application.Features.Properties.Queries.GetProperties;

public sealed record GetPropertiesQuery(
    int Page,
    int PageSize,
    string? SearchTerm,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? City,
    LandClassification? LandClassification,
    LegalStatus? LegalStatus,
    PaymentType? PaymentType,
    PropertyStatus? PropertyStatus,
    PropertyType? PropertyType,
    string SortColumn = "createdAt",
    string SortDirection = "desc"
    ) : ICachedQuery<Result<PaginatedList<PropertySummaryDto>>>
{
    public string CacheKey =>
    $"property:p={Page}:ps={PageSize}" +
    $":q={SearchTerm ?? "-"}" +
    $":sort={SortColumn}:{SortDirection}" +
    $":price:min={MinPrice?.ToString() ?? "-"}:max={MaxPrice?.ToString() ?? "-"}" +
    $":city={City ?? "-"}" +
    $":land={LandClassification?.ToString() ?? "-"}" +
    $":legalStatus={LegalStatus?.ToString() ?? "-"}" +
    $":payment={PaymentType?.ToString() ?? "-"}" +
    $":status={PropertyStatus?.ToString() ?? "-"}" +
    $":propertyType={PropertyType?.ToString() ?? "-"}";

    public string[] Tags => ["properties"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}