// This file is part of NexusForge. Copyright © 2013-2014 NexusForge OÜ.
// 
// NexusForge is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// NexusForge is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with NexusForge. If not, see <http://www.gnu.org/licenses/>.

using System;
using NexusForge.Annotations;
using NexusForge.Dashboard;
using NexusForge.Server;
using Owin;

namespace NexusForge
{
    /// <exclude />
    [Obsolete("Please use `GlobalConfiguration` class for configuration, or `IAppBuilder.UseNexusForgeDashboard` and `IAppBuilder.UseNexusForgeServer` OWIN extension methods instead. Will be removed in version 2.0.0.")]
    public static class OwinBootstrapper
    {
        /// <summary>
        /// Bootstraps NexusForge components using the given configuration
        /// action and maps NexusForge Dashboard to the app builder pipeline
        /// at the configured path ('/nexusforge' by default).
        /// </summary>
        /// <param name="app">The app builder</param>
        /// <param name="configurationAction">Configuration action</param>
        [Obsolete("Please use `GlobalConfiguration` class for configuration, or `IAppBuilder.UseNexusForgeDashboard` and `IAppBuilder.UseNexusForgeServer` OWIN extension methods instead. Will be removed in version 2.0.0.")]
        public static void UseNexusForge(
            [NotNull] this IAppBuilder app,
            [NotNull] Action<IBootstrapperConfiguration> configurationAction)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (configurationAction == null) throw new ArgumentNullException(nameof(configurationAction));

            var configuration = new BootstrapperConfiguration();
            configurationAction(configuration);

            if (configuration.Activator != null)
            {
                JobActivator.Current = configuration.Activator;
            }

            if (configuration.Storage != null)
            {
                JobStorage.Current = configuration.Storage;
            }

            foreach (var filter in configuration.Filters)
            {
                GlobalJobFilters.Filters.Add(filter);
            }

            foreach (var server in configuration.Servers)
            {
                app.RunNexusForgeServer(server());
            }

            app.MapNexusForgeDashboard(configuration.DashboardPath, configuration.AppPath, configuration.AuthorizationFilters);
        }
    }
}
