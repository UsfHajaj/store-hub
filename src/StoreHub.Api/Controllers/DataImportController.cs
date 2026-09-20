using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreHub.Api.Extensions;
using StoreHub.Application.Features.DataImport.DTOs;
using StoreHub.Application.Features.DataImport.Interfaces;
using StoreHub.Application.Features.Identity;
using StoreHub.Shared.Api;
using StoreHub.Shared.Identity;

namespace StoreHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/data-import")]
public sealed class DataImportController : ControllerBase
{
    private readonly IDataImportService _dataImportService;

    public DataImportController(IDataImportService dataImportService)
    {
        _dataImportService = dataImportService;
    }

    [HttpGet("templates/{kind}")]
    public IActionResult DownloadTemplate(DataImportKind kind, [FromQuery] bool sample = false)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (!CanAccess(kind, out var denied))
        {
            return denied!;
        }

        try
        {
            var bytes = _dataImportService.GetTemplate(kind, sample);
            var label = sample ? "sample" : "template";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{kind.ToString().ToLowerInvariant()}-{label}.xlsx");
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.FromFailure(new[] { ex.Message }, traceId));
        }
    }

    [HttpPost("{kind}")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Import(
        DataImportKind kind,
        IFormFile file,
        [FromQuery] Guid? storeId,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (!CanAccess(kind, out var denied))
        {
            return denied!;
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.FromFailure(new[] { "Please upload an Excel (.xlsx) file." }, traceId));
        }

        await using var stream = file.OpenReadStream();
        var result = await _dataImportService.ImportAsync(
            kind,
            stream,
            storeId,
            User.HasStoreManage(),
            cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    private bool CanAccess(DataImportKind kind, out IActionResult? denied)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        denied = kind switch
        {
            DataImportKind.Stores => this.RequireAnyPermission(traceId, PermissionCodes.StoreManage),
            DataImportKind.Users => this.RequireAnyPermission(traceId, PermissionCodes.UserManage),
            DataImportKind.Categories => this.RequireAnyPermission(
                traceId,
                PermissionCodes.PosCategoryManage,
                PermissionCodes.StoreManage),
            DataImportKind.Products => this.RequireAnyPermission(
                traceId,
                PermissionCodes.PosProductCreate,
                PermissionCodes.StoreManage),
            DataImportKind.All => this.RequireAnyPermission(
                traceId,
                PermissionCodes.StoreManage,
                PermissionCodes.UserManage,
                PermissionCodes.PosCategoryManage,
                PermissionCodes.PosProductCreate),
            _ => this.RequireAnyPermission(traceId, PermissionCodes.StoreManage)
        };
        return denied is null;
    }
}
