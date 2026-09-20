using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreHub.Api.Extensions;
using StoreHub.Application.Features.AuditLogs.DTOs;
using StoreHub.Application.Features.AuditLogs.Interfaces;
using StoreHub.Application.Features.Identity;
using StoreHub.Shared.Api;

namespace StoreHub.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = PermissionCodes.AuditLogView)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogQueryService _auditLogs;

    public AuditLogsController(IAuditLogQueryService auditLogs)
    {
        _auditLogs = auditLogs;
    }

    /// <summary>
    /// Paged list with optional filters: <paramref name="request"/>.Search,
    /// FromOccurredOnUtc / ToOccurredOnUtc (UTC, inclusive), EntityType, Action, PerformedByUserId.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] AuditLogFilterRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _auditLogs.GetPagedAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PagedResult<AuditLogListItemDto>>.FromSuccess(result.Value!, traceId));
        }

        return result.ToFailureActionResult(this, traceId);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuditLogListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _auditLogs.GetByIdAsync(id, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
