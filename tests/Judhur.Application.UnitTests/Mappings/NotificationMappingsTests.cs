using Judhur.Application.Features.Notifications.Dto;
using Judhur.Domain.Notifications.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Application.UnitTests.Mappings;

public sealed class NotificationMappingsTests
{
    [Fact]
    public void ToDto_Notification_CopiesAllFields()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();
        var notification = NotificationFactory.Create(userId, NotificationType.NewReview, "عنوان", "نص", referenceId, id);
        notification.CreatedAtUtc = TestClock.UtcNow;

        var dto = notification.ToDto();

        Assert.Equal(id, dto.Id);
        Assert.Equal(NotificationType.NewReview, dto.Type);
        Assert.Equal("عنوان", dto.Title);
        Assert.Equal("نص", dto.Body);
        Assert.Equal(referenceId, dto.ReferenceId);
        Assert.Equal(TestClock.UtcNow, dto.CreatedAtUtc);
    }

    [Fact]
    public void ToDto_UnreadNotification_IsNotRead()
    {
        var dto = NotificationFactory.Create().ToDto();

        Assert.False(dto.IsRead);
    }

    [Fact]
    public void ToDto_ReadNotification_IsRead()
    {
        var notification = NotificationFactory.Create();
        notification.MarkAsRead(TestClock.UtcNow);

        Assert.True(notification.ToDto().IsRead);
    }
}
