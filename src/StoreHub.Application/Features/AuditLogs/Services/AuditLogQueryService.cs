using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StoreHub.Application.Common;
using StoreHub.Application.Features.AuditLogs.DTOs;
using StoreHub.Application.Features.AuditLogs.Interfaces;
using StoreHub.Persistence;
using StoreHub.Shared.Api;
using StoreHub.Shared.Constants;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.AuditLogs.Services;

public sealed class AuditLogQueryService : IAuditLogQueryService
{
    private readonly StoreHubDbContext _db;
    private readonly IValidator<AuditLogFilterRequest> _filterValidator;

    public AuditLogQueryService(StoreHubDbContext db, IValidator<AuditLogFilterRequest> filterValidator)
    {
        _db = db;
        _filterValidator = filterValidator;
    }

    public async Task<Result<PagedResult<AuditLogListItemDto>>> GetPagedAsync(
        AuditLogFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _filterValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<PagedResult<AuditLogListItemDto>>.Fail(
                validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = _db.AuditLogs.AsNoTracking()
            .Where(l => !l.EntityType.Contains("/api/notifications/my/unread-count"))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            var t = request.EntityType.Trim();
            query = query.Where(l => l.EntityType == t);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var a = request.Action.Trim();
            query = query.Where(l => l.Action == a);
        }

        if (request.PerformedByUserId is { } userId && userId != Guid.Empty)
        {
            query = query.Where(l => l.PerformedByUserId == userId);
        }

        if (request.FromOccurredOnUtc is { } fromUtc)
        {
            query = query.Where(l => l.OccurredOnUtc >= fromUtc);
        }

        if (request.ToOccurredOnUtc is { } toUtc)
        {
            query = query.Where(l => l.OccurredOnUtc <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(l =>
                l.EntityType.Contains(term) ||
                l.Action.Contains(term) ||
                (l.DetailsJson != null && l.DetailsJson.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await (
                from l in query
                join u in _db.Users.AsNoTracking() on l.PerformedByUserId equals u.Id into userJoin
                from u in userJoin.DefaultIfEmpty()
                orderby l.OccurredOnUtc descending
                select new AuditLogListItemDto
                {
                    Id = l.Id,
                    Action = l.Action,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    PerformedByUserId = l.PerformedByUserId,
                    PerformedByUserName = u != null ? u.UserName : null,
                    PerformedByNameAr = u != null ? u.NameAr : null,
                    PerformedByNameEn = u != null ? u.NameEn : null,
                    OccurredOnUtc = l.OccurredOnUtc,
                    DetailsJson = l.DetailsJson
                })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await EnrichEntityUserNamesAsync(items, cancellationToken).ConfigureAwait(false);

        return Result<PagedResult<AuditLogListItemDto>>.Ok(new PagedResult<AuditLogListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<AuditLogListItemDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await (
                from l in _db.AuditLogs.AsNoTracking()
                join u in _db.Users.AsNoTracking() on l.PerformedByUserId equals u.Id into userJoin
                from u in userJoin.DefaultIfEmpty()
                where l.Id == id
                select new AuditLogListItemDto
                {
                    Id = l.Id,
                    Action = l.Action,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    PerformedByUserId = l.PerformedByUserId,
                    PerformedByUserName = u != null ? u.UserName : null,
                    PerformedByNameAr = u != null ? u.NameAr : null,
                    PerformedByNameEn = u != null ? u.NameEn : null,
                    OccurredOnUtc = l.OccurredOnUtc,
                    DetailsJson = l.DetailsJson
                })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            return Result<AuditLogListItemDto>.Fail("The audit log entry was not found.", AuditLogErrors.NotFound);
        }

        await EnrichEntityUserNamesAsync([row], cancellationToken).ConfigureAwait(false);

        return Result<AuditLogListItemDto>.Ok(row);
    }

    private async Task EnrichEntityUserNamesAsync(
        IList<AuditLogListItemDto> items,
        CancellationToken cancellationToken)
    {
        var entityUserIds = items
            .Where(i => string.Equals(i.EntityType, "User", StringComparison.Ordinal) && i.EntityId is { } eid && eid != Guid.Empty)
            .Select(i => i.EntityId!.Value)
            .Distinct()
            .ToList();

        if (entityUserIds.Count == 0)
        {
            return;
        }

        var entityUsers = await _db.Users.AsNoTracking()
            .Where(u => entityUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.NameAr, u.NameEn })
            .ToDictionaryAsync(u => u.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            if (!string.Equals(item.EntityType, "User", StringComparison.Ordinal) ||
                item.EntityId is not { } targetId ||
                !entityUsers.TryGetValue(targetId, out var target))
            {
                continue;
            }

            item.EntityTargetUserName = target.UserName;
            item.EntityTargetNameAr = target.NameAr;
            item.EntityTargetNameEn = target.NameEn;
        }
    }
}
