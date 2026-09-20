using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.Stores.DTOs;

public sealed class StoreFilterRequest
{
    public int Page { get; set; } = PaginationConstants.DefaultPage;

    public int PageSize { get; set; } = PaginationConstants.DefaultPageSize;

    public string? Search { get; set; }
}
