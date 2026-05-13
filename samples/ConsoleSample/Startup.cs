using System;
using NexusForge;
using Owin;

namespace ConsoleSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();
            app.UseNexusForgeDashboard(String.Empty);

            app.UseNexusForgeServer(new BackgroundJobServerOptions
            {
                Queues = new[] { "critical", "default" },
                TaskScheduler = null,
                SchedulePollingInterval = TimeSpan.FromSeconds(1)
            });
        }
    }
}