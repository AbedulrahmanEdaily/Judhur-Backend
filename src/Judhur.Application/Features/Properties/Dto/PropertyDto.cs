using Judhur.Domain.Properties.Enums;

namespace Judhur.Application.Features.Properties.Dto;

public class PropertyDto
{
    public Guid Id {get;set;}
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public PaymentType PaymentType { get; set; }
    public PropertyType PropertyType { get; set; }
    public PropertyStatus PropertyStatus { get; set; }
    public decimal Area { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public LandClassification LandClassification { get; set; }
    public LegalStatus LegalStatus { get; set; }
    public string OwnershipDocumentUrl { get; set; } = string.Empty;

}