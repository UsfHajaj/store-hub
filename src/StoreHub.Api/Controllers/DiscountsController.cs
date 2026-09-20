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
[Route("api/stores/{storeId:guid}/discounts")]
[Authorize]
public sealed class DiscountsController : ControllerBase
{
    private readonly IDiscountService _discountService;

    private static readonly string[] DiscountViewPermissions =
    [
        PermissionCodes.PosCatalogView,
        PermissionCodes.PosDiscountManage,
        PermissionCodes.PosDiscountApply,
        PermissionCodes.PosSaleCreate,
        PermissionCodes.StoreManage
    ];

    public DiscountsController(IDiscountService discountService)
    {
        _discountService = discountService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid storeId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, DiscountViewPermissions) is { } denied)
        {
            return denied;
        }

        var result = await _discountService.GetAllAsync(storeId, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DiscountDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        Guid storeId,
        [FromQuery] DiscountFilterRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, DiscountViewPermissions) is { } denied)
        {
            return denied;
        }

        var result = await _discountService.GetPagedAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid storeId, Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, DiscountViewPermissions) is { } denied)
        {
            return denied;
        }

        var result = await _discountService.GetByIdAsync(storeId, id, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.PosDiscountManage)]
    public async Task<IActionResult> Create(
        Guid storeId,
        [FromBody] CreateDiscountRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _discountService.CreateAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        if (result.IsSuccess)
        {
            return Created(string.Empty, ApiResponse<DiscountDto>.FromSuccess(result.Value!, traceId));
        }

        return result.ToFailureActionResult(this, traceId);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PosDiscountManage)]
    public async Task<IActionResult> Update(
        Guid storeId,
        Guid id,
        [FromBody] UpdateDiscountRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _discountService.UpdateAsync(storeId, id, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PosDiscountManage)]
    public async Task<IActionResult> Delete(Guid storeId, Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _discountService.DeleteAsync(storeId, id, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
