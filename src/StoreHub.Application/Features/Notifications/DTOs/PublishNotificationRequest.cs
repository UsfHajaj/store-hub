using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Notifications.DTOs;

/// <summary>
/// Payload for system-generated notifications (no target user — resolved by the publisher).
/// </summary>
public sealed class PublishNotificationRequest
{
    public NotificationType NotificationType { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public NotificationChannel Channel { get; init; } = NotificationChannel.InApp;

    public Guid? RelatedEntityId { get; init; }

    public string? RelatedEntityType { get; init; }
}
