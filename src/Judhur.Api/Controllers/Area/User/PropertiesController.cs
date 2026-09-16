using Asp.Versioning;

using Judhur.Domain.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judhur.Api.Controllers.Area.User;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = Roles.User)]
public class PropertiesController : ApiController
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
}
