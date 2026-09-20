using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.Inventory.DTOs;

public sealed class StocktakeFilterRequest
{
    public int Page { get; init; } = PaginationConstants.DefaultPage;

    public int PageSize { get; init; } = PaginationConstants.DefaultPageSize;
}

public sealed class InventoryFilterRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public bool LowStockOnly { get; init; }
}

public sealed class InventoryItemDto
{
    public Guid ProductId { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public string? Barcode { get; init; }

    public string CategoryNameAr { get; init; } = string.Empty;

    public string CategoryNameEn { get; init; } = string.Empty;

    public decimal StockQuantity { get; init; }

    public decimal ReorderLevel { get; init; }

    public bool IsLowStock { get; init; }

    public bool IsActive { get; init; }
}

public sealed class AdjustStockRequest
{
    public Guid ProductId { get; init; }

    public decimal QuantityChange { get; init; }

    public string? Notes { get; init; }
}

public sealed class StocktakeListItemDto
{
    public Guid Id { get; init; }

    public byte Status { get; init; }

    public Guid StartedByUserId { get; init; }

    public string StartedByName { get; init; } = string.Empty;

    public DateTime CreatedOnUtc { get; init; }

    public DateTime? CompletedOnUtc { get; init; }

    public int LineCount { get; init; }

    public string? Notes { get; init; }
}

public sealed class StocktakeLineDto
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string ProductNameAr { get; init; } = string.Empty;

    public string ProductNameEn { get; init; } = string.Empty;

    public decimal SystemQuantity { get; init; }

    public decimal? CountedQuantity { get; init; }

    public decimal Difference { get; init; }
}

public sealed class StocktakeDto
{
    public Guid Id { get; init; }

    public Guid StoreId { get; init; }

    public byte Status { get; init; }

    public Guid StartedByUserId { get; init; }

    public string StartedByName { get; init; } = string.Empty;

    public DateTime CreatedOnUtc { get; init; }

    public DateTime? CompletedOnUtc { get; init; }

    public string? Notes { get; init; }

    public IReadOnlyList<StocktakeLineDto> Lines { get; init; } = Array.Empty<StocktakeLineDto>();
}

public sealed class CreateStocktakeRequest
{
    public string? Notes { get; init; }
}

public sealed class UpdateStocktakeLineRequest
{
    public Guid ProductId { get; init; }

    public decimal CountedQuantity { get; init; }
}

public sealed class UpdateStocktakeLinesRequest
{
    public IReadOnlyList<UpdateStocktakeLineRequest> Lines { get; init; } = Array.Empty<UpdateStocktakeLineRequest>();
}
