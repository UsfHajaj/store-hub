using StoreHub.Application.Features.Reports.DTOs;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Reports.Interfaces;

public interface IReportService
{
    Task<Result<StoreReportSummaryDto>> GetStoreSummaryAsync(
        Guid storeId,
        ReportRangeRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<AdminReportSummaryDto>> GetAdminSummaryAsync(
        ReportRangeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<MyPerformanceDto>> GetMyPerformanceAsync(
        Guid storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);
}
