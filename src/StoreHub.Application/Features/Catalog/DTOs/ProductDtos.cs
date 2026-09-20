namespace StoreHub.Application.Features.Catalog.DTOs;

public sealed class ProductListItemDto
{
    public Guid Id { get; init; }

    public Guid CategoryId { get; init; }

    public string CategoryNameAr { get; init; } = null!;

    public string CategoryNameEn { get; init; } = null!;

    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public string? Barcode { get; init; }

    public string? Sku { get; init; }

    public decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public decimal StockQuantity { get; init; }

    public bool TracksInventory { get; init; } = true;

    public bool IsActive { get; init; }
}

public sealed class ProductDto
{
    public Guid Id { get; init; }

    public Guid StoreId { get; init; }

    public Guid CategoryId { get; init; }

    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public string? DescriptionAr { get; init; }

    public string? DescriptionEn { get; init; }

    public string? Barcode { get; init; }

    public string? Sku { get; init; }

    public decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public decimal StockQuantity { get; init; }

    public bool TracksInventory { get; init; } = true;

    public bool IsActive { get; init; }
}

public sealed class ProductFilterRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public Guid? CategoryId { get; init; }
}

public sealed class CreateProductRequest
{
    public Guid CategoryId { get; init; }

    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public string? DescriptionAr { get; init; }

    public string? DescriptionEn { get; init; }

    public string? Barcode { get; init; }

    public string? Sku { get; init; }

    public decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public decimal StockQuantity { get; init; }

    public bool TracksInventory { get; init; } = true;

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateProductRequest
{
    public Guid CategoryId { get; init; }

    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public string? DescriptionAr { get; init; }

    public string? DescriptionEn { get; init; }

    public string? Barcode { get; init; }

    public string? Sku { get; init; }

    public decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public decimal StockQuantity { get; init; }

    public bool TracksInventory { get; init; } = true;

    public bool IsActive { get; init; }
}

public sealed class ProductImageUploadDto
{
    public string ImageUrl { get; init; } = null!;
}
