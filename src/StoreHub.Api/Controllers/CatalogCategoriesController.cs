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
[Route("api/stores/{storeId:guid}/categories")]
[Authorize]
public sealed class CatalogCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    private static readonly string[] CatalogViewPermissions =
    [
        PermissionCodes.PosCatalogView,
        PermissionCodes.PosCategoryManage,
        PermissionCodes.PosProductCreate,
        PermissionCodes.PosSaleCreate
    ];

    public CatalogCategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(Guid storeId, [FromQuery] bool tree = false, CancellationToken cancellationToken = default)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, CatalogViewPermissions) is { } denied)
        {
            return denied;
        }

        var canManage = User.HasStoreManage();
        if (tree)
        {
            var treeResult = await _categoryService.GetTreeAsync(storeId, canManage, cancellationToken);
            return treeResult.ToApiActionResult(this, traceId);
        }

        var result = await _categoryService.GetListAsync(storeId, canManage, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        Guid storeId,
        [FromQuery] CategoryFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, CatalogViewPermissions) is { } denied)
        {
            return denied;
        }

        var result = await _categoryService.GetPagedAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.PosCategoryManage)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        Guid storeId,
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _categoryService.CreateAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        if (result.IsSuccess)
        {
            return Created(string.Empty, ApiResponse<CategoryDto>.FromSuccess(result.Value!, traceId));
        }

        return result.ToFailureActionResult(this, traceId);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PosCategoryManage)]
    public async Task<IActionResult> Update(
        Guid storeId,
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _categoryService.UpdateAsync(storeId, id, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PosCategoryManage)]
    public async Task<IActionResult> Delete(Guid storeId, Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _categoryService.DeleteAsync(storeId, id, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
