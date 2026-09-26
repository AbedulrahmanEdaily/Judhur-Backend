namespace Judhur.Application.Common.Models;

public sealed record UserInfoDto(Guid Id, string FullName, string? PhoneNumber, string? ProfileImageUrl);