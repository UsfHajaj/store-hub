using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StoreHub.Application.Features.Identity;
using StoreHub.Application.Features.Identity.Interfaces;
using StoreHub.Domain.Identity;
using StoreHub.Persistence;
using StoreHub.Shared.Options;

namespace StoreHub.Application.Features.Identity.Services;

public sealed class IdentityDatabaseSeeder : IIdentityDatabaseSeeder
{
    private readonly StoreHubDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IdentitySeedOptions _seedOptions;

    public IdentityDatabaseSeeder(
        StoreHubDbContext db,
        IPermissionService permissionService,
        IPasswordHasher passwordHasher,
        IOptions<IdentitySeedOptions> seedOptions)
    {
        _db = db;
        _permissionService = permissionService;
        _passwordHasher = passwordHasher;
        _seedOptions = seedOptions.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _permissionService.SeedDefaultPermissionsAsync(cancellationToken).ConfigureAwait(false);

        await EnsureSystemRoleAsync(SystemRoles.Admin, "مدير النظام", "Full system access", cancellationToken).ConfigureAwait(false);
        await EnsureSystemRoleAsync(SystemRoles.Manager, "المدير", "Store manager", cancellationToken).ConfigureAwait(false);
        await EnsureSystemRoleAsync(SystemRoles.Cashier, "كاشير", "Cashier — checkout only by default", cancellationToken).ConfigureAwait(false);

        var adminRole = await _db.Roles.AsNoTracking()
            .FirstAsync(r => r.NameEn == SystemRoles.Admin, cancellationToken)
            .ConfigureAwait(false);

        var allPermissionIds = await _db.Permissions.AsNoTracking().Select(p => p.Id).ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingLinks = await _db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var missing = allPermissionIds.Where(pid => !existingLinks.Contains(pid)).ToList();
        foreach (var pid in missing)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = pid });
        }

        if (missing.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await EnsureCashierDefaultPermissionsAsync(cancellationToken).ConfigureAwait(false);
        await EnsureManagerDefaultPosPermissionsAsync(cancellationToken).ConfigureAwait(false);

        if (!_seedOptions.BootstrapAdmin)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_seedOptions.AdminPassword))
        {
            throw new InvalidOperationException(
                "IdentitySeed:BootstrapAdmin is true but AdminPassword is empty. Set a strong password or disable BootstrapAdmin.");
        }

        var adminEmail = _seedOptions.AdminEmail.Trim().ToLowerInvariant();
        var adminUserName = _seedOptions.AdminUserName.Trim().ToLowerInvariant();

        if (await _db.Users.AsNoTracking().AnyAsync(u => u.Email == adminEmail, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var user = new User
        {
            UserName = adminUserName,
            Email = adminEmail,
            PasswordHash = _passwordHasher.Hash(_seedOptions.AdminPassword),
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureCashierDefaultPermissionsAsync(CancellationToken cancellationToken)
    {
        var cashier = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.NameEn == SystemRoles.Cashier, cancellationToken)
            .ConfigureAwait(false);
        if (cashier is null)
        {
            return;
        }

        var permissionIds = await _db.Permissions.AsNoTracking()
            .Where(p =>
                p.Code == PermissionCodes.PosSaleCreate ||
                p.Code == PermissionCodes.PosCatalogView ||
                p.Code == PermissionCodes.PosSaleView ||
                p.Code == PermissionCodes.PosDiscountApply ||
                p.Code == PermissionCodes.PosReturnCreate ||
                p.Code == PermissionCodes.StoreView)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var linked = await _db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == cashier.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var missing = permissionIds.Where(pid => !linked.Contains(pid)).ToList();
        foreach (var pid in missing)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = cashier.Id, PermissionId = pid });
        }

        if (missing.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnsureManagerDefaultPosPermissionsAsync(CancellationToken cancellationToken)
    {
        var manager = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.NameEn == SystemRoles.Manager, cancellationToken)
            .ConfigureAwait(false);
        if (manager is null)
        {
            return;
        }

        var posCodes = new[]
        {
            PermissionCodes.PosCatalogView,
            PermissionCodes.PosCategoryManage,
            PermissionCodes.PosProductCreate,
            PermissionCodes.PosProductUpdate,
            PermissionCodes.PosDiscountManage,
            PermissionCodes.PosDiscountApply,
            PermissionCodes.PosSaleCreate,
            PermissionCodes.PosSaleView,
            PermissionCodes.PosInventoryManage,
            PermissionCodes.PosStocktakeManage,
            PermissionCodes.PosReturnCreate,
            PermissionCodes.ReportView,
            PermissionCodes.StoreView
        };

        var linkedCodes = await _db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == manager.Id)
            .Join(_db.Permissions.AsNoTracking(), rp => rp.PermissionId, p => p.Id, (rp, p) => p.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var missingCodes = posCodes.Where(c => !linkedCodes.Contains(c)).ToList();
        if (missingCodes.Count == 0)
        {
            return;
        }

        var permissionIds = await _db.Permissions.AsNoTracking()
            .Where(p => missingCodes.Contains(p.Code))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var pid in permissionIds)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = manager.Id, PermissionId = pid });
        }

        if (permissionIds.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnsureSystemRoleAsync(string nameEn, string nameAr, string descriptionEn, CancellationToken cancellationToken)
    {
        if (await _db.Roles.AsNoTracking().AnyAsync(r => r.NameEn == nameEn, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        _db.Roles.Add(new Role
        {
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = descriptionEn,
            DescriptionEn = descriptionEn,
            IsSystemRole = true
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
