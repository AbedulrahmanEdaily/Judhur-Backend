using System.Security.Claims;

using Judhur.Application.Common.Interfaces;

namespace Judhur.Api.Services;

/// <summary>
/// Reads the caller's id off the current request. This lives in the API layer
/// rather than Infrastructure because HttpContext is a presentation concern:
/// the Application layer only ever sees <see cref="IUser"/>.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : IUser
{
    public Guid? Id
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
