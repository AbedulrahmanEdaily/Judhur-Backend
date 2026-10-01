namespace Judhur.Api;

public static class RateLimitPolicies
{
    public const string ResendConfirmation = "resend-confirmation";
    public const string SendResetPasswordCode = "send-reset-password-code";
    public const string ChangePassword = "change-password";
    public const string GoogleLogin = "google-login";
    public const string SetMyPassword = "set-my-password";
}
