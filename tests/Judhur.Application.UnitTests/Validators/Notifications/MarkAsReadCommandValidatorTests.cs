using FluentValidation.TestHelper;

using Judhur.Application.Features.Notifications.Commands.MarkAsRead;

namespace Judhur.Application.UnitTests.Validators.Notifications;

public sealed class MarkAsReadCommandValidatorTests
{
    private readonly MarkAsReadCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidNotificationId_HasNoErrors()
    {
        _validator.TestValidate(new MarkAsReadCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyNotificationId_ReturnsError()
    {
        _validator.TestValidate(new MarkAsReadCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(c => c.NotificationId)
            .WithErrorMessage("معرّف الإشعار مطلوب.");
    }
}
