using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StoreHub.Application.Common;
using StoreHub.Application.Features.Identity;
using StoreHub.Application.Features.Identity.DTOs;
using StoreHub.Application.Features.Identity.Interfaces;
using StoreHub.Domain.Identity;
using StoreHub.Persistence;
using StoreHub.Shared.Api;
using StoreHub.Shared.Constants;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Identity.Services;

public sealed class RoleService : IRoleService
{
    private readonly StoreHubDbContext _db;
    private readonly IValidator<CreateRoleRequest> _createValidator;
    private readonly IValidator<UpdateRoleRequest> _updateValidator;

    public RoleService(
        StoreHubDbContext db,
        IValidator<CreateRoleRequest> createValidator,
        IValidator<UpdateRoleRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PagedResult<RoleListItemDto>>> GetPagedAsync(
        RoleFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var page = filter.Page <= 0 ? PaginationConstants.DefaultPage : filter.Page;
        var pageSize = filter.PageSize <= 0 ? PaginationConstants.DefaultPageSize : filter.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = _db.Roles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(r =>
                r.NameAr.Contains(term) ||
                r.NameEn.Contains(term) ||
                (r.DescriptionAr != null && r.DescriptionAr.Contains(term)) ||
                (r.DescriptionEn != null && r.DescriptionEn.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(r => r.IsSystemRole)
            .ThenBy(r => r.NameEn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                NameAr = r.NameAr,
                NameEn = r.NameEn,
                DescriptionAr = r.DescriptionAr,
                DescriptionEn = r.DescriptionEn,
                IsSystemRole = r.IsSystemRole,
                PermissionCount = r.RolePermissions.Count,
                UserCount = r.UserRoles.Count
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<RoleListItemDto>>.Ok(new PagedResult<RoleListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<IReadOnlyList<RoleListItemDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Roles.AsNoTracking()
            .OrderByDescending(r => r.IsSystemRole)
            .ThenBy(r => r.NameEn)
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                NameAr = r.NameAr,
                NameEn = r.NameEn,
                DescriptionAr = r.DescriptionAr,
                DescriptionEn = r.DescriptionEn,
                IsSystemRole = r.IsSystemRole,
                PermissionCount = r.RolePermissions.Count,
                UserCount = r.UserRoles.Count
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<RoleListItemDto>>.Ok(list);
    }

    public async Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await MapRoleDtoAsync(id, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return Result<RoleDto>.Fail("The role was not found.", IdentityErrors.RoleNotFound);
        }

        return Result<RoleDto>.Ok(role);
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<RoleDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var nameEn = NormalizeName(request.NameEn);
        var nameAr = NormalizeName(request.NameAr);

        if (await _db.Roles.AsNoTracking().AnyAsync(r => r.NameEn == nameEn, cancellationToken).ConfigureAwait(false))
        {
            return Result<RoleDto>.Fail("A role with this English name already exists.", IdentityErrors.DuplicateRoleName);
        }

        var permissionIds = request.PermissionIds.Distinct().ToList();
        var permissionCheck = await ValidatePermissionIdsAsync(permissionIds, cancellationToken).ConfigureAwait(false);
        if (permissionCheck.IsFailure)
        {
            return Result<RoleDto>.Fail(permissionCheck.Errors, permissionCheck.FailureCode);
        }

        var role = new Role
        {
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = NormalizeOptional(request.DescriptionAr),
            DescriptionEn = NormalizeOptional(request.DescriptionEn),
            IsSystemRole = false
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var permissionId in permissionIds)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }

        if (permissionIds.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<RoleDto>.Ok((await MapRoleDtoAsync(role.Id, cancellationToken).ConfigureAwait(false))!);
    }

    public async Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<RoleDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return Result<RoleDto>.Fail("The role was not found.", IdentityErrors.RoleNotFound);
        }

        var nameEn = NormalizeName(request.NameEn);
        var nameAr = NormalizeName(request.NameAr);

        if (role.IsSystemRole &&
            string.Equals(role.NameEn, SystemRoles.Admin, StringComparison.Ordinal) &&
            !string.Equals(role.NameEn, nameEn, StringComparison.Ordinal))
        {
            return Result<RoleDto>.Fail(
                "The English name of the Admin role cannot be changed.",
                IdentityErrors.SystemRoleNameImmutable);
        }

        if (await _db.Roles.AsNoTracking()
                .AnyAsync(r => r.NameEn == nameEn && r.Id != id, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result<RoleDto>.Fail("A role with this English name already exists.", IdentityErrors.DuplicateRoleName);
        }

        role.NameAr = nameAr;
        role.NameEn = nameEn;
        role.DescriptionAr = NormalizeOptional(request.DescriptionAr);
        role.DescriptionEn = NormalizeOptional(request.DescriptionEn);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<RoleDto>.Ok((await MapRoleDtoAsync(id, cancellationToken).ConfigureAwait(false))!);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return Result.Fail("The role was not found.", IdentityErrors.RoleNotFound);
        }

        if (string.Equals(role.NameEn, SystemRoles.Admin, StringComparison.Ordinal))
        {
            return Result.Fail("The Admin role cannot be deleted.", IdentityErrors.SystemRoleCannotDelete);
        }

        var links = await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.RolePermissions.RemoveRange(links);

        var userLinks = await _db.UserRoles.Where(ur => ur.RoleId == id).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.UserRoles.RemoveRange(userLinks);

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    public async Task<Result<RoleDto>> SetPermissionsAsync(
        Guid id,
        SetRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var roleExists = await _db.Roles.AsNoTracking().AnyAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (!roleExists)
        {
            return Result<RoleDto>.Fail("The role was not found.", IdentityErrors.RoleNotFound);
        }

        var permissionIds = request.PermissionIds.Distinct().ToList();
        var permissionCheck = await ValidatePermissionIdsAsync(permissionIds, cancellationToken).ConfigureAwait(false);
        if (permissionCheck.IsFailure)
        {
            return Result<RoleDto>.Fail(permissionCheck.Errors, permissionCheck.FailureCode);
        }

        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.RolePermissions.RemoveRange(existing);

        foreach (var permissionId in permissionIds)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = permissionId });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<RoleDto>.Ok((await MapRoleDtoAsync(id, cancellationToken).ConfigureAwait(false))!);
    }

    private async Task<Result> ValidatePermissionIdsAsync(
        IReadOnlyList<Guid> permissionIds,
        CancellationToken cancellationToken)
    {
        if (permissionIds.Count == 0)
        {
            return Result.Ok();
        }

        var found = await _db.Permissions.AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        if (found != permissionIds.Count)
        {
            return Result.Fail("One or more permissions were not found.", IdentityErrors.PermissionNotFound);
        }

        return Result.Ok();
    }

    private async Task<RoleDto?> MapRoleDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await _db.Roles.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.NameAr,
                r.NameEn,
                r.DescriptionAr,
                r.DescriptionEn,
                r.IsSystemRole,
                PermissionIds = r.RolePermissions.Select(rp => rp.PermissionId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (role is null)
        {
            return null;
        }

        return new RoleDto
        {
            Id = role.Id,
            NameAr = role.NameAr,
            NameEn = role.NameEn,
            DescriptionAr = role.DescriptionAr,
            DescriptionEn = role.DescriptionEn,
            IsSystemRole = role.IsSystemRole,
            PermissionIds = role.PermissionIds
        };
    }

    private static string NormalizeName(string value) => value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
