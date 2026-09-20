using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.Catalog.DTOs;

public sealed class DiscountFilterRequest
{
    public int Page { get; init; } = PaginationConstants.DefaultPage;

    public int PageSize { get; init; } = PaginationConstants.DefaultPageSize;

    public string? Search { get; init; }
}

public sealed class DiscountDto
{
    public Guid Id { get; init; }

    public Guid StoreId { get; init; }

    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public decimal DiscountPercent { get; init; }

    public DateTime? StartsAtUtc { get; init; }

    public DateTime? EndsAtUtc { get; init; }

    public bool AppliesToAllProducts { get; init; }

    public Guid? CategoryId { get; init; }

    public string? CategoryNameAr { get; init; }

    public string? CategoryNameEn { get; init; }

    public bool IsActive { get; init; }

    public IReadOnlyList<Guid> ProductIds { get; init; } = Array.Empty<Guid>();

    public int ProductCount { get; init; }
}

public sealed class CreateDiscountRequest
{
    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public decimal DiscountPercent { get; init; }

    public DateTime? StartsAtUtc { get; init; }

    public DateTime? EndsAtUtc { get; init; }

    public bool AppliesToAllProducts { get; init; }

    public Guid? CategoryId { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<Guid>? ProductIds { get; init; }
}

public sealed class UpdateDiscountRequest
{
    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public decimal DiscountPercent { get; init; }

    public DateTime? StartsAtUtc { get; init; }

    public DateTime? EndsAtUtc { get; init; }

    public bool AppliesToAllProducts { get; init; }

    public Guid? CategoryId { get; init; }

    public bool IsActive { get; init; }

    public IReadOnlyList<Guid>? ProductIds { get; init; }
}
