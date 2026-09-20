using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreHub.Api.Extensions;
using StoreHub.Application.Features.Identity;
using StoreHub.Application.Features.Inventory.DTOs;
using StoreHub.Application.Features.Inventory.Interfaces;
using StoreHub.Shared.Api;
using StoreHub.Shared.Identity;

namespace StoreHub.Api.Controllers;

[ApiController]
[Route("api/stores/{storeId:guid}/inventory")]
[Authorize]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.PosInventoryManage)]
    public async Task<IActionResult> GetPaged(
        Guid storeId,
        [FromQuery] InventoryFilterRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _inventoryService.GetPagedAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("adjust")]
    [Authorize(Policy = PermissionCodes.PosInventoryManage)]
    public async Task<IActionResult> Adjust(
        Guid storeId,
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _inventoryService.AdjustAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}

[ApiController]
[Route("api/stores/{storeId:guid}/stocktakes")]
[Authorize]
public sealed class StocktakesController : ControllerBase
{
    private readonly IStocktakeService _stocktakeService;

    public StocktakesController(IStocktakeService stocktakeService)
    {
        _stocktakeService = stocktakeService;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.PosStocktakeManage)]
    public async Task<IActionResult> GetList(Guid storeId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _stocktakeService.GetListAsync(storeId, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("paged")]
    [Authorize(Policy = PermissionCodes.PosStocktakeManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StocktakeListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        Guid storeId,
        [FromQuery] StocktakeFilterRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _stocktakeService.GetPagedAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PosStocktakeManage)]
    public async Task<IActionResult> GetById(Guid storeId, Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _stocktakeService.GetByIdAsync(storeId, id, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.PosStocktakeManage)]
    public async Task<IActionResult> Create(
        Guid storeId,
        [FromBody] CreateStocktakeRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _stocktakeService.CreateAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{id:guid}/lines")]
    [Authorize(Policy = PermissionCodes.PosStocktakeManage)]
    public async Task<IActionResult> UpdateLines(
        Guid storeId,
        Guid id,
        [FromBody] UpdateStocktakeLinesRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _stocktakeService.UpdateLinesAsync(storeId, id, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = PermissionCodes.PosStocktakeManage)]
    public async Task<IActionResult> Complete(Guid storeId, Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _stocktakeService.CompleteAsync(storeId, id, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
