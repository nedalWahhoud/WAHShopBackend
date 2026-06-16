using Microsoft.AspNetCore.Mvc;

namespace WAHShopBackend.Models
{
    public class GetItems<T>
    {
        public List<T> Items { get; set; } = [];
        
        public bool AllItemsLoaded { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int UserId { get; set; }

        public List<int> ExcludeProductsIds { get; set; } = [];
        public bool IsAdmin { get; set; } = false;
        public FilterOption? Filter { get; set; }

        public ProductIncludes Includes { get; set; } = ProductIncludes.All;
    }
    public class FilterOption
    {
        public int Id { get; set; } = 0;
        public GetItemFilterType Type { get; set; } = GetItemFilterType.None;
    }
    public enum GetItemFilterType
    {
        None,
        Category,
        Custom,
        Supplier,
        LowStock,
        OnOffer
    }

   
    [Flags]
    public enum ProductIncludes
    {
        None = 0,
        Category = 1 << 0,      // 1
        Suppliers = 1 << 1,     // 2
        TaxRate = 1 << 2,       // 4
        ProductGroup = 1 << 3,  // 8
        ProductImages = 1 << 4, // 16
        ProductDiscount = 1 << 5, // 32
        All = Category | Suppliers | TaxRate | ProductGroup | ProductImages | ProductDiscount
    }
}
