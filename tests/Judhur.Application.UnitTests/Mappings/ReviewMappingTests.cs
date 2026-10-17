using Judhur.Application.Features.Reviews;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Application.UnitTests.Mappings;

public sealed class ReviewMappingTests
{
    [Fact]
    public void ToDto_Review_CopiesAllFields()
    {
        var id = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var review = ReviewFactory.Create(sellerId: sellerId, rating: 3.5m, comment: "  جيد  ", id: id);
        review.CreatedAtUtc = TestClock.UtcNow;
        review.LastModifiedUtc = TestClock.UtcNow.AddHours(1);

        var dto = review.ToDto();

        Assert.Equal(id, dto.Id);
        Assert.Equal(sellerId, dto.SellerId);
        Assert.Equal(3.5m, dto.Rating);
        Assert.Equal("جيد", dto.Comment);
        Assert.Equal(TestClock.UtcNow, dto.CreatedAtUtc);
        Assert.Equal(TestClock.UtcNow.AddHours(1), dto.LastModifiedUtc);
    }

    [Fact]
    public void ToDto_ReviewWithoutComment_HasNullComment()
    {
        Assert.Null(ReviewFactory.Create(comment: "   ").ToDto().Comment);
    }
}
