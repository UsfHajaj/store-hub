using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.Notifications.DTOs;

public sealed class NotificationFilterRequest
{
    public int Page { get; set; } = PaginationConstants.DefaultPage;

    public int PageSize { get; set; } = PaginationConstants.DefaultPageSize;

    public bool? UnreadOnly { get; set; }
}
