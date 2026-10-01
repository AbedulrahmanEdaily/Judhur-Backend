using System.Security.Cryptography;
using System.Text.Json;

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
    Guid? SellerId,
    string SortColumn = "createdAt",
    string SortDirection = "desc"
    ) : ICachedQuery<Result<PaginatedList<PropertySummaryDto>>>
{
    public const int MaxCities = 20;

    private static readonly string[] SortColumns = ["createdat", "price", "city", "landclassification"];

    public string CacheKey
    {
        get
        {
            var sortColumn = SortColumn.ToLowerInvariant();
            var isKnownSort = SortColumns.Contains(sortColumn);
            var normalized = new
            {
                Page,
                PageSize,
                MinPrice,
                MaxPrice,
                SellerId,
                Sort = isKnownSort ? sortColumn : SortColumns[0],
                Descending = !isKnownSort || SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase),
                Cities = Ordered(Cities.Select(c => c.ToLowerInvariant())),
                LandClassifications = Ordered(LandClassifications),
                LegalStatuses = Ordered(LegalStatuses),
                PaymentTypes = Ordered(PaymentTypes),
                PropertyStatuses = Ordered(PropertyStatuses),
                PropertyTypes = Ordered(PropertyTypes)
            };
            var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(normalized));
            return $"properties:search:{Convert.ToHexString(hash)}";
        }
    }

    public string[] Tags => ["properties"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);

    public bool IsCacheable => string.IsNullOrWhiteSpace(SearchTerm);

    private static string[] Ordered<T>(IEnumerable<T> values)
        => [.. values.Select(v => v!.ToString()!).Distinct().Order(StringComparer.Ordinal)];
}
