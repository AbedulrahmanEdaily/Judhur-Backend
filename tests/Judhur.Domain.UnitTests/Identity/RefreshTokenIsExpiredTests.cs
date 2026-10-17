using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Identity;

public sealed class RefreshTokenIsExpiredTests
{
    private static readonly DateTimeOffset ExpiresOn = TestClock.UtcNow.AddDays(7);

    [Fact]
    public void IsExpired_BeforeExpiry_ReturnsFalse()
    {
        var token = RefreshTokenFactory.Create(expiresOnUtc: ExpiresOn);

        Assert.False(token.IsExpired(ExpiresOn.AddTicks(-1)));
    }

    [Fact]
    public void IsExpired_AtCreationTime_ReturnsFalse()
    {
        var token = RefreshTokenFactory.Create(expiresOnUtc: ExpiresOn);

        Assert.False(token.IsExpired(TestClock.UtcNow));
    }

    [Fact]
    public void IsExpired_ExactlyAtExpiry_ReturnsTrue()
    {
        var token = RefreshTokenFactory.Create(expiresOnUtc: ExpiresOn);

        Assert.True(token.IsExpired(ExpiresOn));
    }

    [Fact]
    public void IsExpired_AfterExpiry_ReturnsTrue()
    {
        var token = RefreshTokenFactory.Create(expiresOnUtc: ExpiresOn);

        Assert.True(token.IsExpired(ExpiresOn.AddDays(1)));
    }

    [Fact]
    public void IsExpired_SameInstantInOtherOffset_ReturnsTrue()
    {
        var token = RefreshTokenFactory.Create(expiresOnUtc: ExpiresOn);

        Assert.True(token.IsExpired(ExpiresOn.ToOffset(TimeSpan.FromHours(3))));
    }
}
