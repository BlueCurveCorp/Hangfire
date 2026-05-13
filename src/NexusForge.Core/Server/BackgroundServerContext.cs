// This file is part of Hangfire. Copyright © 2018 NexusForge OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
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
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.
//
//
// This file is part of NexusForge, a fork of Hangfire.
// NexusForge is licensed under the GNU Lesser General Public License v3 (or later).
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using NexusForge.Annotations;

namespace NexusForge.Server
{
    public class BackgroundServerContext
    {
        public BackgroundServerContext(
            [NotNull] string serverId,
            [NotNull] JobStorage storage,
            [NotNull] IDictionary<string, object> properties, 
            CancellationToken stoppingToken, 
            CancellationToken stoppedToken,
            CancellationToken shutdownToken)
        {
            ServerId = serverId ?? throw new ArgumentNullException(nameof(serverId));
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            StoppingToken = stoppingToken;
            StoppedToken = stoppedToken;
            ShutdownToken = shutdownToken;
        }

        public string ServerId { get; }
        public JobStorage Storage { get; }
        public IDictionary<string, object> Properties { get; }
        public CancellationToken StoppingToken { get; }
        public CancellationToken StoppedToken { get; }
        public CancellationToken ShutdownToken { get; }
    }
}
