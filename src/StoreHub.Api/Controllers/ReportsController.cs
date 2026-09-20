using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreHub.Api.Extensions;
using StoreHub.Application.Features.Identity;
using StoreHub.Application.Features.Reports.DTOs;
using StoreHub.Application.Features.Reports.Interfaces;
using StoreHub.Shared.Identity;

namespace StoreHub.Api.Controllers;

[ApiController]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IReportExcelExportService _reportExcelExportService;

    public ReportsController(IReportService reportService, IReportExcelExportService reportExcelExportService)
    {
        _reportService = reportService;
        _reportExcelExportService = reportExcelExportService;
    }

    [HttpGet("api/stores/{storeId:guid}/reports/summary")]
    public async Task<IActionResult> GetStoreSummary(
        Guid storeId,
        [FromQuery] ReportRangeRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, PermissionCodes.ReportView, PermissionCodes.StoreManage) is { } denied)
        {
            return denied;
        }

        var result = await _reportService.GetStoreSummaryAsync(storeId, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("api/reports/admin-summary")]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    public async Task<IActionResult> GetAdminSummary(
        [FromQuery] ReportRangeRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _reportService.GetAdminSummaryAsync(request, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("api/stores/{storeId:guid}/reports/summary/excel")]
    public async Task<IActionResult> ExportStoreSummaryExcel(
        Guid storeId,
        [FromQuery] ReportRangeRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, PermissionCodes.ReportView, PermissionCodes.StoreManage) is { } denied)
        {
            return denied;
        }

        var result = await _reportExcelExportService.ExportStoreSummaryAsync(
            storeId, request, User.HasStoreManage(), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToFailureActionResult(this, traceId);
        }

        return File(
            result.Value!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"store-report-{storeId:N}.xlsx");
    }

    [HttpGet("api/reports/admin-summary/excel")]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    public async Task<IActionResult> ExportAdminSummaryExcel(
        [FromQuery] ReportRangeRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _reportExcelExportService.ExportAdminSummaryAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToFailureActionResult(this, traceId);
        }

        return File(
            result.Value!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "admin-report.xlsx");
    }
}
