using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;

namespace Judhur.Api.IntegrationTests.Infrastructure;

public sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public static string TokenFor(string subject, string email, string fullName, bool emailVerified = true)
        => string.Join('|', "google", subject, email, fullName, emailVerified ? "1" : "0");

    public Task<Result<GoogleUser>> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        var parts = idToken.Split('|');
        if (parts.Length != 5 || parts[0] != "google")
        {
            return Task.FromResult<Result<GoogleUser>>(ApplicationError.InvalidGoogleToken);
        }
        return Task.FromResult<Result<GoogleUser>>(new GoogleUser(parts[1], parts[2], parts[4] == "1", parts[3], null));
    }
}
