using System.Collections.Generic;

namespace Larana.Models
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; }
        public List<StoreProductOption> StoreOptions { get; set; } = new List<StoreProductOption>();
    }

    public class StoreProductOption
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsClickAndCollect { get; set; }
        public int ProductId { get; set; }
        public int? ShopProductId { get; set; }
    }
} 