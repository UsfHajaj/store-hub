using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.Identity.DTOs;

public sealed class UserFilterRequest
{
    public int Page { get; set; } = PaginationConstants.DefaultPage;

    public int PageSize { get; set; } = PaginationConstants.DefaultPageSize;

    public string? Search { get; set; }
}
