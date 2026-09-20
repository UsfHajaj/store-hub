using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreHub.Api.Extensions;
using StoreHub.Application.Features.Identity;
using StoreHub.Application.Features.Stores.DTOs;
using StoreHub.Application.Features.Stores.Interfaces;
using StoreHub.Shared.Api;
using StoreHub.Shared.Identity;

namespace StoreHub.Api.Controllers;

[ApiController]
[Route("api/stores")]
[Authorize]
public sealed class StoresController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoresController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StoreListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var canManage = User.HasClaim(StoreHubClaimTypes.Permission, PermissionCodes.StoreManage);
        var result = await _storeService.GetMineAsync(canManage, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("paged")]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StoreListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] StoreFilterRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _storeService.GetPagedAsync(request, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StoreListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _storeService.GetAllAsync(cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _storeService.GetByIdAsync(id, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{id:guid}/invoice-settings")]
    public async Task<IActionResult> GetInvoiceSettings(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, PermissionCodes.StoreManage, PermissionCodes.StoreView, PermissionCodes.PosInventoryManage) is { } denied)
        {
            return denied;
        }

        var result = await _storeService.GetByIdForMemberAsync(id, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _storeService.CreateAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id },
                ApiResponse<StoreDto>.FromSuccess(result.Value, traceId));
        }

        return result.ToFailureActionResult(this, traceId);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateStoreRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _storeService.UpdateAsync(id, request, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _storeService.ActivateAsync(id, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _storeService.DeactivateAsync(id, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{id:guid}/members")]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _storeService.GetMembersAsync(id, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{id:guid}/members")]
    [Authorize(Policy = PermissionCodes.StoreManage)]
    public async Task<IActionResult> SetMembers(
        Guid id,
        [FromBody] SetStoreMembersRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _storeService.SetMembersAsync(id, request, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{id:guid}/invoice-settings")]
    public async Task<IActionResult> UpdateInvoiceSettings(
        Guid id,
        [FromBody] UpdateInvoiceSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, PermissionCodes.StoreManage, PermissionCodes.StoreView, PermissionCodes.PosInventoryManage) is { } denied)
        {
            return denied;
        }

        var result = await _storeService.UpdateInvoiceSettingsAsync(id, request, User.HasStoreManage(), cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{id:guid}/logo")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (this.RequireAnyPermission(traceId, PermissionCodes.StoreManage, PermissionCodes.StoreView, PermissionCodes.PosInventoryManage) is { } denied)
        {
            return denied;
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<StoreLogoUploadDto>.FromFailure(
                new[] { "Please choose an image file." },
                traceId));
        }

        if (file.Length > 5_000_000)
        {
            return BadRequest(ApiResponse<StoreLogoUploadDto>.FromFailure(
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
            return BadRequest(ApiResponse<StoreLogoUploadDto>.FromFailure(
                new[] { "Only JPG, PNG, WEBP, or GIF images are allowed." },
                traceId));
        }

        var access = await _storeService.GetByIdAsync(id, cancellationToken);
        if (access.IsFailure)
        {
            // still enforce membership via invoice update with logo only
        }

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "stores", id.ToString("N"));
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.CreateVersion7():N}{ext}";
        var physicalPath = Path.Combine(folder, fileName);
        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        var url = $"/uploads/stores/{id:N}/{fileName}";
        var update = await _storeService.UpdateInvoiceSettingsAsync(
            id,
            new UpdateInvoiceSettingsRequest { LogoUrl = url },
            User.HasStoreManage(),
            cancellationToken);
        if (update.IsFailure)
        {
            return update.ToFailureActionResult(this, traceId);
        }

        return Ok(ApiResponse<StoreLogoUploadDto>.FromSuccess(new StoreLogoUploadDto { ImageUrl = url }, traceId));
    }
}
