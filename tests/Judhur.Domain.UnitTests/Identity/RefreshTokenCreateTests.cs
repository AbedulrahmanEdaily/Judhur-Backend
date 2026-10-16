using Judhur.Domain.Identity;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Identity;

public sealed class RefreshTokenCreateTests
{
    [Fact]
    public void Create_ValidToken_ReturnsSuccess()
    {
        var result = RefreshTokenFactory.CreateResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_ValidToken_CopiesData()
    {
        var userId = Guid.NewGuid();
        var expiresOn = TestClock.UtcNow.AddDays(30);

        var token = RefreshTokenFactory.Create(userId: userId, expiresOnUtc: expiresOn);

        Assert.Equal(RefreshTokenFactory.DefaultTokenHash, token.TokenHash);
        Assert.Equal(userId, token.UserId);
        Assert.Equal(expiresOn, token.ExpiresOnUtc);
    }

    [Fact]
    public void Create_GivenId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var token = RefreshTokenFactory.Create(id: id);

        Assert.Equal(id, token.Id);
    }

    [Fact]
    public void Create_EmptyId_GeneratesNewId()
    {
        var token = RefreshTokenFactory.Create(id: Guid.Empty);

        Assert.NotEqual(Guid.Empty, token.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_BlankTokenHash_ReturnsTokenRequired(string tokenHash)
    {
        var result = RefreshTokenFactory.CreateResult(tokenHash);

        Assert.Equal(RefreshTokenErrors.TokenRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_NullTokenHash_ReturnsTokenRequired()
    {
        var result = RefreshTokenFactory.CreateResult(null!);

        Assert.Equal(RefreshTokenErrors.TokenRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_TokenHashAtLimit_ReturnsSuccess()
    {
        var result = RefreshTokenFactory.CreateResult(new string('a', RefreshToken.MaxTokenHashLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_TokenHashOverLimit_ReturnsTokenTooLong()
    {
        var result = RefreshTokenFactory.CreateResult(new string('a', RefreshToken.MaxTokenHashLength + 1));

        Assert.Equal(RefreshTokenErrors.TokenTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptyUser_ReturnsUserIdRequired()
    {
        var result = RefreshTokenFactory.CreateResult(userId: Guid.Empty);

        Assert.Equal(RefreshTokenErrors.UserIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ExpiryEqualToNow_ReturnsExpiryInvalid()
    {
        var result = RefreshTokenFactory.CreateResult(expiresOnUtc: TestClock.UtcNow, nowUtc: TestClock.UtcNow);

        Assert.Equal(RefreshTokenErrors.ExpiryInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ExpiryInThePast_ReturnsExpiryInvalid()
    {
        var result = RefreshTokenFactory.CreateResult(expiresOnUtc: TestClock.UtcNow.AddSeconds(-1), nowUtc: TestClock.UtcNow);

        Assert.Equal(RefreshTokenErrors.ExpiryInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ExpiryOneTickAfterNow_ReturnsSuccess()
    {
        var result = RefreshTokenFactory.CreateResult(expiresOnUtc: TestClock.UtcNow.AddTicks(1), nowUtc: TestClock.UtcNow);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_BlankTokenAndEmptyUserAndPastExpiry_ReturnsTokenRequiredFirst()
    {
        var result = RefreshTokenFactory.CreateResult("", Guid.Empty, TestClock.UtcNow.AddDays(-1), TestClock.UtcNow);

        Assert.Equal(RefreshTokenErrors.TokenRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptyUserAndPastExpiry_ReturnsUserIdRequiredFirst()
    {
        var result = RefreshTokenFactory.CreateResult(userId: Guid.Empty, expiresOnUtc: TestClock.UtcNow.AddDays(-1), nowUtc: TestClock.UtcNow);

        Assert.Equal(RefreshTokenErrors.UserIdRequired.Code, result.TopError.Code);
    }
}
