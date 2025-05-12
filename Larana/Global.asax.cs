using System.Web.Mvc;
using System.Web.Routing;
using System;
using System.Web;
using System.Web.Security;
using Larana.Models;
using Larana.Data;
using System.Globalization;
using System.Threading;
using System.Web.Optimization;

namespace Larana
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Initialize database and seed data
            DatabaseInitializer.Initialize();
            
            // Create Reviews table using direct SQL
            try 
            {
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["LaranaConnection"].ConnectionString;
                using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // Check if table exists
                    bool tableExists = false;
                    using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Reviews'", connection))
                    {
                        var result = cmd.ExecuteScalar();
                        tableExists = Convert.ToInt32(result) > 0;
                    }
                    
                    if (!tableExists)
                    {
                        // Create table if it doesn't exist
                        string createTableSql = @"
                        CREATE TABLE [dbo].[Reviews] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [ProductId] INT NOT NULL,
                            [UserId] INT NOT NULL,
                            [Rating] INT NOT NULL,
                            [Comment] NVARCHAR(500) NOT NULL,
                            [CreatedAt] DATETIME NOT NULL,
                            CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_Reviews_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]),
                            CONSTRAINT [FK_Reviews_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
                        )";
                        
                        using (var cmd = new System.Data.SqlClient.SqlCommand(createTableSql, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        
                        // Create indexes
                        string createIndexesSql = @"
                        CREATE INDEX [IX_Reviews_ProductId] ON [dbo].[Reviews]([ProductId]);
                        CREATE INDEX [IX_Reviews_UserId] ON [dbo].[Reviews]([UserId]);";
                        
                        using (var cmd = new System.Data.SqlClient.SqlCommand(createIndexesSql, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        
                        System.Diagnostics.Debug.WriteLine("Reviews table created successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error creating Reviews table: " + ex.Message);
                // Hatayı kontrol et ama uygulamanın çalışmasını engelleme
            }

            // Register areas, routes and bundles
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
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
