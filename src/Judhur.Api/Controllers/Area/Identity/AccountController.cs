using Asp.Versioning;

using Judhur.Application.Features.Identity;
using Judhur.Application.Features.Identity.Commands.ChangePassword;
using Judhur.Application.Features.Identity.Commands.ConfirmEmail;
using Judhur.Application.Features.Identity.Commands.GoogleLogin;
using Judhur.Application.Features.Identity.Commands.Login;
using Judhur.Application.Features.Identity.Commands.Logout;
using Judhur.Application.Features.Identity.Commands.RefreshToken;
using Judhur.Application.Features.Identity.Commands.RemoveProfileImage;
using Judhur.Application.Features.Identity.Commands.Register;
using Judhur.Application.Features.Identity.Commands.ResendConfirmation;
using Judhur.Application.Features.Identity.Commands.SendResetPasswordCode;
using Judhur.Application.Features.Identity.Commands.UpdateMyProfile;
using Judhur.Application.Features.Identity.Commands.UploadProfileImage;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Application.Features.Identity.Queries.GetMyProfile;

using MediatR;

using Microsoft.AspNetCore.Authorization;
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

    [HttpPost("google")]
    [EnableRateLimiting(RateLimitPolicies.GoogleLogin)]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [EndpointSummary("Signs in with a Google account.")]
    [EndpointDescription("Exchanges a Google ID token for the same token pair as the normal login. An existing account with the same email is linked automatically. A new account needs phoneNumber and city: without them the endpoint returns 400 with the error key Identity.GoogleRegistrationIncomplete, and the client sends the same ID token again with both fields. Returns 401 for an invalid or expired Google token, 403 when the Google email is not verified or the account is locked.")]
    [EndpointName("GoogleLogin")]
    public async Task<IActionResult> GoogleLoginAsync([FromBody] GoogleLoginCommand request, CancellationToken ct)
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

    [HttpPost("send-reset-password-code")]
    [EnableRateLimiting(RateLimitPolicies.SendResetPasswordCode)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [EndpointSummary("Sends a password reset code to the user's email.")]
    [EndpointDescription("Always answers 204, whether or not the address belongs to an account.")]
    [EndpointName("SendResetPasswordCode")]
    public async Task<IActionResult> SendResetPasswordCodeAsync([FromBody] SendResetPasswordCodeCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("change-password")]
    [EnableRateLimiting(RateLimitPolicies.ChangePassword)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [EndpointSummary("Verifies the reset code and sets a new password.")]
    [EndpointName("ChangePassword")]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(_ => NoContent(), Problem);
    }
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Refreshes access token using a valid refresh token.")]
    [EndpointDescription("Exchanges an expired access token and a valid refresh token for a new token pair.")]
    [EndpointName("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Logs the user out.")]
    [EndpointDescription("Revokes the given refresh token so it can no longer be used to obtain new access tokens.")]
    [EndpointName("Logout")]
    public async Task<IActionResult> LogoutAsync([FromBody] LogoutCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MyProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Gets the profile of the authenticated user.")]
    [EndpointDescription("Returns the full profile of the signed-in user: full name, email, phone number, city, bio, profile image, roles, whether the account has a password (false for accounts created with Google that never set one) and the registration date. Returns 401 if the request has no valid access token and 404 if the account no longer exists.")]
    [EndpointName("GetMyProfile")]
    public async Task<IActionResult> GetMyProfileAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new GetMyProfileQuery(), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(MyProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Updates the profile of the authenticated user.")]
    [EndpointDescription("Replaces the full name, phone number, city and bio of the signed-in user and returns the updated profile. All four fields are sent on every request; an empty or whitespace bio clears it. Email cannot be changed here. Returns 400 on validation errors, 401 without a valid access token, 404 if the account no longer exists and 409 if the account was modified by another request at the same time.")]
    [EndpointName("UpdateMyProfile")]
    public async Task<IActionResult> UpdateMyProfileAsync([FromBody] UpdateMyProfileCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPut("me/photo")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProfileImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Uploads or replaces the profile image of the authenticated user.")]
    [EndpointDescription("Uploads a JPG, PNG or WEBP image (max 5 MB) sent as the 'file' form field and sets it as the profile image of the signed-in user, replacing any previous one. The previous image is deleted from storage. Returns the new image URL. Returns 400 on an invalid file, 401 without a valid access token and 404 if the account no longer exists.")]
    [EndpointName("UploadProfileImage")]
    public async Task<IActionResult> UploadProfileImageAsync(IFormFile file, CancellationToken ct)
    {
        await using var content = file.OpenReadStream();
        var result = await _sender.Send(new UploadProfileImageCommand(
            content,
            file.FileName,
            file.ContentType,
            file.Length), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("me/photo")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Removes the profile image of the authenticated user.")]
    [EndpointDescription("Clears the profile image of the signed-in user and deletes it from storage. Calling it when the user has no image succeeds without changes. Returns 401 without a valid access token and 404 if the account no longer exists.")]
    [EndpointName("RemoveProfileImage")]
    public async Task<IActionResult> RemoveProfileImageAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new RemoveProfileImageCommand(), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
