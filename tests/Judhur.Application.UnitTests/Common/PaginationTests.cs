using Judhur.Application.Common;

namespace Judhur.Application.UnitTests.Common;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(Pagination.MaxPage)]
    public void IsPageValid_PageWithinRange_ReturnsTrue(int page)
    {
        Assert.True(Pagination.IsPageValid(page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Pagination.MaxPage + 1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void IsPageValid_PageOutOfRange_ReturnsFalse(int page)
    {
        Assert.False(Pagination.IsPageValid(page));
    }
}
