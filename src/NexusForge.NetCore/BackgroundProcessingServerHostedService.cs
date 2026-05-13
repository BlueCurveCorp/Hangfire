// This file is part of Hangfire.
// Copyright © 2021 NexusForge OÜ.
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

#if !NET451 && !NETSTANDARD1_3

using System;
using System.Threading;
using System.Threading.Tasks;
using NexusForge.Annotations;
using NexusForge.Server;
using Microsoft.Extensions.Hosting;

namespace NexusForge
{
    public sealed class BackgroundProcessingServerHostedService : IHostedService, IDisposable
    {
        private IBackgroundProcessingServer _server;

#if NETSTANDARD2_1
        public BackgroundProcessingServerHostedService([NotNull] IBackgroundProcessingServer server)
            : this(server, null)
        {
        }

        public BackgroundProcessingServerHostedService(
            [NotNull] IBackgroundProcessingServer server,
            [CanBeNull] IHostApplicationLifetime lifetime)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            lifetime?.ApplicationStopping.Register(server.SendStop);
        }
#else
        public BackgroundProcessingServerHostedService([NotNull] IBackgroundProcessingServer server)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
        }
#endif

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server?.SendStop();
            return _server?.WaitForShutdownAsync(cancellationToken) ?? Task.CompletedTask;
        }

        public void Dispose()
        {
            _server?.Dispose();
            _server = null;
        }
    }
}

#endif