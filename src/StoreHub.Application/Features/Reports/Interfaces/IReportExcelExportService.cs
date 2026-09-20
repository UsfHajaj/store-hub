using StoreHub.Application.Features.Reports.DTOs;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Reports.Interfaces;

public interface IReportExcelExportService
{
    Task<Result<byte[]>> ExportStoreSummaryAsync(
        Guid storeId,
        ReportRangeRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<byte[]>> ExportAdminSummaryAsync(
        ReportRangeRequest request,
        CancellationToken cancellationToken = default);
}
