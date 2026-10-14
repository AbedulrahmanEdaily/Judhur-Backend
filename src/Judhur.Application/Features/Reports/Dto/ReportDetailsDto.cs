using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Properties.Enums;
using Judhur.Domain.Reports.Enums;

namespace Judhur.Application.Features.Reports.Dto;

public sealed class ReportDetailsDto
{
    public Guid Id { get; init; }
    public ReportReason Reason { get; init; }
    public string? Details { get; init; }
    public ReportStatus Status { get; init; }
    public string? AdminNote { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? ReviewedAtUtc { get; init; }
    public Guid ReporterId { get; init; }
    public UserInfoDto? Reporter { get; set; }
    public ReportedPropertyDto Property { get; init; } = null!;
    public int OtherPendingReportsOnProperty { get; init; }
}

public sealed record ReportedPropertyDto(
    Guid Id,
    string Title,
    string City,
    ModerationStatus ModerationStatus,
    bool IsActive,
    bool IsDeleted,
    string? MainImageUrl,
    Guid SellerId);
