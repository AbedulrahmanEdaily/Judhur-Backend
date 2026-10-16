using FluentValidation;
using FluentValidation.TestHelper;

using Judhur.Application.Features.Properties.Commands.DeactivateProperty;
using Judhur.Application.Features.Properties.Commands.DeleteProperty;
using Judhur.Application.Features.Properties.Commands.DeletePropertyImage;
using Judhur.Application.Features.Properties.Commands.MarkPropertyAsRented;
using Judhur.Application.Features.Properties.Commands.MarkPropertyAsSold;
using Judhur.Application.Features.Properties.Commands.ReactivateProperty;
using Judhur.Application.Features.Properties.Commands.ResubmitPropertyForReview;
using Judhur.Application.Features.Properties.Commands.SetMainPropertyImage;
using Judhur.Application.Features.Properties.Commands.UpdatePropertyDescription;
using Judhur.Application.Features.Properties.Queries.GetMyPropertyById;
using Judhur.Application.Features.Properties.Queries.GetPropertyById;
using Judhur.Domain.Properties;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Properties;

public sealed class PropertyIdValidatorsTests
{
    private const string PropertyIdRequired = "معرّف العقار مطلوب.";

    private static void AssertPropertyIdRule<T>(IValidator<T> validator, Func<Guid, T> create)
    {
        Assert.True(validator.Validate(create(Guid.NewGuid())).IsValid);

        var failure = Assert.Single(validator.Validate(create(Guid.Empty)).Errors);
        Assert.Equal("PropertyId", failure.PropertyName);
        Assert.Equal(PropertyIdRequired, failure.ErrorMessage);
    }

    [Fact]
    public void DeactivateProperty_PropertyIdRule()
        => AssertPropertyIdRule(new DeactivatePropertyCommandValidator(), id => new DeactivatePropertyCommand(id));

    [Fact]
    public void DeleteProperty_PropertyIdRule()
        => AssertPropertyIdRule(new DeletePropertyCommandValidator(), id => new DeletePropertyCommand(id));

    [Fact]
    public void MarkPropertyAsRented_PropertyIdRule()
        => AssertPropertyIdRule(new MarkPropertyAsRentedCommandValidator(), id => new MarkPropertyAsRentedCommand(id));

    [Fact]
    public void MarkPropertyAsSold_PropertyIdRule()
        => AssertPropertyIdRule(new MarkPropertyAsSoldCommandValidator(), id => new MarkPropertyAsSoldCommand(id));

    [Fact]
    public void ReactivateProperty_PropertyIdRule()
        => AssertPropertyIdRule(new ReactivatePropertyCommandValidator(), id => new ReactivatePropertyCommand(id));

    [Fact]
    public void ResubmitPropertyForReview_PropertyIdRule()
        => AssertPropertyIdRule(new ResubmitPropertyForReviewCommandValidator(), id => new ResubmitPropertyForReviewCommand(id));

    [Fact]
    public void GetMyPropertyById_PropertyIdRule()
        => AssertPropertyIdRule(new GetMyPropertyByIdQueryValidator(), id => new GetMyPropertyByIdQuery(id));

    [Fact]
    public void GetPropertyById_PropertyIdRule()
        => AssertPropertyIdRule(new GetPropertyByIdQueryValidator(), id => new GetPropertyByIdQuery(id, false));

    [Fact]
    public void DeletePropertyImage_PropertyIdRule()
        => AssertPropertyIdRule(new DeletePropertyImageCommandValidator(), id => new DeletePropertyImageCommand(id, Guid.NewGuid()));

    [Fact]
    public void SetMainPropertyImage_PropertyIdRule()
        => AssertPropertyIdRule(new SetMainPropertyImageCommandValidator(), id => new SetMainPropertyImageCommand(id, Guid.NewGuid()));

    [Fact]
    public void UpdatePropertyDescription_PropertyIdRule()
        => AssertPropertyIdRule(new UpdatePropertyDescriptionCommandValidator(), id => new UpdatePropertyDescriptionCommand(id, "وصف"));

    [Fact]
    public void DeletePropertyImage_EmptyImageId_ReturnsError()
    {
        new DeletePropertyImageCommandValidator()
            .TestValidate(new DeletePropertyImageCommand(Guid.NewGuid(), Guid.Empty))
            .ShouldHaveValidationErrorFor(c => c.ImageId)
            .WithErrorMessage("معرّف الصورة مطلوب.");
    }

    [Fact]
    public void SetMainPropertyImage_EmptyImageId_ReturnsError()
    {
        new SetMainPropertyImageCommandValidator()
            .TestValidate(new SetMainPropertyImageCommand(Guid.NewGuid(), Guid.Empty))
            .ShouldHaveValidationErrorFor(c => c.ImageId)
            .WithErrorMessage("معرّف الصورة مطلوب.");
    }

    [Fact]
    public void UpdatePropertyDescription_NullDescription_HasNoErrors()
    {
        new UpdatePropertyDescriptionCommandValidator()
            .TestValidate(new UpdatePropertyDescriptionCommand(Guid.NewGuid(), null))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdatePropertyDescription_DescriptionAtMaxLength_HasNoErrors()
    {
        new UpdatePropertyDescriptionCommandValidator()
            .TestValidate(new UpdatePropertyDescriptionCommand(Guid.NewGuid(), Text(Property.MaxDescriptionLength)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdatePropertyDescription_DescriptionOverMaxLength_ReturnsError()
    {
        new UpdatePropertyDescriptionCommandValidator()
            .TestValidate(new UpdatePropertyDescriptionCommand(Guid.NewGuid(), Text(Property.MaxDescriptionLength + 1)))
            .ShouldHaveValidationErrorFor(c => c.Description)
            .WithErrorMessage($"لا يمكن أن يتجاوز الوصف {Property.MaxDescriptionLength} حرف.");
    }
}
