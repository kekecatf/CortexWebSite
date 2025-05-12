using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Larana.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string Brand { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Sales { get; set; }

        public string Category { get; set; }

        public string PhotoPath { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        public int Stock { get; set; }

        public bool IsClickAndCollect { get; set; }

        public bool IsActive { get; set; }

        public int DisplayOrder { get; set; }

        public int? DukkanId { get; set; }
        
        [Index]
        public int ViewCount { get; set; }
        
        [Index]
        public int OrderCount { get; set; }
        
        [Index]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("DukkanId")]
        public virtual Dukkan Dukkan { get; set; }

        public virtual ICollection<Review> Reviews { get; set; }

        public Product()
        {
            Sales = 0;
            Stock = 0;
            IsActive = true;
            DisplayOrder = 999;
            ViewCount = 0;
            OrderCount = 0;
            CreatedAt = DateTime.Now;
            Reviews = new HashSet<Review>();
        }
    }
}
