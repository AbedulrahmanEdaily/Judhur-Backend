using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Identity;

public sealed class RefreshToken : AuditableEntity
{
    public const int MaxTokenHashLength = 128;

    public string TokenHash { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTimeOffset ExpiresOnUtc { get; private set; }
    private RefreshToken()
    { }

    private RefreshToken(Guid id, string tokenHash, Guid userId, DateTimeOffset expiresOnUtc)
        : base(id)
    {
        TokenHash = tokenHash;
        UserId = userId;
        ExpiresOnUtc = expiresOnUtc;
    }

    public bool IsExpired(DateTimeOffset nowUtc) => ExpiresOnUtc <= nowUtc;

    public static Result<RefreshToken> Create(Guid id, string tokenHash, Guid userId, DateTimeOffset expiresOnUtc, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return RefreshTokenErrors.TokenRequired;
        }

        if (tokenHash.Length > MaxTokenHashLength)
        {
            return RefreshTokenErrors.TokenTooLong;
        }
        if (userId == Guid.Empty)
        {
            return RefreshTokenErrors.UserIdRequired;
        }
        if (expiresOnUtc <= nowUtc)
        {
            return RefreshTokenErrors.ExpiryInvalid;
        }
        return new RefreshToken(id, tokenHash, userId, expiresOnUtc);
    }
}
