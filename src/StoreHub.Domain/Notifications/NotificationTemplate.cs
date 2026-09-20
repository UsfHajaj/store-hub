using StoreHub.Domain.Common;
using StoreHub.Domain.Enums;

namespace StoreHub.Domain.Notifications;

public sealed class NotificationTemplate : AuditableDomainEntity
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? SubjectTemplate { get; set; }

    public string BodyTemplate { get; set; } = null!;

    public NotificationType NotificationType { get; set; }

    public NotificationChannel Channel { get; set; }

    public bool IsActive { get; set; } = true;
}
