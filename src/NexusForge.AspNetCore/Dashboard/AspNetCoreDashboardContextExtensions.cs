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
using NexusForge.Annotations;
using Microsoft.AspNetCore.Http;

namespace NexusForge.Dashboard
{
    public static class AspNetCoreDashboardContextExtensions
    {
        public static HttpContext GetHttpContext([NotNull] this DashboardContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var aspNetCoreContext = context as AspNetCoreDashboardContext;
            if (aspNetCoreContext == null)
            {
                throw new ArgumentException($"Context argument should be of type `{nameof(AspNetCoreDashboardContext)}`!", nameof(context));
            }

            return aspNetCoreContext.HttpContext;
        }
    }
}