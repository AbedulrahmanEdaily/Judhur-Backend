using Judhur.Contracts.Common;

namespace Judhur.Contracts.Requests;

public record PropertyFilterRequest
{
    public string? SearchTerm { get; set; }
    public string SortColumn { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
    public string? City { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public LegalStatus? LegalStatus { get; set; }
    public LandClassification? LandClassification { get; set; }
    public PaymentType? PaymentType { get; set; }
    public PropertyStatus? PropertyStatus { get; set; }
    public PropertyType? PropertyType { get; set; }
}