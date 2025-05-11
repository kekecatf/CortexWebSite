using System;
using System.Data.Entity;
using System.Linq;
using Larana.Models;

namespace Larana.Models
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext()
            : base("OrdersDbContext")
        {
            // Disable proxy creation
            this.Configuration.ProxyCreationEnabled = false;
            
            // Don't initialize database here since we're doing it in Global.asax.cs
            Database.SetInitializer<OrdersDbContext>(null);
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure Order entity
            modelBuilder.Entity<Order>()
                .HasKey(o => o.Id);

            modelBuilder.Entity<Order>()
                .HasRequired(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Order>()
                .HasRequired(o => o.Cart)
                .WithMany()
                .HasForeignKey(o => o.CartId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderDetails)
                .WithRequired(od => od.Order)
                .HasForeignKey(od => od.OrderId)
                .WillCascadeOnDelete(true);

            // Configure OrderDetail entity
            modelBuilder.Entity<OrderDetail>()
                .HasKey(od => od.Id);

            modelBuilder.Entity<OrderDetail>()
                .HasRequired(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .WillCascadeOnDelete(true);

            // Configure Role entity
            modelBuilder.Entity<Role>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<Role>()
                .Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(50);

            base.OnModelCreating(modelBuilder);
        }
    }
}
