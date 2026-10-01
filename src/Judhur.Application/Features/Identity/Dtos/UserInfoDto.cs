namespace Judhur.Application.Features.Identity.Dtos;

public sealed record UserInfoDto(Guid Id, string FullName, string? PhoneNumber, string? ProfileImageUrl);
