// This file is part of NexusForge. Copyright © 2016 NexusForge OÜ.
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
using System.Collections.Generic;
using NexusForge.Annotations;
using Microsoft.Owin;

namespace NexusForge.Dashboard
{
    public sealed class OwinDashboardContext : DashboardContext
    {
        public OwinDashboardContext(
            [NotNull] JobStorage storage,
            [NotNull] DashboardOptions options,
            [NotNull] IDictionary<string, object> environment) 
            : base(storage, options)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));

            Environment = environment;
            Request = new OwinDashboardRequest(environment);
            Response = new OwinDashboardResponse(environment);
        }

        public IDictionary<string, object> Environment { get; }

        public override string GetUserName()
        {
            var context = new OwinContext(Environment);
            return context.Authentication?.User?.Identity?.IsAuthenticated == true
                ? context.Authentication.User.Identity.Name
                : null;
        }
    }
}
