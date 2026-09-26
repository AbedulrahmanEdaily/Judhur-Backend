using Judhur.Domain.Properties.Enums;

namespace Judhur.Application.Features.Properties.Dto;

/// <summary>
/// A listing card as seen by its owner ("My properties"). Unlike <see cref="PropertySummaryDto"/>
/// it carries the moderation/visibility state, which is private to the seller.
/// </summary>
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
}
