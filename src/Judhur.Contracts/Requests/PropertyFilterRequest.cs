using Judhur.Contracts.Common;

namespace Judhur.Contracts.Requests;

public record PropertyFilterRequest
{
    public string? SearchTerm { get; set; }
    public string SortColumn { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
    public decimal? MaxPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public List<string>? City { get; set; }
    public List<LegalStatus>? LegalStatus { get; set; }
    public List<LandClassification>? LandClassification { get; set; }
    public List<PaymentType>? PaymentType { get; set; }
    public List<PropertyStatus>? PropertyStatus { get; set; }
    public List<PropertyType>? PropertyType { get; set; }
}
