using FluentValidation.TestHelper;

using Judhur.Application.Features.Sellers.Queries.GetSellerProfile;

namespace Judhur.Application.UnitTests.Validators.Sellers;

public sealed class GetSellerProfileQueryValidatorTests
{
    private readonly GetSellerProfileQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidSellerId_HasNoErrors()
    {
        _validator.TestValidate(new GetSellerProfileQuery(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptySellerId_ReturnsError()
    {
        _validator.TestValidate(new GetSellerProfileQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(q => q.SellerId)
            .WithErrorMessage("معرّف البائع مطلوب.");
    }
}
