using Judhur.Domain.Properties.Enums;

namespace Judhur.Application.Features.Properties.Dto;

public sealed class MyPropertyDto
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
    public ModerationStatus ModerationStatus { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? MainImageUrl { get; set; }
}
