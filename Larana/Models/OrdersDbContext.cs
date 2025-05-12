using System;
using System.Data.Entity;
using System.Linq;
using Larana.Models;
using Larana.Data;

namespace Larana.Models
{
    // This context is for backward compatibility - we should eventually
    // merge all functionality into ApplicationDbContext
    public class OrdersDbContext : BaseDbContext
    {
        public OrdersDbContext()
            : base("LaranaConnection") // Use the same connection as ApplicationDbContext
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
    }
}
