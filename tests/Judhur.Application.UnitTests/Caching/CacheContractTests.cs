using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Commands.DeleteMyAccount;
using Judhur.Application.Features.Identity.Commands.RemoveProfileImage;
using Judhur.Application.Features.Identity.Commands.UpdateMyProfile;
using Judhur.Application.Features.Properties.Commands.ApproveProperty;
using Judhur.Application.Features.Properties.Commands.DeleteProperty;
using Judhur.Application.Features.Properties.Queries.GetPropertyById;
using Judhur.Application.Features.Reviews.Commands.CreateReview;
using Judhur.Application.Features.Reviews.Commands.DeleteReview;
using Judhur.Application.Features.Reviews.Commands.UpdateReview;
using Judhur.Application.Features.Sellers.Queries.GetSellerProfile;
using Judhur.Application.Features.Users.Commands.BanUser;
using Judhur.Application.Features.Users.Commands.UnbanUser;

namespace Judhur.Application.UnitTests.Caching;

public sealed class CacheContractTests
{
    private static readonly Guid Id = Guid.Parse("0199a1b2-0000-7000-8000-0000000000c1");

    [Fact]
    public void GetPropertyById_Member_UsesMemberKey()
    {
        Assert.Equal($"properties:{Id}:member", new GetPropertyByIdQuery(Id, true).CacheKey);
    }

    [Fact]
    public void GetPropertyById_Guest_UsesGuestKey()
    {
        Assert.Equal($"properties:{Id}:guest", new GetPropertyByIdQuery(Id, false).CacheKey);
    }

    [Fact]
    public void GetPropertyById_TagsAndExpiration()
    {
        ICachedQuery query = new GetPropertyByIdQuery(Id, false);

        Assert.Equal(["properties", "sellers"], query.Tags);
        Assert.Equal(TimeSpan.FromMinutes(10), query.Expiration);
        Assert.True(query.IsCacheable);
    }

    [Fact]
    public void GetSellerProfile_KeyTagsAndExpiration()
    {
        ICachedQuery query = new GetSellerProfileQuery(Id);

        Assert.Equal($"sellers:{Id}", query.CacheKey);
        Assert.Equal(["properties", "sellers"], query.Tags);
        Assert.Equal(TimeSpan.FromMinutes(10), query.Expiration);
        Assert.True(query.IsCacheable);
    }

    public static TheoryData<IInvalidateCacheCommand, string[]> InvalidatingCommands => new()
    {
        { new ApprovePropertyCommand(Id, "AAAAAAAAB9E="), ["properties"] },
        { new DeletePropertyCommand(Id), ["properties"] },
        { new UpdateMyProfileCommand("علي", "0591234567", "رام الله", null), ["properties"] },
        { new RemoveProfileImageCommand(), ["properties"] },
        { new DeleteMyAccountCommand("Passw0rd"), ["properties", "sellers"] },
        { new BanUserCommand(Id, "احتيال"), ["properties", "sellers"] },
        { new UnbanUserCommand(Id), ["sellers"] },
        { new CreateReviewCommand(Id, 5m, null), ["sellers"] },
        { new UpdateReviewCommand(Id, 5m, null), ["sellers"] },
        { new DeleteReviewCommand(Id), ["sellers"] },
    };

    [Theory]
    [MemberData(nameof(InvalidatingCommands))]
    public void InvalidatingCommand_DeclaresExpectedTags(IInvalidateCacheCommand command, string[] expected)
    {
        Assert.Equal(expected, command.Tags);
    }
}
