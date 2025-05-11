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
using System.Globalization;
using System.Threading;

namespace Larana
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Türkçe karakter desteği için kültür ayarları
            var cultureInfo = new CultureInfo("tr-TR");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

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
            try
            {
                if (HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    var formsIdentity = HttpContext.Current.User.Identity as FormsIdentity;
                    if (formsIdentity != null && !string.IsNullOrEmpty(formsIdentity.Ticket.UserData))
                    {
                        // Split the roles and ensure no empty elements
                        var roles = formsIdentity.Ticket.UserData
                            .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        if (roles.Length > 0)
                        {
                            HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(formsIdentity, roles);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - authentication error should not break the request
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
            }
        }
        
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Her istek başlangıcında kültür ayarlarını yap
            var cultureInfo = new CultureInfo("tr-TR");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            
            // Response kodlamasını ayarla
            HttpContext.Current.Response.ContentEncoding = System.Text.Encoding.UTF8;
            HttpContext.Current.Response.Charset = "utf-8";
        }
    }
}
