using Judhur.Application.Features.Identity;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Common.Interfaces;

public interface ITokenProvider
{
    Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default);
}