using Judhur.Domain.Properties.Enums;

namespace Judhur.Application.Features.Properties.Dto;

/// <summary>
/// The shape returned by the property listing/search endpoint — only what a results
/// card needs to display. Deliberately excludes the fields <see cref="PropertyDto"/>
/// carries for the single-property page (description, exact address, coordinates,
/// legal fields, ownership document, seller info).
/// </summary>
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
}
