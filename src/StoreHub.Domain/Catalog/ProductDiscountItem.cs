namespace StoreHub.Domain.Catalog;

public sealed class ProductDiscountItem
{
    public Guid DiscountId { get; set; }

    public Guid ProductId { get; set; }

    public ProductDiscount Discount { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
