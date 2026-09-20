using StoreHub.Domain.Common;
using StoreHub.Domain.Enums;

namespace StoreHub.Domain.Notifications;

public sealed class NotificationDispatchLog : AuditableDomainEntity
{
    public Guid NotificationId { get; set; }

    public NotificationChannel Channel { get; set; }

    public NotificationDispatchStatus DispatchStatus { get; set; }

    public DateTime AttemptedOnUtc { get; set; }

    public string? ExternalReference { get; set; }

    public string? ErrorMessage { get; set; }

    public Notification Notification { get; set; } = null!;
}
