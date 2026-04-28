namespace WAHShopBackend.Models
{
    public class GetItems<T>
    {
        public List<T> Items { get; set; } = [];
        
        public bool AllItemsLoaded { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public FilterOption? Filter { get; set; }
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

    public class FilterOption
    {
        public int Id { get; set; } = 0;
        public GetItemFilterType Type { get; set; } = GetItemFilterType.None;
    }
}
