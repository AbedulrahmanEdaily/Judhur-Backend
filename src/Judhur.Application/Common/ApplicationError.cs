using Judhur.Domain.Common.Results;

namespace Judhur.Application.Common;

public static class ApplicationError
{
    public static readonly Error ExpiredAccessTokenInvalid = Error.Unauthorized(
        code: "Auth.ExpiredAccessToken.Invalid",
        description: "Expired access token is not valid.");
    public static readonly Error UserIdClaimInvalid = Error.Unauthorized(
        code: "Auth.UserIdClaim.Invalid",
        description: "Invalid userId claim.");
    public static readonly Error UserNotFound = Error.NotFound(
        code: "Auth.User.NotFound",
        description: "User not found.");
    public static readonly Error TokenGenerationFailed = Error.Failure(
        code: "Auth.TokenGeneration.Failed",
        description: "Failed to generate new JWT token.");
    public static readonly Error Unauthenticated = Error.Unauthorized(
        code: "Auth.NotAuthenticated",
        description: "يجب تسجيل الدخول لتنفيذ هذا الإجراء.");
}
