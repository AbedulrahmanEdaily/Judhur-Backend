namespace Judhur.Application.Features.Identity.Dtos;

public sealed record AppUserDto(Guid UserId, string Email, IList<string> Roles);
