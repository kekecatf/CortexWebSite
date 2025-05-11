using System.Data.Entity;
using Larana.Models;

namespace Larana.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection")
        {
            // Disable proxy creation for better performance
            this.Configuration.ProxyCreationEnabled = false;
            
            // Don't initialize database here - it's handled in Global.asax.cs
            Database.SetInitializer<ApplicationDbContext>(null);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Dukkan> Dukkans { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Dukkan relationship (corrected to use Owner)
            modelBuilder.Entity<Dukkan>()
                .HasRequired(d => d.Owner)
                .WithMany(u => u.Dukkans)
                .HasForeignKey(d => d.OwnerId)
                .WillCascadeOnDelete(false);

            // Configure Rating entity with NO cascade delete
            modelBuilder.Entity<Rating>()
                .HasRequired(r => r.Dukkan)
                .WithMany(d => d.Ratings)
                .HasForeignKey(r => r.DukkanId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Rating>()
                .HasRequired(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .WillCascadeOnDelete(false);

            // Configure table names and schema
            modelBuilder.Entity<Dukkan>().ToTable("Dukkans", "dbo");
            modelBuilder.Entity<User>().ToTable("Users", "dbo");
            modelBuilder.Entity<Product>().ToTable("Products", "dbo");
            modelBuilder.Entity<Cart>().ToTable("Carts", "dbo");
            modelBuilder.Entity<CartItem>().ToTable("CartItems", "dbo");
            modelBuilder.Entity<Order>().ToTable("Orders", "dbo");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails", "dbo");
            modelBuilder.Entity<Role>().ToTable("Roles", "dbo");
            modelBuilder.Entity<Rating>().ToTable("Ratings", "dbo");

            // Dukkan - Product relationship
            modelBuilder.Entity<Product>()
                .HasRequired(p => p.Dukkan)
                .WithMany(d => d.Products)
                .HasForeignKey(p => p.DukkanId)
                .WillCascadeOnDelete(false);

            // User - Cart relationship
            modelBuilder.Entity<Cart>()
                .HasRequired(c => c.User)
                .WithMany(u => u.Carts)
                .HasForeignKey(c => c.UserId)
                .WillCascadeOnDelete(false);

            // Cart - CartItem relationship
            modelBuilder.Entity<CartItem>()
                .HasRequired(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .WillCascadeOnDelete(true);

            // Product - CartItem relationship
            modelBuilder.Entity<CartItem>()
                .HasRequired(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .WillCascadeOnDelete(false);

            // Configure Order entity
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

            // Configure OrderDetail entity
            modelBuilder.Entity<OrderDetail>()
                .HasRequired(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<OrderDetail>()
                .HasRequired(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .WillCascadeOnDelete(false);

            // Configure Role entity
            modelBuilder.Entity<Role>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<Role>()
                .Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(50);
        }
    }
} 