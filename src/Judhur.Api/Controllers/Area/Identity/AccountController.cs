using Asp.Versioning;

using Judhur.Application.Features.Identity;
using Judhur.Application.Features.Identity.Commands.Login;
using Judhur.Application.Features.Identity.Commands.Register;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Judhur.Api.Controllers.Area.Identity;

[Area("Identity")]
[Route("api/Identity/Account")]
[ApiVersionNeutral]
public sealed class AccountController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Create a new user")]
    [EndpointDescription("Create a new user")]
    [EndpointName("RegisterUser")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(_ => Created(), Problem);
    }
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Generates an access and refresh token for a valid user.")]
    [EndpointDescription("Authenticates a user using provided credentials and returns a JWT token pair.")]
    [EndpointName("LoginUser")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(response => Ok(response), Problem);
    }
}