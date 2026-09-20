using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Notifications.DTOs;

public sealed class NotificationListItemDto
{
    public Guid Id { get; init; }

    public NotificationType NotificationType { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public NotificationChannel Channel { get; init; }

    public bool IsRead { get; init; }

    public Guid? RelatedEntityId { get; init; }

    public string? RelatedEntityType { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}
