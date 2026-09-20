using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Notifications.DTOs;

public sealed class CreateNotificationBatchRequest
{
    public IReadOnlyList<Guid> UserIds { get; set; } = Array.Empty<Guid>();

    public NotificationType NotificationType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    public Guid? RelatedEntityId { get; set; }

    public string? RelatedEntityType { get; set; }
}
