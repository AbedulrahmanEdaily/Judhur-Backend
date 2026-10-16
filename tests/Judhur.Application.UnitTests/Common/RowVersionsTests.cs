using Judhur.Application.Common;

namespace Judhur.Application.UnitTests.Common;

public sealed class RowVersionsTests
{
    private static readonly byte[] SampleVersion = [0, 0, 0, 0, 0, 0, 7, 209];

    [Fact]
    public void TryParse_EightByteBase64_ReturnsTrueWithBytes()
    {
        var parsed = RowVersions.TryParse("AAAAAAAAB9E=", out var rowVersion);

        Assert.True(parsed);
        Assert.Equal(SampleVersion, rowVersion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData("AAAAAA==")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")]
    [InlineData("AAAAAAAAB9")]
    public void TryParse_InvalidValue_ReturnsFalse(string? value)
    {
        Assert.False(RowVersions.TryParse(value, out _));
    }

    [Fact]
    public void TryParse_InvalidValue_StillReturnsEightByteBuffer()
    {
        RowVersions.TryParse(null, out var rowVersion);

        Assert.Equal(8, rowVersion.Length);
    }

    [Fact]
    public void Format_EightBytes_ReturnsBase64()
    {
        Assert.Equal("AAAAAAAAB9E=", RowVersions.Format(SampleVersion));
    }

    [Fact]
    public void Format_ThenTryParse_RoundTrips()
    {
        byte[] original = [1, 2, 3, 4, 5, 6, 7, 8];

        var parsed = RowVersions.TryParse(RowVersions.Format(original), out var rowVersion);

        Assert.True(parsed);
        Assert.Equal(original, rowVersion);
    }

    [Fact]
    public void PropertyName_IsRowVersion()
    {
        Assert.Equal("RowVersion", RowVersions.PropertyName);
    }
}
