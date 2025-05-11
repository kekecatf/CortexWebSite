using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Larana.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4,
        Completed = 5
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CartId { get; set; }

        [StringLength(250)]
        public string Address { get; set; }

        [StringLength(100)]
        public string ShippingCompany { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [StringLength(100)]
        public string RecipientName { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal? TotalAmount { get; set; }

        [Required]
        [EnumDataType(typeof(OrderStatus))]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public bool IsClickAndCollect { get; set; } = false;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        [ForeignKey("CartId")]
        public virtual Cart Cart { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }

        public Order()
        {
            OrderDetails = new HashSet<OrderDetail>();
            PaymentMethod = "Credit Card"; // Default payment method
        }
    }
}
