using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreHub.Api.Extensions;
using StoreHub.Application.Features.Catalog.DTOs;
using StoreHub.Application.Features.Catalog.Interfaces;
using StoreHub.Application.Features.Identity;
using StoreHub.Shared.Api;

namespace StoreHub.Api.Controllers;

[ApiController]
[Route("api/stores/{storeId:guid}/products")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    private static readonly string[] CatalogViewPermissions =
    [
        PermissionCodes.PosCatalogView,
        PermissionCodes.PosCategoryManage,
        PermissionCodes.PosProductCreate,
        PermissionCodes.PosProductUpdate,
        PermissionCodes.PosSaleCreate
    ];

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        Guid storeId,
        [FromQuery] ProductFilterRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, CatalogViewPermissions) is { } denied)
        {
            return denied;
        }

        var result = await _productService.GetPagedAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid storeId, Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, CatalogViewPermissions) is { } denied)
        {
            return denied;
        }

        var result = await _productService.GetByIdAsync(storeId, id, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.PosProductCreate)]
    public async Task<IActionResult> Create(
        Guid storeId,
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _productService.CreateAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        if (result.IsSuccess)
        {
            return Created(string.Empty, ApiResponse<ProductDto>.FromSuccess(result.Value!, traceId));
        }

        return result.ToFailureActionResult(this, traceId);
    }

    [HttpPost("upload-image")]
    [RequestSizeLimit(5_000_000)]
    [ProducesResponseType(typeof(ApiResponse<ProductImageUploadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadImage(
        Guid storeId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, [PermissionCodes.PosProductCreate, PermissionCodes.PosProductUpdate]) is { } denied)
        {
            return denied;
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<ProductImageUploadDto>.FromFailure(
                new[] { "Please choose an image file." },
                traceId));
        }

        if (file.Length > 5_000_000)
        {
            return BadRequest(ApiResponse<ProductImageUploadDto>.FromFailure(
                new[] { "Image must be 5 MB or smaller." },
                traceId));
        }

        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/jpg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif"
        };

        if (!allowed.TryGetValue(contentType, out var ext))
        {
            return BadRequest(ApiResponse<ProductImageUploadDto>.FromFailure(
                new[] { "Only JPG, PNG, WEBP, or GIF images are allowed." },
                traceId));
        }

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "products", storeId.ToString("N"));
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.CreateVersion7():N}{ext}";
        var physicalPath = Path.Combine(folder, fileName);
        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        var url = $"/uploads/products/{storeId:N}/{fileName}";
        return Ok(ApiResponse<ProductImageUploadDto>.FromSuccess(new ProductImageUploadDto { ImageUrl = url }, traceId));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PosProductUpdate)]
    public async Task<IActionResult> Update(
        Guid storeId,
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _productService.UpdateAsync(storeId, id, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PosProductUpdate)]
    public async Task<IActionResult> Delete(Guid storeId, Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _productService.DeleteAsync(storeId, id, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
