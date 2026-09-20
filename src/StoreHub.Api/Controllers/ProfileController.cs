using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreHub.Api.Extensions;
using StoreHub.Application.Features.Identity.DTOs;
using StoreHub.Application.Features.Identity.Interfaces;
using StoreHub.Application.Features.Reports.DTOs;
using StoreHub.Application.Features.Reports.Interfaces;
using StoreHub.Shared.Api;
using StoreHub.Shared.Identity;

namespace StoreHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IReportService _reportService;
    private readonly ICurrentUserService _currentUser;

    public ProfileController(
        IUserService userService,
        IReportService reportService,
        ICurrentUserService currentUser)
    {
        _userService = userService;
        _reportService = reportService;
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (_currentUser.UserId is null)
        {
            return Unauthorized(ApiResponse.FromFailure(new[] { "Not authenticated." }, traceId));
        }

        var result = await _userService.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("performance")]
    [ProducesResponseType(typeof(ApiResponse<MyPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPerformance(
        [FromQuery] Guid storeId,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (storeId == Guid.Empty)
        {
            return BadRequest(ApiResponse.FromFailure(new[] { "storeId is required." }, traceId));
        }

        var result = await _reportService.GetMyPerformanceAsync(
            storeId,
            User.HasStoreManage(),
            cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
