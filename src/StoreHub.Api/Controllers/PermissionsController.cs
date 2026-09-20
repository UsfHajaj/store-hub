using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreHub.Api.Extensions;
using StoreHub.Application.Features.Identity;
using StoreHub.Application.Features.Identity.DTOs;
using StoreHub.Application.Features.Identity.Interfaces;
using StoreHub.Shared.Api;

namespace StoreHub.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = PermissionCodes.RoleManage)]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _permissionService.GetAllAsync(cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
