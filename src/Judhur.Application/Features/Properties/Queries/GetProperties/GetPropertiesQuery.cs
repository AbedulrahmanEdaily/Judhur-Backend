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
    IReadOnlyList<string> Cities,
    IReadOnlyList<LandClassification> LandClassifications,
    IReadOnlyList<LegalStatus> LegalStatuses,
    IReadOnlyList<PaymentType> PaymentTypes,
    IReadOnlyList<PropertyStatus> PropertyStatuses,
    IReadOnlyList<PropertyType> PropertyTypes,
    string SortColumn = "createdAt",
    string SortDirection = "desc"
    ) : ICachedQuery<Result<PaginatedList<PropertySummaryDto>>>
{
    public const int MaxCities = 20;

    public string CacheKey =>
    $"property:p={Page}:ps={PageSize}" +
    $":q={SearchTerm ?? "-"}" +
    $":sort={SortColumn}:{SortDirection}" +
    $":price:min={MinPrice?.ToString() ?? "-"}:max={MaxPrice?.ToString() ?? "-"}" +
    $":city={KeyOf(Cities.Select(c => c.ToLowerInvariant()))}" +
    $":land={KeyOf(LandClassifications)}" +
    $":legalStatus={KeyOf(LegalStatuses)}" +
    $":payment={KeyOf(PaymentTypes)}" +
    $":status={KeyOf(PropertyStatuses)}" +
    $":propertyType={KeyOf(PropertyTypes)}";

    public string[] Tags => ["properties"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);

    private static string KeyOf<T>(IEnumerable<T> values)
    {
        var ordered = values.Select(v => v!.ToString()).Order(StringComparer.Ordinal).ToList();
        return ordered.Count == 0 ? "-" : string.Join(",", ordered);
    }
}
