using System;
using System.Web;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Owin;
using Owin;
using Larana.Services;
using Larana.Data;

namespace Larana
{
    public class HangfireConfig
    {
        public static void Configure(IAppBuilder app)
        {
            // Configure Hangfire with SQL Server
            GlobalConfiguration.Configuration
                .UseSqlServerStorage("OrdersDbContext");

            // Register the Hangfire dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() }
            });

            // Start the Hangfire server
            app.UseHangfireServer();

            // Schedule recurring jobs
            ConfigureJobs();
        }

        private static void ConfigureJobs()
        {
            // Recalculate shop ratings daily at midnight
            RecurringJob.AddOrUpdate<PopularityService>(
                "RecalculateShopRatings",
                service => service.RecalculateShopRatings(),
                Cron.Daily);
        }
    }

    // Basic authorization filter for Hangfire dashboard
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // Only allow admin users to access the dashboard
            var owinContext = new OwinContext(context.GetOwinEnvironment());
            return owinContext.Authentication.User.IsInRole("Admin");
        }
    }
} 