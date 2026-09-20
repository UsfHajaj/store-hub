using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StoreHub.Application.Common;
using StoreHub.Application.Features.Notifications.DTOs;
using StoreHub.Application.Features.Notifications.Interfaces;
using StoreHub.Application.Features.Stores.DTOs;
using StoreHub.Application.Features.Stores.Interfaces;
using StoreHub.Domain.Enums;
using StoreHub.Domain.Stores;
using StoreHub.Persistence;
using StoreHub.Shared.Api;
using StoreHub.Shared.Constants;
using StoreHub.Shared.Identity;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Stores.Services;

public sealed class StoreService : IStoreService
{
    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly IValidator<CreateStoreRequest> _createValidator;
    private readonly IValidator<UpdateStoreRequest> _updateValidator;

    public StoreService(
        StoreHubDbContext db,
        ICurrentUserService currentUser,
        INotificationService notifications,
        IValidator<CreateStoreRequest> createValidator,
        IValidator<UpdateStoreRequest> updateValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PagedResult<StoreListItemDto>>> GetPagedAsync(
        StoreFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var page = filter.Page <= 0 ? PaginationConstants.DefaultPage : filter.Page;
        var pageSize = filter.PageSize <= 0 ? PaginationConstants.DefaultPageSize : filter.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = QueryStoreList();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(s =>
                s.NameAr.Contains(term) ||
                s.NameEn.Contains(term) ||
                (s.DescriptionAr != null && s.DescriptionAr.Contains(term)) ||
                (s.DescriptionEn != null && s.DescriptionEn.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderBy(s => s.NameEn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<StoreListItemDto>>.Ok(new PagedResult<StoreListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<IReadOnlyList<StoreListItemDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await QueryStoreList()
            .OrderBy(s => s.NameEn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<StoreListItemDto>>.Ok(list);
    }

    public async Task<Result<IReadOnlyList<StoreListItemDto>>> GetMineAsync(
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        if (canManageAllStores)
        {
            return await GetAllAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_currentUser.UserId is null)
        {
            return Result<IReadOnlyList<StoreListItemDto>>.Fail(
                "Current user is required.",
                StoreErrors.CurrentUserRequired);
        }

        var userId = _currentUser.UserId.Value;
        var list = await _db.Stores.AsNoTracking()
            .Where(s => s.IsActive && s.Members.Any(m => m.UserId == userId))
            .OrderBy(s => s.NameEn)
            .Select(s => new StoreListItemDto
            {
                Id = s.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                DescriptionAr = s.DescriptionAr,
                DescriptionEn = s.DescriptionEn,
                StoreType = s.StoreType,
                IsActive = s.IsActive,
                MemberCount = s.Members.Count
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<StoreListItemDto>>.Ok(list);
    }

    public async Task<Result<StoreDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var store = await MapStoreDtoAsync(id, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return Result<StoreDto>.Fail("The store was not found.", StoreErrors.StoreNotFound);
        }

        return Result<StoreDto>.Ok(store);
    }

    public async Task<Result<StoreDto>> GetByIdForMemberAsync(
        Guid id,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, id, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<StoreDto>.Fail(access.Errors, access.FailureCode);
        }

        return await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<StoreDto>> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<StoreDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var nameEn = request.NameEn.Trim();
        var nameAr = request.NameAr.Trim();

        if (await _db.Stores.AsNoTracking().AnyAsync(s => s.NameEn == nameEn, cancellationToken).ConfigureAwait(false))
        {
            return Result<StoreDto>.Fail("A store with this English name already exists.", StoreErrors.DuplicateStoreName);
        }

        var memberIds = request.MemberUserIds.Distinct().ToList();
        var memberCheck = await ValidateUserIdsAsync(memberIds, cancellationToken).ConfigureAwait(false);
        if (memberCheck.IsFailure)
        {
            return Result<StoreDto>.Fail(memberCheck.Errors, memberCheck.FailureCode);
        }

        var store = new Store
        {
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = NormalizeOptional(request.DescriptionAr),
            DescriptionEn = NormalizeOptional(request.DescriptionEn),
            StoreType = request.StoreType,
            IsActive = true,
            OwnerUserId = _currentUser.UserId
        };

        _db.Stores.Add(store);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var userId in memberIds)
        {
            _db.StoreMembers.Add(new StoreMember { StoreId = store.Id, UserId = userId });
        }

        if (memberIds.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<StoreDto>.Ok((await MapStoreDtoAsync(store.Id, cancellationToken).ConfigureAwait(false))!);
    }

    public async Task<Result<StoreDto>> UpdateAsync(Guid id, UpdateStoreRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<StoreDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return Result<StoreDto>.Fail("The store was not found.", StoreErrors.StoreNotFound);
        }

        var nameEn = request.NameEn.Trim();
        var nameAr = request.NameAr.Trim();

        if (await _db.Stores.AsNoTracking()
                .AnyAsync(s => s.NameEn == nameEn && s.Id != id, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result<StoreDto>.Fail("A store with this English name already exists.", StoreErrors.DuplicateStoreName);
        }

        store.NameAr = nameAr;
        store.NameEn = nameEn;
        store.DescriptionAr = NormalizeOptional(request.DescriptionAr);
        store.DescriptionEn = NormalizeOptional(request.DescriptionEn);
        store.StoreType = request.StoreType;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<StoreDto>.Ok((await MapStoreDtoAsync(id, cancellationToken).ConfigureAwait(false))!);
    }

    public async Task<Result> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return Result.Fail("The store was not found.", StoreErrors.StoreNotFound);
        }

        store.IsActive = true;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return Result.Fail("The store was not found.", StoreErrors.StoreNotFound);
        }

        store.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<StoreMemberDto>>> GetMembersAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Stores.AsNoTracking().AnyAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false))
        {
            return Result<IReadOnlyList<StoreMemberDto>>.Fail("The store was not found.", StoreErrors.StoreNotFound);
        }

        var members = await MapMembersAsync(id, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<StoreMemberDto>>.Ok(members);
    }

    public async Task<Result<IReadOnlyList<StoreMemberDto>>> SetMembersAsync(
        Guid id,
        SetStoreMembersRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Stores.AsNoTracking().AnyAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false))
        {
            return Result<IReadOnlyList<StoreMemberDto>>.Fail("The store was not found.", StoreErrors.StoreNotFound);
        }

        var userIds = request.UserIds.Distinct().ToList();
        var memberCheck = await ValidateUserIdsAsync(userIds, cancellationToken).ConfigureAwait(false);
        if (memberCheck.IsFailure)
        {
            return Result<IReadOnlyList<StoreMemberDto>>.Fail(memberCheck.Errors, memberCheck.FailureCode);
        }

        var existing = await _db.StoreMembers.Where(m => m.StoreId == id).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var previousIds = existing.Select(m => m.UserId).ToHashSet();
        _db.StoreMembers.RemoveRange(existing);

        foreach (var userId in userIds)
        {
            _db.StoreMembers.Add(new StoreMember { StoreId = id, UserId = userId });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var store = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new { s.NameAr, s.NameEn })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var storeLabel = store is null ? id.ToString() : $"{store.NameAr} / {store.NameEn}";

        foreach (var userId in userIds.Where(uid => !previousIds.Contains(uid)))
        {
            await _notifications.NotifyUserSafeAsync(
                userId,
                new PublishNotificationRequest
                {
                    NotificationType = NotificationType.StoreMembership,
                    Title = "إضافة إلى محل",
                    Message =
                        $"تم إضافتك كعضو في المحل «{storeLabel}».\n" +
                        $"You were added as a member of «{storeLabel}».",
                    RelatedEntityId = id,
                    RelatedEntityType = nameof(Store)
                },
                cancellationToken).ConfigureAwait(false);
        }

        var members = await MapMembersAsync(id, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<StoreMemberDto>>.Ok(members);
    }

    public async Task<Result<StoreDto>> UpdateInvoiceSettingsAsync(
        Guid id,
        UpdateInvoiceSettingsRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, id, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<StoreDto>.Fail(access.Errors, access.FailureCode);
        }

        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return Result<StoreDto>.Fail("The store was not found.", StoreErrors.StoreNotFound);
        }

        store.InvoiceDisplayNameAr = request.InvoiceDisplayNameAr is null
            ? store.InvoiceDisplayNameAr
            : NormalizeOptional(request.InvoiceDisplayNameAr);
        store.InvoiceDisplayNameEn = request.InvoiceDisplayNameEn is null
            ? store.InvoiceDisplayNameEn
            : NormalizeOptional(request.InvoiceDisplayNameEn);
        store.TaxNumber = request.TaxNumber is null
            ? store.TaxNumber
            : NormalizeOptional(request.TaxNumber);
        store.InvoiceFooter = request.InvoiceFooter is null
            ? store.InvoiceFooter
            : NormalizeOptional(request.InvoiceFooter);
        if (request.LogoUrl is not null)
        {
            store.LogoUrl = NormalizeOptional(request.LogoUrl);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<StoreDto>.Ok((await MapStoreDtoAsync(id, cancellationToken).ConfigureAwait(false))!);
    }

    private IQueryable<StoreListItemDto> QueryStoreList() =>
        _db.Stores.AsNoTracking()
            .Select(s => new StoreListItemDto
            {
                Id = s.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                DescriptionAr = s.DescriptionAr,
                DescriptionEn = s.DescriptionEn,
                StoreType = s.StoreType,
                IsActive = s.IsActive,
                MemberCount = s.Members.Count
            });

    private async Task<StoreDto?> MapStoreDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var store = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.NameAr,
                s.NameEn,
                s.DescriptionAr,
                s.DescriptionEn,
                s.StoreType,
                s.IsActive,
                s.OwnerUserId,
                s.InvoiceDisplayNameAr,
                s.InvoiceDisplayNameEn,
                s.LogoUrl,
                s.TaxNumber,
                s.InvoiceFooter,
                MemberUserIds = s.Members.Select(m => m.UserId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (store is null)
        {
            return null;
        }

        return new StoreDto
        {
            Id = store.Id,
            NameAr = store.NameAr,
            NameEn = store.NameEn,
            DescriptionAr = store.DescriptionAr,
            DescriptionEn = store.DescriptionEn,
            StoreType = store.StoreType,
            IsActive = store.IsActive,
            OwnerUserId = store.OwnerUserId,
            InvoiceDisplayNameAr = store.InvoiceDisplayNameAr,
            InvoiceDisplayNameEn = store.InvoiceDisplayNameEn,
            LogoUrl = store.LogoUrl,
            TaxNumber = store.TaxNumber,
            InvoiceFooter = store.InvoiceFooter,
            MemberUserIds = store.MemberUserIds
        };
    }

    private async Task<IReadOnlyList<StoreMemberDto>> MapMembersAsync(Guid storeId, CancellationToken cancellationToken)
    {
        return await (
                from m in _db.StoreMembers.AsNoTracking()
                join u in _db.Users.AsNoTracking() on m.UserId equals u.Id
                where m.StoreId == storeId
                orderby u.UserName
                select new StoreMemberDto
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    NameAr = u.NameAr,
                    NameEn = u.NameEn,
                    Email = u.Email
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Result> ValidateUserIdsAsync(IReadOnlyList<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return Result.Ok();
        }

        var found = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        if (found != userIds.Count)
        {
            return Result.Fail("One or more users were not found.", StoreErrors.UserNotFound);
        }

        return Result.Ok();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
