using StoreHub.Domain.Enums;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Common;

public abstract class AuditableDomainEntity : AuditableEntity
{
    public RecordStatus RecordStatus { get; set; } = RecordStatus.Active;

    public DateTime? DeletedOnUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
