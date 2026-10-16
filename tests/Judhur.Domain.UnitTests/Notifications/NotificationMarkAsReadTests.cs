using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Notifications;

public sealed class NotificationMarkAsReadTests
{
    [Fact]
    public void MarkAsRead_Unread_SetsReadTime()
    {
        var notification = NotificationFactory.Create();
        var readAt = TestClock.UtcNow.AddMinutes(5);

        notification.MarkAsRead(readAt);

        Assert.True(notification.IsRead);
        Assert.Equal(readAt, notification.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_AlreadyRead_KeepsFirstReadTime()
    {
        var notification = NotificationFactory.Create();
        var firstReadAt = TestClock.UtcNow.AddMinutes(5);
        notification.MarkAsRead(firstReadAt);

        notification.MarkAsRead(firstReadAt.AddHours(1));

        Assert.True(notification.IsRead);
        Assert.Equal(firstReadAt, notification.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_AlreadyReadWithEarlierTime_KeepsFirstReadTime()
    {
        var notification = NotificationFactory.Create();
        var firstReadAt = TestClock.UtcNow.AddMinutes(5);
        notification.MarkAsRead(firstReadAt);

        notification.MarkAsRead(firstReadAt.AddHours(-1));

        Assert.Equal(firstReadAt, notification.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_Unread_KeepsContent()
    {
        var notification = NotificationFactory.Create();

        notification.MarkAsRead(TestClock.UtcNow);

        Assert.Equal(NotificationFactory.DefaultTitle, notification.Title);
        Assert.Equal(NotificationFactory.DefaultBody, notification.Body);
        Assert.Equal(NotificationFactory.DefaultUserId, notification.UserId);
    }
}
