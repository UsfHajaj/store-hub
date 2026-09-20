using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Notifications.DTOs;

public sealed class CreateNotificationTemplateRequest
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? SubjectTemplate { get; set; }

    public string BodyTemplate { get; set; } = string.Empty;

    public NotificationType NotificationType { get; set; }

    public NotificationChannel Channel { get; set; }
}
