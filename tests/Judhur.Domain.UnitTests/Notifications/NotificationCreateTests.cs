using Judhur.Domain.Notifications;
using Judhur.Domain.Notifications.Enums;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Notifications;

public sealed class NotificationCreateTests
{
    [Fact]
    public void Create_ValidNotification_ReturnsSuccess()
    {
        var result = NotificationFactory.CreateResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_ValidNotification_CopiesData()
    {
        var userId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();

        var notification = NotificationFactory.Create(userId, NotificationType.NewMessage, "رسالة جديدة", "لديك رسالة جديدة", referenceId);

        Assert.Equal(userId, notification.UserId);
        Assert.Equal(NotificationType.NewMessage, notification.Type);
        Assert.Equal("رسالة جديدة", notification.Title);
        Assert.Equal("لديك رسالة جديدة", notification.Body);
        Assert.Equal(referenceId, notification.ReferenceId);
    }

    [Fact]
    public void Create_ValidNotification_StartsUnread()
    {
        var notification = NotificationFactory.Create();

        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);
    }

    [Fact]
    public void Create_NullReference_ReturnsSuccessWithoutReference()
    {
        var result = Notification.Create(
            Guid.NewGuid(),
            NotificationFactory.DefaultUserId,
            NotificationType.AccountBanned,
            NotificationFactory.DefaultTitle,
            NotificationFactory.DefaultBody,
            null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ReferenceId);
    }

    [Fact]
    public void Create_GivenId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var notification = NotificationFactory.Create(id: id);

        Assert.Equal(id, notification.Id);
    }

    [Fact]
    public void Create_EmptyId_GeneratesNewId()
    {
        var notification = NotificationFactory.Create(id: Guid.Empty);

        Assert.NotEqual(Guid.Empty, notification.Id);
    }

    [Fact]
    public void Create_EmptyUser_ReturnsUserIdRequired()
    {
        var result = NotificationFactory.CreateResult(userId: Guid.Empty);

        Assert.Equal(NotificationErrors.UserIdRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(NotificationType.PropertyApproved)]
    [InlineData(NotificationType.PropertyRejected)]
    [InlineData(NotificationType.NewMessage)]
    [InlineData(NotificationType.NewReview)]
    [InlineData(NotificationType.ReportResolved)]
    [InlineData(NotificationType.AccountBanned)]
    [InlineData(NotificationType.PropertySuspended)]
    [InlineData(NotificationType.PropertyPendingReview)]
    [InlineData(NotificationType.NewReport)]
    public void Create_DefinedType_ReturnsSuccess(NotificationType type)
    {
        var result = NotificationFactory.CreateResult(type: type);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    public void Create_UndefinedType_ReturnsInvalidType(int type)
    {
        var result = NotificationFactory.CreateResult(type: (NotificationType)type);

        Assert.Equal(NotificationErrors.InvalidType.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptyUserAndUndefinedType_ReturnsUserIdRequiredFirst()
    {
        var result = NotificationFactory.CreateResult(userId: Guid.Empty, type: (NotificationType)99);

        Assert.Equal(NotificationErrors.UserIdRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Create_BlankTitle_ReturnsTitleRequired(string title)
    {
        var result = NotificationFactory.CreateResult(title: title);

        Assert.Equal(NotificationErrors.TitleRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_NullTitle_ReturnsTitleRequired()
    {
        var result = NotificationFactory.CreateResult(title: null!);

        Assert.Equal(NotificationErrors.TitleRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_TitleAtLimit_ReturnsSuccess()
    {
        var result = NotificationFactory.CreateResult(title: new string('ع', Notification.MaxTitleLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_TitleOverLimit_ReturnsTitleTooLong()
    {
        var result = NotificationFactory.CreateResult(title: new string('ع', Notification.MaxTitleLength + 1));

        Assert.Equal(NotificationErrors.TitleTooLong.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankBody_ReturnsBodyRequired(string body)
    {
        var result = NotificationFactory.CreateResult(body: body);

        Assert.Equal(NotificationErrors.BodyRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_NullBody_ReturnsBodyRequired()
    {
        var result = NotificationFactory.CreateResult(body: null!);

        Assert.Equal(NotificationErrors.BodyRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_BodyAtLimit_ReturnsSuccess()
    {
        var result = NotificationFactory.CreateResult(body: new string('ن', Notification.MaxBodyLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_BodyOverLimit_ReturnsBodyTooLong()
    {
        var result = NotificationFactory.CreateResult(body: new string('ن', Notification.MaxBodyLength + 1));

        Assert.Equal(NotificationErrors.BodyTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_TitleOverLimitAndBlankBody_ReturnsTitleTooLongFirst()
    {
        var result = NotificationFactory.CreateResult(title: new string('ع', Notification.MaxTitleLength + 1), body: "");

        Assert.Equal(NotificationErrors.TitleTooLong.Code, result.TopError.Code);
    }
}
