using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Notifications.DTOs;

public sealed class CreateNotificationRequest
{
    public Guid UserId { get; set; }

    public NotificationType NotificationType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    public Guid? RelatedEntityId { get; set; }

    public string? RelatedEntityType { get; set; }
}
