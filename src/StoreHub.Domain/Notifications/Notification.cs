using StoreHub.Domain.Common;
using StoreHub.Domain.Enums;
using StoreHub.Domain.Identity;

namespace StoreHub.Domain.Notifications;

public sealed class Notification : AuditableDomainEntity
{
    public Guid UserId { get; set; }

    public NotificationType NotificationType { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public NotificationChannel Channel { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadOnUtc { get; set; }

    public Guid? RelatedEntityId { get; set; }

    public string? RelatedEntityType { get; set; }

    public User User { get; set; } = null!;

    public ICollection<NotificationDispatchLog> DispatchLogs { get; set; } = new List<NotificationDispatchLog>();
}
