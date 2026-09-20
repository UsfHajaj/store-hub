using StoreHub.Application.Features.DataImport.DTOs;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.DataImport.Interfaces;

public interface IDataImportService
{
    byte[] GetTemplate(DataImportKind kind, bool sample);

    Task<Result<DataImportResultDto>> ImportAsync(
        DataImportKind kind,
        Stream fileStream,
        Guid? storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);
}
