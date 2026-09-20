using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Notifications.DTOs;

public sealed class NotificationTemplateDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? SubjectTemplate { get; init; }

    public string BodyTemplate { get; init; } = string.Empty;

    public NotificationType NotificationType { get; init; }

    public NotificationChannel Channel { get; init; }

    public bool IsActive { get; init; }
}
