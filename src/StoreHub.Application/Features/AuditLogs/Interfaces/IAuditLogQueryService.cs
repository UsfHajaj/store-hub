using StoreHub.Application.Features.AuditLogs.DTOs;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.AuditLogs.Interfaces;

public interface IAuditLogQueryService
{
    Task<Result<PagedResult<AuditLogListItemDto>>> GetPagedAsync(
        AuditLogFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuditLogListItemDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
