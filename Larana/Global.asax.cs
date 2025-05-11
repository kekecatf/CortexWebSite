using System.Web.Mvc;
using System.Web.Routing;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Larana.Models;
using Larana.Data;
using System.Linq;
using System.IO;
using System.Data.Entity;
using System.Web.Optimization;

namespace Larana
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Set the DataDirectory path
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data"));

            // Use CreateDatabaseIfNotExists for all context types
            Database.SetInitializer(new CreateDatabaseIfNotExists<ApplicationDbContext>());
            Database.SetInitializer(new CreateDatabaseIfNotExists<DbAccount>());
            Database.SetInitializer(new CreateDatabaseIfNotExists<OrdersDbContext>());
            
            try
            {
                // Initialize database contexts
                using (var context = new ApplicationDbContext())
                {
                    // Force database creation
                    if (!context.Database.Exists())
                    {
                        context.Database.Create();
                    }
                    
                    // Apply the fix for the FK constraint issue
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
                        // Execute SQL only if database exists and has tables
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
                
                // Initialize other contexts
                using (var db = new DbAccount())
                {
                    if (!db.Database.Exists())
                    {
                        db.Database.Create();
                    }
                }
                
                using (var ordersDb = new OrdersDbContext())
                {
                    if (!ordersDb.Database.Exists())
                    {
                        ordersDb.Database.Create();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log any database initialization errors
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            }

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            using (var db = new ApplicationDbContext())
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
                    // Log the error or handle it appropriately
                    System.Diagnostics.Debug.WriteLine($"Database seeding error: {ex.Message}");
                }
            }
        }

        private void SeedRole(ApplicationDbContext context, string roleName)
        {
            try
            {
                if (!context.Roles.Any(r => r.Name == roleName))
                {
                    context.Roles.Add(new Larana.Models.Role { Name = roleName });
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error seeding role {roleName}: {ex.Message}");
            }
        }

        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            if (HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
            {
                var formsIdentity = HttpContext.Current.User.Identity as FormsIdentity;
                if (formsIdentity != null)
                {
                    var roles = formsIdentity.Ticket.UserData.Split(',');
                    HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(formsIdentity, roles);
                }
            }
        }
    }
}
