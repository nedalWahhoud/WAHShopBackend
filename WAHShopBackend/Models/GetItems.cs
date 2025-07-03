namespace WAHShopBackend.Models
{
    public class GetItems<T>
    {
        public List<T> Items { get; set; } = [];
        public bool AllItemsLoaded { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } 
    }
}
