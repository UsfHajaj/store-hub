using StoreHub.Application.Features.Notifications.DTOs;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Notifications.Interfaces;

public interface INotificationService
{
    Task<Result<NotificationDto>> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<NotificationDto>>> CreateBatchAsync(
        CreateNotificationBatchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<NotificationListItemDto>>> GetMyNotificationsAsync(
        NotificationFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<int>> GetMyUnreadCountAsync(CancellationToken cancellationToken = default);

    Task<Result<NotificationDto>> MarkReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> MarkAllMyReadAsync(CancellationToken cancellationToken = default);

    Task EnsureDefaultTemplatesAsync(CancellationToken cancellationToken = default);

    Task<Result<NotificationTemplateDto>> CreateTemplateAsync(
        CreateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationTemplateDto>> UpdateTemplateAsync(
        Guid id,
        UpdateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationTemplateDto>> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<NotificationTemplateDto>>> GetTemplatesPagedAsync(
        NotificationTemplateFilterRequest request,
        CancellationToken cancellationToken = default);

    Task PublishIntegrationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort in-app notification to one user. Never throws; failures are swallowed.
    /// </summary>
    Task NotifyUserSafeAsync(Guid userId, PublishNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort in-app notification to all active members of a store.
    /// </summary>
    Task NotifyStoreMembersSafeAsync(
        Guid storeId,
        PublishNotificationRequest request,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);
}
