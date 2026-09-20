using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.Notifications.DTOs;

public sealed class NotificationTemplateFilterRequest
{
    public int Page { get; set; } = PaginationConstants.DefaultPage;

    public int PageSize { get; set; } = PaginationConstants.DefaultPageSize;

    public string? Search { get; set; }

    public bool? ActiveOnly { get; set; }
}
