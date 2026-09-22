using Asp.Versioning;

using Judhur.Application.Features.Identity;
using Judhur.Application.Features.Identity.Commands.ConfirmEmail;
using Judhur.Application.Features.Identity.Commands.ForgetPassword;
using Judhur.Application.Features.Identity.Commands.Login;
using Judhur.Application.Features.Identity.Commands.Register;
using Judhur.Application.Features.Identity.Commands.ResendConfirmation;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Confirms a user's email address.")]
    [EndpointName("ConfirmEmail")]
    public async Task<IActionResult> ConfirmEmailAsync([FromBody] ConfirmEmailCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("resend-confirmation")]
    [EnableRateLimiting(RateLimitPolicies.ResendConfirmation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [EndpointSummary("Sends the confirmation email again.")]
    [EndpointDescription("Always answers 204, whether or not the address belongs to an account.")]
    [EndpointName("ResendConfirmation")]
    public async Task<IActionResult> ResendConfirmationAsync([FromBody] ResendConfirmationCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(_ => NoContent(), Problem);
    }
    [HttpPost("forget-password")]
    [EnableRateLimiting(RateLimitPolicies.ForgetPassword)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [EndpointSummary("Sends reset password code in the email")]
    [EndpointDescription("Always answers 204, whether or not the address belongs to an account.")]
    [EndpointName("ForgetPassword")]
    public async Task<IActionResult> ForgetPasswordAsync([FromBody] ForgetPasswordCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(_ => NoContent(), Problem);
    }
}