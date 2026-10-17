using Judhur.Application.Common;

namespace Judhur.Application.UnitTests.Common;

public sealed class TokenHashingTests
{
    [Fact]
    public void Hash_KnownValue_ReturnsUppercaseSha256Hex()
    {
        Assert.Equal(
            "9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08",
            TokenHashing.Hash("test"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("refresh-token")]
    [InlineData("رمز عربي")]
    public void Hash_AnyValue_ReturnsHashLengthCharacters(string value)
    {
        Assert.Equal(TokenHashing.HashLength, TokenHashing.Hash(value).Length);
    }

    [Fact]
    public void Hash_SameValue_IsDeterministic()
    {
        Assert.Equal(TokenHashing.Hash("123456"), TokenHashing.Hash("123456"));
    }

    [Fact]
    public void Hash_DifferentValues_ReturnsDifferentHashes()
    {
        Assert.NotEqual(TokenHashing.Hash("123456"), TokenHashing.Hash("123457"));
    }

    [Fact]
    public void Matches_HashOfSameValue_ReturnsTrue()
    {
        var hash = TokenHashing.Hash("123456");

        Assert.True(TokenHashing.Matches(hash, "123456"));
    }

    [Fact]
    public void Matches_HashOfDifferentValue_ReturnsFalse()
    {
        var hash = TokenHashing.Hash("123456");

        Assert.False(TokenHashing.Matches(hash, "654321"));
    }

    [Fact]
    public void Matches_TruncatedHash_ReturnsFalse()
    {
        var hash = TokenHashing.Hash("123456")[..32];

        Assert.False(TokenHashing.Matches(hash, "123456"));
    }

    [Fact]
    public void Matches_EmptyExpectedHash_ReturnsFalse()
    {
        Assert.False(TokenHashing.Matches(string.Empty, "123456"));
    }
}
