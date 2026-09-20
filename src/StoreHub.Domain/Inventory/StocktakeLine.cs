using StoreHub.Domain.Catalog;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Inventory;

public sealed class StocktakeLine : BaseEntity
{
    public Guid StocktakeId { get; set; }

    public Guid ProductId { get; set; }

    public decimal SystemQuantity { get; set; }

    public decimal? CountedQuantity { get; set; }

    public decimal Difference { get; set; }

    public Stocktake Stocktake { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
