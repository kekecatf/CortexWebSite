using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Larana.Models
{
    [Table("ShopProducts")]
    public class ShopProduct
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DukkanId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        public int Stock { get; set; }

        public bool IsClickAndCollect { get; set; }

        [Range(0, int.MaxValue)]
        public int Sales { get; set; }

        // Navigation properties
        [ForeignKey("DukkanId")]
        public virtual Dukkan Dukkan { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public ShopProduct()
        {
            Stock = 0;
            Sales = 0;
            IsClickAndCollect = false;
        }
    }
} 