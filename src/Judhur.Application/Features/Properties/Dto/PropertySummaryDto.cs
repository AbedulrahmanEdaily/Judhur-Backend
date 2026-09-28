using Judhur.Domain.Properties.Enums;

namespace Judhur.Application.Features.Properties.Dto;

public sealed class PropertySummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public PaymentType PaymentType { get; set; }
    public PropertyType PropertyType { get; set; }
    public PropertyStatus PropertyStatus { get; set; }
    public decimal Area { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? MainImageUrl { get; set; }
}
