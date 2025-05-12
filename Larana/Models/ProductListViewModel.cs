using System.Collections.Generic;

namespace Larana.Models
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; }
        public PaginationInfo PaginationInfo { get; set; }
        public string SearchTerm { get; set; }
        public int DukkanId { get; set; }
    }

    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (TotalItems + ItemsPerPage - 1) / ItemsPerPage;
    }
} 