namespace StoreHub.Application.Features.Stores.DTOs;

public sealed class SetStoreMembersRequest
{
    public IReadOnlyList<Guid> UserIds { get; set; } = Array.Empty<Guid>();
}
