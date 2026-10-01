using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Common.Interfaces;

public interface IGoogleTokenValidator
{
    Task<Result<GoogleUser>> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

