using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Larana.Models
{
    [Table("Dukkans")]
    public class Dukkan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Index]
        public DateTime CreatedAt { get; set; }
        
        [Index]
        public int ViewCount { get; set; }
        
        [Index]
        public int OrderCount { get; set; }

        public bool IsActive { get; set; }

        public string LogoPath { get; set; }
        
        [Range(0, 5)]
        public decimal Rating { get; set; }
        
        [StringLength(100)]
        public string Location { get; set; }
        
        [StringLength(50)]
        public string Category { get; set; }
        
        // New properties for enhanced profile
        public string CoverImagePath { get; set; }
        
        [StringLength(100)]
        public string FacebookUrl { get; set; }
        
        [StringLength(100)]
        public string InstagramUrl { get; set; }
        
        [StringLength(100)]
        public string TwitterUrl { get; set; }
        
        [StringLength(100)]
        public string WebsiteUrl { get; set; }
        
        [StringLength(200)]
        public string FullAddress { get; set; }
        
        [StringLength(20)]
        public string PhoneNumber { get; set; }
        
        [StringLength(100)]
        public string Email { get; set; }
        
        [StringLength(200)]
        public string OperatingHours { get; set; }
        
        // Draft status for shop - true means published, false means draft
        public bool IsPublished { get; set; }

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; }
        
        [ForeignKey("OwnerId")]
        public virtual User Owner { get; set; }
        
        // Collection of ratings for this shop
        public virtual ICollection<Rating> Ratings { get; set; }
        
        // Rating count to avoid repeated querying
        public int RatingCount { get; set; }

        public Dukkan()
        {
            CreatedAt = DateTime.Now;
            IsActive = true;
            IsPublished = false; // Default to draft
            Products = new HashSet<Product>();
            Ratings = new HashSet<Rating>();
            Rating = 0;
            RatingCount = 0;
            ViewCount = 0;
            OrderCount = 0;
        }
    }
} 