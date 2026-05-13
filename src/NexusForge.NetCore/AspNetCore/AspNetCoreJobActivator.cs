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
using Microsoft.Extensions.DependencyInjection;

namespace NexusForge.AspNetCore
{
    public class AspNetCoreJobActivator : JobActivator
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AspNetCoreJobActivator([NotNull] IServiceScopeFactory serviceScopeFactory)
        {
            if (serviceScopeFactory == null) throw new ArgumentNullException(nameof(serviceScopeFactory));
            _serviceScopeFactory = serviceScopeFactory;
        }

        public override JobActivatorScope BeginScope(JobActivatorContext context)
        {
            return new AspNetCoreJobActivatorScope(_serviceScopeFactory.CreateScope());
        }

#pragma warning disable CS0672 // Member overrides obsolete member
        public override JobActivatorScope BeginScope()
#pragma warning restore CS0672 // Member overrides obsolete member
        {
            return new AspNetCoreJobActivatorScope(_serviceScopeFactory.CreateScope());
        }
    }
}