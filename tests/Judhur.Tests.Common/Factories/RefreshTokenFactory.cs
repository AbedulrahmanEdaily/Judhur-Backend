using Judhur.Domain.Common.Results;
using Judhur.Domain.Identity;

namespace Judhur.Tests.Common.Factories;

public static class RefreshTokenFactory
{
    public const string DefaultTokenHash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

    public static readonly Guid DefaultUserId = ConversationFactory.DefaultBuyerId;
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(7);

    public static Result<RefreshToken> CreateResult(
        string tokenHash = DefaultTokenHash,
        Guid? userId = null,
        DateTimeOffset? expiresOnUtc = null,
        DateTimeOffset? nowUtc = null,
        Guid? id = null)
    {
        var now = nowUtc ?? TestClock.UtcNow;

        return RefreshToken.Create(
            id ?? Guid.NewGuid(),
            tokenHash,
            userId ?? DefaultUserId,
            expiresOnUtc ?? now.Add(DefaultLifetime),
            now);
    }

    public static RefreshToken Create(
        string tokenHash = DefaultTokenHash,
        Guid? userId = null,
        DateTimeOffset? expiresOnUtc = null,
        DateTimeOffset? nowUtc = null,
        Guid? id = null)
        => CreateResult(tokenHash, userId, expiresOnUtc, nowUtc, id).GetValueOrThrow();
}
