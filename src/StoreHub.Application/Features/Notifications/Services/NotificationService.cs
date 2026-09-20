using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StoreHub.Application.Common;
using StoreHub.Application.Features.Notifications.DTOs;
using StoreHub.Application.Features.Notifications.Interfaces;
using StoreHub.Domain.Enums;
using StoreHub.Domain.Notifications;
using StoreHub.Persistence;
using StoreHub.Shared.Api;
using StoreHub.Shared.Constants;
using StoreHub.Shared.Identity;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Notifications.Services;

public sealed class NotificationService : INotificationService
{
    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateNotificationRequest> _createValidator;
    private readonly IValidator<CreateNotificationBatchRequest> _batchValidator;
    private readonly IValidator<NotificationFilterRequest> _filterValidator;
    private readonly IValidator<CreateNotificationTemplateRequest> _createTemplateValidator;
    private readonly IValidator<UpdateNotificationTemplateRequest> _updateTemplateValidator;
    private readonly IValidator<NotificationTemplateFilterRequest> _templateFilterValidator;

    public NotificationService(
        StoreHubDbContext db,
        ICurrentUserService currentUser,
        IValidator<CreateNotificationRequest> createValidator,
        IValidator<CreateNotificationBatchRequest> batchValidator,
        IValidator<NotificationFilterRequest> filterValidator,
        IValidator<CreateNotificationTemplateRequest> createTemplateValidator,
        IValidator<UpdateNotificationTemplateRequest> updateTemplateValidator,
        IValidator<NotificationTemplateFilterRequest> templateFilterValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _batchValidator = batchValidator;
        _filterValidator = filterValidator;
        _createTemplateValidator = createTemplateValidator;
        _updateTemplateValidator = updateTemplateValidator;
        _templateFilterValidator = templateFilterValidator;
    }

    public async Task<Result<NotificationDto>> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<NotificationDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var userCheck = await ValidateActiveUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (userCheck is not null)
        {
            return Result<NotificationDto>.Fail(userCheck.Value.message, userCheck.Value.code);
        }

        var utc = DateTime.UtcNow;
        var entity = new Notification
        {
            UserId = request.UserId,
            NotificationType = request.NotificationType,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Channel = request.Channel,
            IsRead = false,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityType = string.IsNullOrWhiteSpace(request.RelatedEntityType)
                ? null
                : request.RelatedEntityType.Trim(),
            RecordStatus = RecordStatus.Active
        };

        AppendDispatchLogs(entity, utc);
        _db.Notifications.Add(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<NotificationDto>.Ok(await MapNotificationAsync(entity.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> CreateBatchAsync(
        CreateNotificationBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _batchValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<IReadOnlyList<NotificationDto>>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var distinctIds = request.UserIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return Result<IReadOnlyList<NotificationDto>>.Fail(
                "At least one valid user id is required.",
                NotificationErrors.NoValidUsersInBatch);
        }

        var validUserIds = await _db.Users.AsNoTracking()
            .Where(u => distinctIds.Contains(u.Id) && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (validUserIds.Count == 0)
        {
            return Result<IReadOnlyList<NotificationDto>>.Fail(
                "No active users were found for the supplied ids.",
                NotificationErrors.NoValidUsersInBatch);
        }

        var utc = DateTime.UtcNow;
        var created = new List<Notification>();
        foreach (var userId in validUserIds)
        {
            var entity = new Notification
            {
                UserId = userId,
                NotificationType = request.NotificationType,
                Title = request.Title.Trim(),
                Message = request.Message.Trim(),
                Channel = request.Channel,
                IsRead = false,
                RelatedEntityId = request.RelatedEntityId,
                RelatedEntityType = string.IsNullOrWhiteSpace(request.RelatedEntityType)
                    ? null
                    : request.RelatedEntityType.Trim(),
                RecordStatus = RecordStatus.Active
            };
            AppendDispatchLogs(entity, utc);
            _db.Notifications.Add(entity);
            created.Add(entity);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var list = new List<NotificationDto>();
        foreach (var entity in created)
        {
            list.Add(await MapNotificationAsync(entity.Id, cancellationToken).ConfigureAwait(false));
        }

        return Result<IReadOnlyList<NotificationDto>>.Ok(list);
    }

    public async Task<Result<NotificationDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = RequireCurrentUserId();
        if (current is null)
        {
            return Result<NotificationDto>.Fail(
                "An authenticated user is required.",
                NotificationErrors.CurrentUserRequired);
        }

        var entity = await _db.Notifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return Result<NotificationDto>.Fail("The notification was not found.", NotificationErrors.NotFound);
        }

        if (entity.UserId != current.Value)
        {
            return Result<NotificationDto>.Fail(
                "You can only view your own notifications.",
                NotificationErrors.NotOwner);
        }

        return Result<NotificationDto>.Ok(await MapNotificationAsync(id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<PagedResult<NotificationListItemDto>>> GetMyNotificationsAsync(
        NotificationFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _filterValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<PagedResult<NotificationListItemDto>>.Fail(
                validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var current = RequireCurrentUserId();
        if (current is null)
        {
            return Result<PagedResult<NotificationListItemDto>>.Fail(
                "An authenticated user is required.",
                NotificationErrors.CurrentUserRequired);
        }

        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == current.Value);
        if (request.UnreadOnly.GetValueOrDefault())
        {
            query = query.Where(n => !n.IsRead && n.ReadOnUtc == null);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(n => n.CreatedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationListItemDto
            {
                Id = n.Id,
                NotificationType = n.NotificationType,
                Title = n.Title,
                Message = n.Message,
                Channel = n.Channel,
                IsRead = n.IsRead,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType,
                CreatedOnUtc = n.CreatedOnUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<NotificationListItemDto>>.Ok(new PagedResult<NotificationListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<int>> GetMyUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var current = RequireCurrentUserId();
        if (current is null)
        {
            return Result<int>.Fail(
                "An authenticated user is required.",
                NotificationErrors.CurrentUserRequired);
        }

        var count = await _db.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == current.Value && !n.IsRead && n.ReadOnUtc == null, cancellationToken)
            .ConfigureAwait(false);

        return Result<int>.Ok(count);
    }

    public async Task<Result<NotificationDto>> MarkReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = RequireCurrentUserId();
        if (current is null)
        {
            return Result<NotificationDto>.Fail(
                "An authenticated user is required.",
                NotificationErrors.CurrentUserRequired);
        }

        var entity = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return Result<NotificationDto>.Fail("The notification was not found.", NotificationErrors.NotFound);
        }

        if (entity.UserId != current.Value)
        {
            return Result<NotificationDto>.Fail(
                "You can only mark your own notifications as read.",
                NotificationErrors.NotOwner);
        }

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            entity.ReadOnUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<NotificationDto>.Ok(await MapNotificationAsync(entity.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result> MarkAllMyReadAsync(CancellationToken cancellationToken = default)
    {
        var current = RequireCurrentUserId();
        if (current is null)
        {
            return Result.Fail("An authenticated user is required.", NotificationErrors.CurrentUserRequired);
        }

        var utc = DateTime.UtcNow;
        await _db.Notifications
            .Where(n => n.UserId == current.Value && !n.IsRead && n.ReadOnUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadOnUtc, utc),
                cancellationToken)
            .ConfigureAwait(false);

        return Result.Ok();
    }

    public async Task EnsureDefaultTemplatesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var def in DefaultTemplateDefinitions)
        {
            var exists = await _db.NotificationTemplates.AsNoTracking()
                .AnyAsync(t => t.Code == def.Code, cancellationToken)
                .ConfigureAwait(false);
            if (exists)
            {
                continue;
            }

            _db.NotificationTemplates.Add(new NotificationTemplate
            {
                Code = def.Code,
                Name = def.Name,
                SubjectTemplate = def.Subject,
                BodyTemplate = def.Body,
                NotificationType = def.Type,
                Channel = def.Channel,
                IsActive = true,
                RecordStatus = RecordStatus.Active
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<NotificationTemplateDto>> CreateTemplateAsync(
        CreateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createTemplateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<NotificationTemplateDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.NotificationTemplates.AsNoTracking()
                .AnyAsync(t => t.Code == code, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result<NotificationTemplateDto>.Fail(
                "A template with this code already exists.",
                NotificationErrors.DuplicateTemplateCode);
        }

        var entity = new NotificationTemplate
        {
            Code = code,
            Name = request.Name.Trim(),
            SubjectTemplate = string.IsNullOrWhiteSpace(request.SubjectTemplate)
                ? null
                : request.SubjectTemplate.Trim(),
            BodyTemplate = request.BodyTemplate.Trim(),
            NotificationType = request.NotificationType,
            Channel = request.Channel,
            IsActive = true,
            RecordStatus = RecordStatus.Active
        };

        _db.NotificationTemplates.Add(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<NotificationTemplateDto>.Ok(await MapTemplateAsync(entity.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<NotificationTemplateDto>> UpdateTemplateAsync(
        Guid id,
        UpdateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateTemplateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<NotificationTemplateDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var entity = await _db.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return Result<NotificationTemplateDto>.Fail(
                "The notification template was not found.",
                NotificationErrors.TemplateNotFound);
        }

        entity.Name = request.Name.Trim();
        entity.SubjectTemplate = string.IsNullOrWhiteSpace(request.SubjectTemplate)
            ? null
            : request.SubjectTemplate.Trim();
        entity.BodyTemplate = request.BodyTemplate.Trim();
        entity.NotificationType = request.NotificationType;
        entity.Channel = request.Channel;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<NotificationTemplateDto>.Ok(await MapTemplateAsync(entity.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<NotificationTemplateDto>> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _db.NotificationTemplates.AsNoTracking().AnyAsync(t => t.Id == id, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result<NotificationTemplateDto>.Fail(
                "The notification template was not found.",
                NotificationErrors.TemplateNotFound);
        }

        return Result<NotificationTemplateDto>.Ok(await MapTemplateAsync(id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<PagedResult<NotificationTemplateDto>>> GetTemplatesPagedAsync(
        NotificationTemplateFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _templateFilterValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<PagedResult<NotificationTemplateDto>>.Fail(
                validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = _db.NotificationTemplates.AsNoTracking().AsQueryable();
        if (request.ActiveOnly == true)
        {
            query = query.Where(t => t.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(t => t.Name.Contains(term) || t.Code.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderBy(t => t.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new NotificationTemplateDto
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                SubjectTemplate = t.SubjectTemplate,
                BodyTemplate = t.BodyTemplate,
                NotificationType = t.NotificationType,
                Channel = t.Channel,
                IsActive = t.IsActive
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<NotificationTemplateDto>>.Ok(new PagedResult<NotificationTemplateDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task PublishIntegrationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await CreateAsync(request, cancellationToken).ConfigureAwait(false);
            _ = result;
        }
        catch
        {
            // Intentionally swallow: domain workflows must not fail on notification side effects.
        }
    }

    private static void AppendDispatchLogs(Notification notification, DateTime utc)
    {
        if (notification.Channel is NotificationChannel.InApp or NotificationChannel.Both)
        {
            notification.DispatchLogs.Add(new NotificationDispatchLog
            {
                Channel = NotificationChannel.InApp,
                DispatchStatus = NotificationDispatchStatus.Sent,
                AttemptedOnUtc = utc,
                RecordStatus = RecordStatus.Active
            });
        }

        if (notification.Channel is NotificationChannel.Email or NotificationChannel.Both)
        {
            notification.DispatchLogs.Add(new NotificationDispatchLog
            {
                Channel = NotificationChannel.Email,
                DispatchStatus = NotificationDispatchStatus.Pending,
                AttemptedOnUtc = utc,
                RecordStatus = RecordStatus.Active
            });
        }
    }

    private Guid? RequireCurrentUserId()
    {
        if (_currentUser.UserId is { } id && id != Guid.Empty)
        {
            return id;
        }

        return null;
    }

    private async Task<(string message, string code)?> ValidateActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            return ("The user was not found.", NotificationErrors.UserNotFound);
        }

        if (!user.IsActive)
        {
            return ("The user is inactive.", NotificationErrors.UserInactive);
        }

        return null;
    }

    private async Task<NotificationDto> MapNotificationAsync(Guid id, CancellationToken cancellationToken)
    {
        var n = await _db.Notifications.AsNoTracking()
            .FirstAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return new NotificationDto
        {
            Id = n.Id,
            UserId = n.UserId,
            NotificationType = n.NotificationType,
            Title = n.Title,
            Message = n.Message,
            Channel = n.Channel,
            IsRead = n.IsRead,
            ReadOnUtc = n.ReadOnUtc,
            RelatedEntityId = n.RelatedEntityId,
            RelatedEntityType = n.RelatedEntityType,
            CreatedOnUtc = n.CreatedOnUtc
        };
    }

    private async Task<NotificationTemplateDto> MapTemplateAsync(Guid id, CancellationToken cancellationToken)
    {
        var t = await _db.NotificationTemplates.AsNoTracking()
            .FirstAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return new NotificationTemplateDto
        {
            Id = t.Id,
            Code = t.Code,
            Name = t.Name,
            SubjectTemplate = t.SubjectTemplate,
            BodyTemplate = t.BodyTemplate,
            NotificationType = t.NotificationType,
            Channel = t.Channel,
            IsActive = t.IsActive
        };
    }

    private static readonly (string Code, string Name, string? Subject, string Body, NotificationType Type, NotificationChannel Channel)
        [] DefaultTemplateDefinitions =
    [
        (
            "STORE_HUB_GENERAL",
            "إدارة المحلات — إشعار عام",
            "إشعار",
            "مرحباً بك في نظام إدارة المحلات. يمكنك توسيع القوالب من هنا.",
            NotificationType.General,
            NotificationChannel.InApp
        )
    ];
}
