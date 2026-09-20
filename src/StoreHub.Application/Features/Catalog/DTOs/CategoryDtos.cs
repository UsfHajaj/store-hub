using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.Catalog.DTOs;

public sealed class CategoryDto
{
    public Guid Id { get; init; }

    public Guid StoreId { get; init; }

    public Guid? ParentCategoryId { get; init; }

    public string? ParentNameAr { get; init; }

    public string? ParentNameEn { get; init; }

    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public int SortOrder { get; init; }

    public bool IsActive { get; init; }

    public int ProductCount { get; init; }

    public int ChildCount { get; init; }
}

public sealed class CategoryFilterRequest
{
    public int Page { get; init; } = PaginationConstants.DefaultPage;

    public int PageSize { get; init; } = PaginationConstants.DefaultPageSize;

    public string? Search { get; init; }
}

public sealed class CategoryTreeNodeDto
{
    public Guid Id { get; init; }

    public Guid? ParentCategoryId { get; init; }

    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public int SortOrder { get; init; }

    public bool IsActive { get; init; }

    public int ProductCount { get; init; }

    public IReadOnlyList<CategoryTreeNodeDto> Children { get; init; } = Array.Empty<CategoryTreeNodeDto>();
}

public sealed class CreateCategoryRequest
{
    public Guid? ParentCategoryId { get; init; }

    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateCategoryRequest
{
    public Guid? ParentCategoryId { get; init; }

    public string NameAr { get; init; } = null!;

    public string NameEn { get; init; } = null!;

    public bool IsActive { get; init; }
}
