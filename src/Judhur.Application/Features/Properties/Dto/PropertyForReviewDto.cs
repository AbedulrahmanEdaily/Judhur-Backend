using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Properties.Enums;

namespace Judhur.Application.Features.Properties.Dto;

public sealed class PropertyForReviewDto
{
    public Guid Id { get; set; }
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
    public ModerationStatus ModerationStatus { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<PropertyImageDto> Images { get; set; } = [];
    public UserInfoDto? Seller { get; set; }
    public string? OwnershipDocumentUrl { get; set; }
    public DateTimeOffset? OwnershipDocumentExpiresAtUtc { get; set; }
}