using System.Security.Claims;

using Judhur.Application.Common.Interfaces;

namespace Judhur.Api.Services;

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
