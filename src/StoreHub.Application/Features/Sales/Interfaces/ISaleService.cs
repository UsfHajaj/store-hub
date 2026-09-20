using StoreHub.Application.Features.Sales.DTOs;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Sales.Interfaces;

public interface ISaleService
{
    Task<Result<SaleDto>> CreateAsync(
        Guid storeId,
        CreateSaleRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<SaleListItemDto>>> GetPagedAsync(
        Guid storeId,
        SaleFilterRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<SaleDto>> GetByIdAsync(
        Guid storeId,
        Guid saleId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<SaleReturnDto>> CreateReturnAsync(
        Guid storeId,
        Guid saleId,
        CreateSaleReturnRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);
}

public interface IInvoicePdfService
{
    Task<Result<byte[]>> GenerateSaleInvoicePdfAsync(
        Guid storeId,
        Guid saleId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);
}
