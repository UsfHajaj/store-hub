using Microsoft.EntityFrameworkCore;
using StoreHub.Application.Common;
using StoreHub.Application.Features.Catalog.DTOs;
using StoreHub.Application.Features.Catalog.Interfaces;
using StoreHub.Application.Features.DataImport.DTOs;
using StoreHub.Application.Features.DataImport.Interfaces;
using StoreHub.Application.Features.Identity.DTOs;
using StoreHub.Application.Features.Identity.Interfaces;
using StoreHub.Application.Features.Stores.DTOs;
using StoreHub.Application.Features.Stores.Interfaces;
using StoreHub.Domain.Enums;
using StoreHub.Persistence;
using StoreHub.Shared.Identity;
using StoreHub.Shared.Results;

namespace StoreHub.Infrastructure.Excel;

public sealed class DataImportService : IDataImportService
{
    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly IDiscountService _discountService;
    private readonly IStoreService _storeService;
    private readonly IUserService _userService;

    public DataImportService(
        StoreHubDbContext db,
        ICurrentUserService currentUser,
        ICategoryService categoryService,
        IProductService productService,
        IDiscountService discountService,
        IStoreService storeService,
        IUserService userService)
    {
        _db = db;
        _currentUser = currentUser;
        _categoryService = categoryService;
        _productService = productService;
        _discountService = discountService;
        _storeService = storeService;
        _userService = userService;
    }

    public byte[] GetTemplate(DataImportKind kind, bool sample) =>
        kind switch
        {
            DataImportKind.Categories => BuildCategoriesTemplate(sample),
            DataImportKind.Products => BuildProductsTemplate(sample),
            DataImportKind.Stores => BuildStoresTemplate(sample),
            DataImportKind.Users => BuildUsersTemplate(sample),
            DataImportKind.All => BuildAllTemplate(sample),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    public async Task<Result<DataImportResultDto>> ImportAsync(
        DataImportKind kind,
        Stream fileStream,
        Guid? storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        if (kind == DataImportKind.All)
        {
            return await ImportAllAsync(fileStream, storeId, canManageAllStores, cancellationToken)
                .ConfigureAwait(false);
        }

        IReadOnlyList<Dictionary<string, string>> rows;
        try
        {
            rows = ExcelSheetHelper.ReadRows(fileStream);
        }
        catch (Exception ex)
        {
            return Result<DataImportResultDto>.Fail($"Could not read Excel file: {ex.Message}");
        }

        if (rows.Count == 0)
        {
            return Result<DataImportResultDto>.Fail("The file has no data rows.");
        }

        return kind switch
        {
            DataImportKind.Categories => await ImportCategoriesAsync(rows, storeId, canManageAllStores, cancellationToken)
                .ConfigureAwait(false),
            DataImportKind.Products => await ImportProductsAsync(rows, storeId, canManageAllStores, cancellationToken)
                .ConfigureAwait(false),
            DataImportKind.Stores => await ImportStoresAsync(rows, cancellationToken).ConfigureAwait(false),
            DataImportKind.Users => await ImportUsersAsync(rows, cancellationToken).ConfigureAwait(false),
            _ => Result<DataImportResultDto>.Fail("Unknown import type.")
        };
    }

    private static byte[] BuildAllTemplate(bool sample)
    {
        var sheets = new List<ExcelSheetSpec>
        {
            new(
                "Stores",
                new[] { "NameAr", "NameEn", "DescriptionAr", "DescriptionEn", "StoreType" },
                sample
                    ? new List<IReadOnlyList<object?>>
                    {
                        new object?[] { "فرع المعادي", "Maadi branch", "كافيه", "Cafe branch", "Cafe" }
                    }
                    : null),
            new(
                "Categories",
                new[] { "StoreNameAr", "NameAr", "NameEn", "ParentNameAr", "IsActive" },
                sample
                    ? new List<IReadOnlyList<object?>>
                    {
                        new object?[] { "فرع المعادي", "مشروبات", "Drinks", "", true },
                        new object?[] { "فرع المعادي", "ساخنة", "Hot", "مشروبات", true },
                        new object?[] { "فرع المعادي", "حلويات", "Desserts", "", true }
                    }
                    : null),
            new(
                "Products",
                new[]
                {
                    "StoreNameAr", "NameAr", "NameEn", "CategoryNameAr", "Barcode", "Sku", "Price",
                    "StockQuantity", "TracksInventory", "IsActive", "DiscountPercent", "DiscountNameAr",
                    "DiscountNameEn"
                },
                sample
                    ? new List<IReadOnlyList<object?>>
                    {
                        new object?[]
                        {
                            "فرع المعادي", "قهوة", "Coffee", "مشروبات", "1001", "SKU-C1", 25, 0, false, true, 10,
                            "خصم قهوة", "Coffee deal"
                        },
                        new object?[]
                        {
                            "فرع المعادي", "كرواسون", "Croissant", "حلويات", "1002", "SKU-D1", 30, 40, true, true, "",
                            "", ""
                        }
                    }
                    : null),
            new(
                "Users",
                new[] { "UserName", "NameAr", "NameEn", "Email", "Password", "Roles" },
                sample
                    ? new List<IReadOnlyList<object?>>
                    {
                        new object?[]
                        {
                            "cashier1", "كاشير ١", "Cashier 1", "cashier1@example.com", "Pass@12345", "Cashier"
                        }
                    }
                    : null)
        };

        return ExcelSheetHelper.BuildMultiSheetWorkbook(
            sheets,
            "ملف واحد فيه كل الشيتات. الترتيب: Stores ثم Categories ثم Products ثم Users. في التصنيفات والمنتجات اكتب StoreNameAr ليطابق اسم المحل. DiscountPercent اختياري بجانب المنتج.",
            "One file with all sheets. Order: Stores → Categories → Products → Users. Set StoreNameAr on Categories/Products to match the store name. DiscountPercent next to a product is optional.");
    }

    private async Task<Result<DataImportResultDto>> ImportAllAsync(
        Stream fileStream,
        Guid? storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        ms.Position = 0;

        ClosedXML.Excel.XLWorkbook wb;
        try
        {
            wb = new ClosedXML.Excel.XLWorkbook(ms);
        }
        catch (Exception ex)
        {
            return Result<DataImportResultDto>.Fail($"Could not read Excel file: {ex.Message}");
        }

        using (wb)
        {
            var created = 0;
            var skipped = 0;
            var failed = 0;
            var errors = new List<string>();

            async Task Merge(string sheet, Func<IReadOnlyList<Dictionary<string, string>>, Task<Result<DataImportResultDto>>> run)
            {
                if (!ExcelSheetHelper.HasSheet(wb, sheet))
                {
                    return;
                }

                var rows = ExcelSheetHelper.ReadRows(wb, sheet);
                if (rows.Count == 0)
                {
                    return;
                }

                var part = await run(rows).ConfigureAwait(false);
                if (part.IsFailure)
                {
                    failed++;
                    errors.Add($"{sheet}: {string.Join("; ", part.Errors)}");
                    return;
                }

                created += part.Value!.Created;
                skipped += part.Value.Skipped;
                failed += part.Value.Failed;
                errors.AddRange(part.Value.Errors.Select(e => $"{sheet}: {e}"));
            }

            await Merge("Stores", rows => ImportStoresAsync(rows, cancellationToken)).ConfigureAwait(false);
            await Merge(
                    "Categories",
                    rows => ImportCategoriesAsync(rows, storeId, canManageAllStores, cancellationToken))
                .ConfigureAwait(false);
            await Merge(
                    "Products",
                    rows => ImportProductsAsync(rows, storeId, canManageAllStores, cancellationToken))
                .ConfigureAwait(false);
            await Merge("Users", rows => ImportUsersAsync(rows, cancellationToken)).ConfigureAwait(false);

            if (created == 0 && skipped == 0 && failed == 0 && errors.Count == 0)
            {
                return Result<DataImportResultDto>.Fail(
                    "No data found. Expected sheets: Stores, Categories, Products, Users.");
            }

            return Result<DataImportResultDto>.Ok(new DataImportResultDto
            {
                Created = created,
                Skipped = skipped,
                Failed = failed,
                Errors = errors.Take(50).ToList()
            });
        }
    }

    private static byte[] BuildCategoriesTemplate(bool sample)
    {
        var headers = new[] { "NameAr", "NameEn", "ParentNameAr", "IsActive" };
        IReadOnlyList<IReadOnlyList<object?>>? rows = sample
            ? new List<IReadOnlyList<object?>>
            {
                new object?[] { "مشروبات", "Drinks", "", true },
                new object?[] { "ساخنة", "Hot", "مشروبات", true },
                new object?[] { "حلويات", "Desserts", "", true }
            }
            : null;

        return ExcelSheetHelper.BuildWorkbook(
            "Categories",
            headers,
            rows,
            "املأ الاسم بالعربي والإنجليزي. ParentNameAr اختياري لاسم التصنيف الأب. IsActive: true/false أو نعم/لا.",
            "Fill Arabic/English names. ParentNameAr is optional parent category name. IsActive: true/false.");
    }

    private static byte[] BuildProductsTemplate(bool sample)
    {
        var headers = new[]
        {
            "NameAr", "NameEn", "CategoryNameAr", "Barcode", "Sku", "Price", "StockQuantity",
            "TracksInventory", "IsActive", "DiscountPercent", "DiscountNameAr", "DiscountNameEn"
        };
        IReadOnlyList<IReadOnlyList<object?>>? rows = sample
            ? new List<IReadOnlyList<object?>>
            {
                new object?[] { "قهوة", "Coffee", "مشروبات", "1001", "SKU-C1", 25, 0, false, true, 10, "خصم قهوة", "Coffee deal" },
                new object?[] { "كرواسون", "Croissant", "حلويات", "1002", "SKU-D1", 30, 40, true, true, "", "", "" },
                new object?[] { "شاي", "Tea", "مشروبات", "1003", "SKU-T1", 15, 100, true, true, 50, "نصف السعر", "Half price" }
            }
            : null;

        return ExcelSheetHelper.BuildWorkbook(
            "Products",
            headers,
            rows,
            "CategoryNameAr يجب أن يطابق تصنيف موجود في المحل. DiscountPercent اختياري (مثلاً 10) لإنشاء خصم مربوط بالمنتج. TracksInventory=false للمنتجات بدون مخزون مثل القهوة.",
            "CategoryNameAr must match an existing store category. Optional DiscountPercent creates a product discount. TracksInventory=false for non-stock items.");
    }

    private static byte[] BuildStoresTemplate(bool sample)
    {
        var headers = new[] { "NameAr", "NameEn", "DescriptionAr", "DescriptionEn", "StoreType" };
        IReadOnlyList<IReadOnlyList<object?>>? rows = sample
            ? new List<IReadOnlyList<object?>>
            {
                new object?[] { "فرع المعادي", "Maadi branch", "كافيه", "Cafe branch", "Cafe" },
                new object?[] { "فرع مدينة نصر", "Nasr City", "", "", "Restaurant" }
            }
            : null;

        return ExcelSheetHelper.BuildWorkbook(
            "Stores",
            headers,
            rows,
            "StoreType أحد القيم: Cafe, Restaurant, Clothing, Cosmetics, Nuts, Other",
            "StoreType one of: Cafe, Restaurant, Clothing, Cosmetics, Nuts, Other");
    }

    private static byte[] BuildUsersTemplate(bool sample)
    {
        var headers = new[] { "UserName", "NameAr", "NameEn", "Email", "Password", "Roles" };
        IReadOnlyList<IReadOnlyList<object?>>? rows = sample
            ? new List<IReadOnlyList<object?>>
            {
                new object?[] { "cashier1", "كاشير ١", "Cashier 1", "cashier1@example.com", "Pass@12345", "Cashier" },
                new object?[] { "manager1", "مدير ١", "Manager 1", "manager1@example.com", "Pass@12345", "Manager" }
            }
            : null;

        return ExcelSheetHelper.BuildWorkbook(
            "Users",
            headers,
            rows,
            "Roles أسماء أدوار مفصولة بفاصلة وتطابق اسم الدور الموجود في النظام (عربي أو إنجليزي).",
            "Roles is a comma-separated list of existing role names (Arabic or English).");
    }

    private async Task<Result<DataImportResultDto>> ImportCategoriesAsync(
        IReadOnlyList<Dictionary<string, string>> rows,
        Guid? storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var skipped = 0;
        var failed = 0;
        var errors = new List<string>();

        // Pass 1: parents (no ParentNameAr), Pass 2: children
        foreach (var pass in new[] { 1, 2 })
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var parentName = ExcelSheetHelper.Get(row, "ParentNameAr", "ParentName");
                var isChild = !string.IsNullOrWhiteSpace(parentName);
                if (pass == 1 && isChild)
                {
                    continue;
                }

                if (pass == 2 && !isChild)
                {
                    continue;
                }

                var resolvedStore = await ResolveStoreIdAsync(
                        ExcelSheetHelper.Get(row, "StoreNameAr", "StoreNameEn", "StoreName"),
                        storeId,
                        canManageAllStores,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (resolvedStore.IsFailure)
                {
                    failed++;
                    errors.Add($"Row {i + 2}: {string.Join("; ", resolvedStore.Errors)}");
                    continue;
                }

                var targetStoreId = resolvedStore.Value!.Value;

                var nameAr = ExcelSheetHelper.Get(row, "NameAr");
                var nameEn = ExcelSheetHelper.Get(row, "NameEn");
                if (string.IsNullOrWhiteSpace(nameAr) || string.IsNullOrWhiteSpace(nameEn))
                {
                    failed++;
                    errors.Add($"Row {i + 2}: NameAr and NameEn are required.");
                    continue;
                }

                var exists = await _db.ProductCategories.AsNoTracking()
                    .AnyAsync(c => c.StoreId == targetStoreId && (c.NameAr == nameAr || c.NameEn == nameEn), cancellationToken)
                    .ConfigureAwait(false);
                if (exists)
                {
                    skipped++;
                    continue;
                }

                Guid? parentId = null;
                if (isChild)
                {
                    parentId = await _db.ProductCategories.AsNoTracking()
                        .Where(c => c.StoreId == targetStoreId && (c.NameAr == parentName || c.NameEn == parentName))
                        .Select(c => (Guid?)c.Id)
                        .FirstOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);
                    if (parentId is null)
                    {
                        failed++;
                        errors.Add($"Row {i + 2}: parent category '{parentName}' was not found.");
                        continue;
                    }
                }

                var result = await _categoryService.CreateAsync(
                        targetStoreId,
                        new CreateCategoryRequest
                        {
                            NameAr = nameAr,
                            NameEn = nameEn,
                            ParentCategoryId = parentId,
                            IsActive = ExcelSheetHelper.ParseBool(ExcelSheetHelper.Get(row, "IsActive")) ?? true
                        },
                        canManageAllStores,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (result.IsFailure)
                {
                    failed++;
                    errors.Add($"Row {i + 2}: {string.Join("; ", result.Errors)}");
                }
                else
                {
                    created++;
                }
            }
        }

        return Result<DataImportResultDto>.Ok(new DataImportResultDto
        {
            Created = created,
            Skipped = skipped,
            Failed = failed,
            Errors = errors.Take(40).ToList()
        });
    }

    private async Task<Result<DataImportResultDto>> ImportProductsAsync(
        IReadOnlyList<Dictionary<string, string>> rows,
        Guid? storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var skipped = 0;
        var failed = 0;
        var errors = new List<string>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var resolvedStore = await ResolveStoreIdAsync(
                    ExcelSheetHelper.Get(row, "StoreNameAr", "StoreNameEn", "StoreName"),
                    storeId,
                    canManageAllStores,
                    cancellationToken)
                .ConfigureAwait(false);
            if (resolvedStore.IsFailure)
            {
                failed++;
                errors.Add($"Row {i + 2}: {string.Join("; ", resolvedStore.Errors)}");
                continue;
            }

            var targetStoreId = resolvedStore.Value!.Value;

            var nameAr = ExcelSheetHelper.Get(row, "NameAr");
            var nameEn = ExcelSheetHelper.Get(row, "NameEn");
            var categoryName = ExcelSheetHelper.Get(row, "CategoryNameAr", "CategoryNameEn", "Category");
            if (string.IsNullOrWhiteSpace(nameAr) || string.IsNullOrWhiteSpace(nameEn) || string.IsNullOrWhiteSpace(categoryName))
            {
                failed++;
                errors.Add($"Row {i + 2}: NameAr, NameEn, and CategoryNameAr are required.");
                continue;
            }

            var category = await _db.ProductCategories.AsNoTracking()
                .Where(c => c.StoreId == targetStoreId &&
                            (c.NameAr == categoryName || c.NameEn == categoryName))
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (category is null)
            {
                failed++;
                errors.Add($"Row {i + 2}: category '{categoryName}' was not found.");
                continue;
            }

            var barcode = ExcelSheetHelper.Get(row, "Barcode");
            var exists = await _db.Products.AsNoTracking()
                .AnyAsync(
                    p => p.StoreId == targetStoreId &&
                         (p.NameAr == nameAr ||
                          p.NameEn == nameEn ||
                          (!string.IsNullOrWhiteSpace(barcode) && p.Barcode == barcode)),
                    cancellationToken)
                .ConfigureAwait(false);
            if (exists)
            {
                skipped++;
                continue;
            }

            var price = ExcelSheetHelper.ParseDecimal(ExcelSheetHelper.Get(row, "Price")) ?? 0;
            var stock = ExcelSheetHelper.ParseDecimal(ExcelSheetHelper.Get(row, "StockQuantity")) ?? 0;
            var tracks = ExcelSheetHelper.ParseBool(ExcelSheetHelper.Get(row, "TracksInventory")) ?? true;
            var active = ExcelSheetHelper.ParseBool(ExcelSheetHelper.Get(row, "IsActive")) ?? true;

            var productResult = await _productService.CreateAsync(
                    targetStoreId,
                    new CreateProductRequest
                    {
                        CategoryId = category.Id,
                        NameAr = nameAr,
                        NameEn = nameEn,
                        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                        Sku = NullIfEmpty(ExcelSheetHelper.Get(row, "Sku")),
                        Price = price,
                        StockQuantity = stock,
                        TracksInventory = tracks,
                        IsActive = active
                    },
                    canManageAllStores,
                    cancellationToken)
                .ConfigureAwait(false);

            if (productResult.IsFailure)
            {
                failed++;
                errors.Add($"Row {i + 2}: {string.Join("; ", productResult.Errors)}");
                continue;
            }

            created++;

            var discountPct = ExcelSheetHelper.ParseDecimal(ExcelSheetHelper.Get(row, "DiscountPercent"));
            if (discountPct is > 0 and <= 100)
            {
                var dNameAr = ExcelSheetHelper.Get(row, "DiscountNameAr");
                var dNameEn = ExcelSheetHelper.Get(row, "DiscountNameEn");
                if (string.IsNullOrWhiteSpace(dNameAr))
                {
                    dNameAr = $"خصم {nameAr}";
                }

                if (string.IsNullOrWhiteSpace(dNameEn))
                {
                    dNameEn = $"Discount {nameEn}";
                }

                var discountResult = await _discountService.CreateAsync(
                        targetStoreId,
                        new CreateDiscountRequest
                        {
                            NameAr = dNameAr,
                            NameEn = dNameEn,
                            DiscountPercent = discountPct.Value,
                            AppliesToAllProducts = false,
                            IsActive = true,
                            ProductIds = new[] { productResult.Value!.Id }
                        },
                        canManageAllStores,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (discountResult.IsFailure)
                {
                    errors.Add($"Row {i + 2} (discount): {string.Join("; ", discountResult.Errors)}");
                }
            }
        }

        return Result<DataImportResultDto>.Ok(new DataImportResultDto
        {
            Created = created,
            Skipped = skipped,
            Failed = failed,
            Errors = errors.Take(40).ToList()
        });
    }

    private async Task<Result<Guid?>> ResolveStoreIdAsync(
        string storeName,
        Guid? fallbackStoreId,
        bool canManageAllStores,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(storeName))
        {
            var id = await _db.Stores.AsNoTracking()
                .Where(s => s.NameAr == storeName || s.NameEn == storeName)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (id is null)
            {
                return Result<Guid?>.Fail($"Store '{storeName}' was not found.");
            }

            var access = await StoreAccessHelper.EnsureStoreAccessAsync(
                    _db, id.Value, canManageAllStores, _currentUser.UserId, cancellationToken)
                .ConfigureAwait(false);
            if (access.IsFailure)
            {
                return Result<Guid?>.Fail(access.Errors, access.FailureCode);
            }

            return Result<Guid?>.Ok(id);
        }

        if (fallbackStoreId is null || fallbackStoreId == Guid.Empty)
        {
            return Result<Guid?>.Fail("Select a store or set StoreNameAr in the row.");
        }

        var fbAccess = await StoreAccessHelper.EnsureStoreAccessAsync(
                _db, fallbackStoreId.Value, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (fbAccess.IsFailure)
        {
            return Result<Guid?>.Fail(fbAccess.Errors, fbAccess.FailureCode);
        }

        return Result<Guid?>.Ok(fallbackStoreId);
    }

    private async Task<Result<DataImportResultDto>> ImportStoresAsync(
        IReadOnlyList<Dictionary<string, string>> rows,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var skipped = 0;
        var failed = 0;
        var errors = new List<string>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var nameAr = ExcelSheetHelper.Get(row, "NameAr");
            var nameEn = ExcelSheetHelper.Get(row, "NameEn");
            if (string.IsNullOrWhiteSpace(nameAr) || string.IsNullOrWhiteSpace(nameEn))
            {
                failed++;
                errors.Add($"Row {i + 2}: NameAr and NameEn are required.");
                continue;
            }

            var exists = await _db.Stores.AsNoTracking()
                .AnyAsync(s => s.NameAr == nameAr || s.NameEn == nameEn, cancellationToken)
                .ConfigureAwait(false);
            if (exists)
            {
                skipped++;
                continue;
            }

            if (!TryParseStoreType(ExcelSheetHelper.Get(row, "StoreType"), out var storeType))
            {
                storeType = StoreType.Other;
            }

            var result = await _storeService.CreateAsync(
                    new CreateStoreRequest
                    {
                        NameAr = nameAr,
                        NameEn = nameEn,
                        DescriptionAr = NullIfEmpty(ExcelSheetHelper.Get(row, "DescriptionAr")),
                        DescriptionEn = NullIfEmpty(ExcelSheetHelper.Get(row, "DescriptionEn")),
                        StoreType = storeType
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                failed++;
                errors.Add($"Row {i + 2}: {string.Join("; ", result.Errors)}");
            }
            else
            {
                created++;
            }
        }

        return Result<DataImportResultDto>.Ok(new DataImportResultDto
        {
            Created = created,
            Skipped = skipped,
            Failed = failed,
            Errors = errors.Take(40).ToList()
        });
    }

    private async Task<Result<DataImportResultDto>> ImportUsersAsync(
        IReadOnlyList<Dictionary<string, string>> rows,
        CancellationToken cancellationToken)
    {
        var roles = await _db.Roles.AsNoTracking()
            .Select(r => new { r.Id, r.NameAr, r.NameEn })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var created = 0;
        var skipped = 0;
        var failed = 0;
        var errors = new List<string>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var userName = ExcelSheetHelper.Get(row, "UserName");
            var email = ExcelSheetHelper.Get(row, "Email");
            var password = ExcelSheetHelper.Get(row, "Password");
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                failed++;
                errors.Add($"Row {i + 2}: UserName, Email, and Password are required.");
                continue;
            }

            var exists = await _db.Users.AsNoTracking()
                .AnyAsync(u => u.UserName == userName || u.Email == email, cancellationToken)
                .ConfigureAwait(false);
            if (exists)
            {
                skipped++;
                continue;
            }

            var roleNames = ExcelSheetHelper.Get(row, "Roles", "Role")
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var roleIds = new List<Guid>();
            foreach (var rn in roleNames)
            {
                var role = roles.FirstOrDefault(r =>
                    string.Equals(r.NameEn, rn, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.NameAr, rn, StringComparison.OrdinalIgnoreCase));
                if (role is null)
                {
                    failed++;
                    errors.Add($"Row {i + 2}: role '{rn}' was not found.");
                    roleIds.Clear();
                    break;
                }

                roleIds.Add(role.Id);
            }

            if (roleNames.Length > 0 && roleIds.Count == 0)
            {
                continue;
            }

            var result = await _userService.CreateAsync(
                    new CreateUserRequest
                    {
                        UserName = userName,
                        NameAr = NullIfEmpty(ExcelSheetHelper.Get(row, "NameAr")),
                        NameEn = NullIfEmpty(ExcelSheetHelper.Get(row, "NameEn")),
                        Email = email,
                        Password = password,
                        RoleIds = roleIds
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                failed++;
                errors.Add($"Row {i + 2}: {string.Join("; ", result.Errors)}");
            }
            else
            {
                created++;
            }
        }

        return Result<DataImportResultDto>.Ok(new DataImportResultDto
        {
            Created = created,
            Skipped = skipped,
            Failed = failed,
            Errors = errors.Take(40).ToList()
        });
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryParseStoreType(string raw, out StoreType type)
    {
        type = StoreType.Other;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (Enum.TryParse(raw.Trim(), ignoreCase: true, out type))
        {
            return true;
        }

        type = raw.Trim().ToLowerInvariant() switch
        {
            "كافيه" or "cafe" => StoreType.Cafe,
            "مطعم" or "restaurant" => StoreType.Restaurant,
            "ملابس" or "clothing" => StoreType.Clothing,
            "تجميل" or "cosmetics" => StoreType.Cosmetics,
            "مكسرات" or "nuts" => StoreType.Nuts,
            _ => StoreType.Other
        };
        return true;
    }
}
