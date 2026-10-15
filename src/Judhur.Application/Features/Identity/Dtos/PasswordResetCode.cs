namespace Judhur.Application.Features.Identity.Dtos;

public sealed record PasswordResetCode(Guid UserId, string Code);
