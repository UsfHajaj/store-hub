using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreHub.Api.Extensions;
using StoreHub.Application.Features.Identity;
using StoreHub.Application.Features.Sales.DTOs;
using StoreHub.Application.Features.Sales.Interfaces;
using StoreHub.Shared.Api;
using StoreHub.Shared.Identity;

namespace StoreHub.Api.Controllers;

[ApiController]
[Route("api/stores/{storeId:guid}/sales")]
[Authorize]
public sealed class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly IInvoicePdfService _invoicePdfService;

    public SalesController(ISaleService saleService, IInvoicePdfService invoicePdfService)
    {
        _saleService = saleService;
        _invoicePdfService = invoicePdfService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SaleListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        Guid storeId,
        [FromQuery] SaleFilterRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, PermissionCodes.PosSaleView, PermissionCodes.PosSaleCreate, PermissionCodes.StoreManage) is { } denied)
        {
            return denied;
        }

        var result = await _saleService.GetPagedAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid storeId, Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, PermissionCodes.PosSaleView, PermissionCodes.PosSaleCreate, PermissionCodes.StoreManage) is { } denied)
        {
            return denied;
        }

        var result = await _saleService.GetByIdAsync(storeId, id, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        Guid storeId,
        [FromBody] CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, PermissionCodes.PosSaleCreate, PermissionCodes.StoreManage) is { } denied)
        {
            return denied;
        }

        var result = await _saleService.CreateAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { storeId, id = result.Value!.Id },
                ApiResponse<SaleDto>.FromSuccess(result.Value, traceId));
        }

        return result.ToFailureActionResult(this, traceId);
    }

    [HttpGet("{id:guid}/invoice.pdf")]
    public async Task<IActionResult> GetInvoicePdf(Guid storeId, Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, PermissionCodes.PosSaleView, PermissionCodes.PosSaleCreate, PermissionCodes.StoreManage) is { } denied)
        {
            return denied;
        }

        var result = await _invoicePdfService.GenerateSaleInvoicePdfAsync(storeId, id, User.HasStoreManage(), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToFailureActionResult(this, traceId);
        }

        return File(result.Value!, "application/pdf", $"invoice-{id:N}.pdf");
    }

    [HttpPost("{id:guid}/returns")]
    [Authorize(Policy = PermissionCodes.PosReturnCreate)]
    public async Task<IActionResult> CreateReturn(
        Guid storeId,
        Guid id,
        [FromBody] CreateSaleReturnRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _saleService.CreateReturnAsync(storeId, id, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
