namespace StoreHub.Domain.Stores;

public sealed class StoreMember
{
    public Guid StoreId { get; set; }

    public Guid UserId { get; set; }

    public Store Store { get; set; } = null!;
}
