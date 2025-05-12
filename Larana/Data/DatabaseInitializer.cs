using System;
using System.Data.Entity;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Threading;
using Larana.Models;

namespace Larana.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize()
        {
            // Set culture settings for Turkish language support
            SetCultureSettings();
            
            // Set the DataDirectory path
            AppDomain.CurrentDomain.SetData("DataDirectory", 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data"));
            
            // Set database initializers for specific context types
            Database.SetInitializer<ApplicationDbContext>(null);
            Database.SetInitializer<DbAccount>(null);
            Database.SetInitializer<OrdersDbContext>(null);
            
            InitializeApplicationDb();
            SeedDatabase();
        }
        
        private static void SetCultureSettings()
        {
            var cultureInfo = new CultureInfo("tr-TR");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        }
        
        private static void InitializeApplicationDb()
        {
            try
            {
                // Initialize main database context
                using (var context = DbContextFactory.CreateApplicationDbContext())
                {
                    // Force database creation if it doesn't exist
                    if (!context.Database.Exists())
                    {
                        context.Database.Create();
                    }
                    
                    // Apply the fix for the FK constraint issue
                    ApplyForeignKeyFixes(context);
                }
            }
            catch (Exception ex)
            {
                // Log any database initialization errors
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            }
        }
        
        private static void ApplyForeignKeyFixes(ApplicationDbContext context)
        {
            var script = @"
                -- Drop existing FK constraint if it exists
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Ratings_dbo.Users_UserId')
                BEGIN
                    ALTER TABLE [dbo].[Ratings] DROP CONSTRAINT [FK_dbo.Ratings_dbo.Users_UserId]
                    ALTER TABLE [dbo].[Ratings] ADD CONSTRAINT [FK_dbo.Ratings_dbo.Users_UserId] 
                    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) 
                    ON DELETE NO ACTION ON UPDATE NO ACTION
                END

                -- Drop existing FK constraint if it exists for DukkanId
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Ratings_dbo.Dukkans_DukkanId')
                BEGIN
                    ALTER TABLE [dbo].[Ratings] DROP CONSTRAINT [FK_dbo.Ratings_dbo.Dukkans_DukkanId]
                    ALTER TABLE [dbo].[Ratings] ADD CONSTRAINT [FK_dbo.Ratings_dbo.Dukkans_DukkanId] 
                    FOREIGN KEY ([DukkanId]) REFERENCES [dbo].[Dukkans] ([Id]) 
                    ON DELETE NO ACTION ON UPDATE NO ACTION
                END
            ";
            
            try
            {
                // Execute SQL only if database exists
                if (context.Database.Exists())
                {
                    context.Database.ExecuteSqlCommand(script);
                }
            }
            catch (Exception ex)
            {
                // Just log error - tables might not exist yet
                System.Diagnostics.Debug.WriteLine($"FK constraint script error: {ex.Message}");
            }
        }
        
        private static void SeedDatabase()
        {
            using (var db = DbContextFactory.CreateApplicationDbContext())
            {
                try
                {
                    // Create default roles if they don't exist
                    SeedRole(db, "Admin");
                    SeedRole(db, "Customer");
                    SeedRole(db, "Seller");

                    // Create admin user if it doesn't exist
                    if (!db.Users.Any(u => u.Username == "admin"))
                    {
                        var admin = new User
                        {
                            Username = "admin",
                            Email = "admin@larana.com",
                            CreatedAt = DateTime.Now,
                            IsActive = true,
                            UserType = UserType.Admin,
                            Role = UserRole.Admin,
                            Roles = "Admin"
                        };
                        admin.SetPassword("admin");
                        db.Users.Add(admin);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    // Log the error
                    System.Diagnostics.Debug.WriteLine($"Database seeding error: {ex.Message}");
                }
            }
        }
        
        private static void SeedRole(ApplicationDbContext context, string roleName)
        {
            try
            {
                if (!context.Roles.Any(r => r.Name == roleName))
                {
                    context.Roles.Add(new Role { Name = roleName });
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error seeding role {roleName}: {ex.Message}");
            }
        }
    }
} 