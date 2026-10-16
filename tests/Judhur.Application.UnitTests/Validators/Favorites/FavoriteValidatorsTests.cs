using FluentValidation.TestHelper;

using Judhur.Application.Features.Favorites.Commands.AddFavorite;
using Judhur.Application.Features.Favorites.Commands.RemoveFavorite;

namespace Judhur.Application.UnitTests.Validators.Favorites;

public sealed class FavoriteValidatorsTests
{
    [Fact]
    public void AddFavorite_ValidPropertyId_HasNoErrors()
    {
        new AddFavoriteCommandValidator().TestValidate(new AddFavoriteCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AddFavorite_EmptyPropertyId_ReturnsError()
    {
        new AddFavoriteCommandValidator().TestValidate(new AddFavoriteCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(c => c.PropertyId)
            .WithErrorMessage("معرّف العقار مطلوب.");
    }

    [Fact]
    public void RemoveFavorite_ValidPropertyId_HasNoErrors()
    {
        new RemoveFavoriteCommandValidator().TestValidate(new RemoveFavoriteCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RemoveFavorite_EmptyPropertyId_ReturnsError()
    {
        new RemoveFavoriteCommandValidator().TestValidate(new RemoveFavoriteCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(c => c.PropertyId)
            .WithErrorMessage("معرّف العقار مطلوب.");
    }
}
