using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Larana.Models;

namespace Larana.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        
        // New fields for shop product variants
        public int? ShopProductId { get; set; }
        public int? DukkanId { get; set; }
        public decimal UnitPrice { get; set; }

        [ForeignKey("CartId")]
        public virtual Cart Cart { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
        
        [ForeignKey("ShopProductId")]
        public virtual Larana.Models.ShopProduct ShopProduct { get; set; }
        
        [ForeignKey("DukkanId")]
        public virtual Dukkan Dukkan { get; set; }
    }
}
