using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Larana.Models
{
    public class Rating
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Value { get; set; }
        
        [StringLength(500)]
        public string Comment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Required]
        public int DukkanId { get; set; }
        
        [ForeignKey("DukkanId")]
        public virtual Dukkan Dukkan { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
} 