using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Identity;

public sealed class RefreshToken : AuditableEntity
{
    public string Token { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTimeOffset ExpiresOnUtc { get; private set; }
    private RefreshToken()
    { }

    private RefreshToken(Guid id, string token, Guid userId, DateTimeOffset expiresOnUtc)
        : base(id)
    {
        Token = token;
        UserId = userId;
        ExpiresOnUtc = expiresOnUtc;
    }

    public bool IsExpired(DateTimeOffset nowUtc) => ExpiresOnUtc <= nowUtc;

    public static Result<RefreshToken> Create(Guid id, string token, Guid userId, DateTimeOffset expiresOnUtc, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return RefreshTokenErrors.TokenRequired;
        }
        if (userId == Guid.Empty)
        {
            return RefreshTokenErrors.UserIdRequired;
        }
        if (expiresOnUtc <= nowUtc)
        {
            return RefreshTokenErrors.ExpiryInvalid;
        }
        return new RefreshToken(id, token, userId, expiresOnUtc);
    }
}
