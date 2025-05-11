using Microsoft.Owin;
using Owin;
using Larana.App_Start;

[assembly: OwinStartup(typeof(Larana.Startup))]
namespace Larana
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure Hangfire for background jobs
            HangfireConfig.Configure(app);
        }
    }
} 