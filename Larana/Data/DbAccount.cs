using System.Data.Entity;
using Larana.Models;

namespace Larana.Data
{
    public class DbAccount : DbContext
    {
        public DbAccount() : base("DbAccount")
        {
            // Disable proxy creation
            this.Configuration.ProxyCreationEnabled = false;
            
            // Don't initialize database here since we're doing it in Global.asax.cs
            Database.SetInitializer<DbAccount>(null);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure User entity
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(u => u.Password)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(u => u.Roles)
                .IsRequired()
                .HasMaxLength(50);

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