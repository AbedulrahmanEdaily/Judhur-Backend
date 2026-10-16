using System.Reflection;

using Judhur.Application.Common;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.UnitTests.Common;

public sealed class ApplicationErrorTests
{
    private static readonly IReadOnlyList<(string Name, Error Error)> AllErrors = [.. typeof(ApplicationError)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(Error))
        .Select(field => (field.Name, (Error)field.GetValue(null)!))];

    [Fact]
    public void AllErrors_AreDiscovered()
    {
        Assert.True(AllErrors.Count >= 25);
    }

    [Fact]
    public void Codes_AreUnique()
    {
        var duplicates = AllErrors
            .GroupBy(e => e.Error.Code)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Codes_AreNotBlankAndHaveNoWhitespace()
    {
        Assert.All(AllErrors, e =>
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Error.Code), e.Name);
            Assert.DoesNotContain(' ', e.Error.Code);
        });
    }

    [Fact]
    public void Descriptions_AreNotBlank()
    {
        Assert.All(AllErrors, e => Assert.False(string.IsNullOrWhiteSpace(e.Error.Description), e.Name));
    }

    [Fact]
    public void PageInvalid_DescriptionMentionsMaxPage()
    {
        Assert.Contains(Pagination.MaxPage.ToString(), ApplicationError.PageInvalid.Description);
    }

    [Theory]
    [InlineData(nameof(ApplicationError.Unauthenticated), ErrorKind.Unauthorized)]
    [InlineData(nameof(ApplicationError.ExpiredAccessTokenInvalid), ErrorKind.Unauthorized)]
    [InlineData(nameof(ApplicationError.UserIdClaimInvalid), ErrorKind.Unauthorized)]
    [InlineData(nameof(ApplicationError.InvalidGoogleToken), ErrorKind.Unauthorized)]
    [InlineData(nameof(ApplicationError.AccountBanned), ErrorKind.Forbidden)]
    [InlineData(nameof(ApplicationError.UserNotFound), ErrorKind.NotFound)]
    [InlineData(nameof(ApplicationError.SellerNotFound), ErrorKind.NotFound)]
    [InlineData(nameof(ApplicationError.DuplicateEmail), ErrorKind.Conflict)]
    [InlineData(nameof(ApplicationError.CannotBanSelf), ErrorKind.Conflict)]
    [InlineData(nameof(ApplicationError.PropertyChangedSinceReview), ErrorKind.Conflict)]
    [InlineData(nameof(ApplicationError.InvalidPassword), ErrorKind.Validation)]
    [InlineData(nameof(ApplicationError.PageInvalid), ErrorKind.Validation)]
    [InlineData(nameof(ApplicationError.PageSizeInvalid), ErrorKind.Validation)]
    [InlineData(nameof(ApplicationError.TokenGenerationFailed), ErrorKind.Failure)]
    [InlineData(nameof(ApplicationError.UploadFailed), ErrorKind.Failure)]
    public void Error_HasExpectedKind(string name, ErrorKind expected)
    {
        var error = AllErrors.Single(e => e.Name == name).Error;

        Assert.Equal(expected, error.Type);
    }
}
