// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Globalization;
using System.Linq;
using NexusForge.Annotations;
using NexusForge.Common;
using NexusForge.Server;
using Newtonsoft.Json;

namespace NexusForge
{
    public class DisableConcurrentExecutionAttribute : JobFilterAttribute, IServerFilter
    {
        public DisableConcurrentExecutionAttribute(int timeoutInSeconds)
        {
            if (timeoutInSeconds < 0) throw new ArgumentException("Timeout argument value should be greater that zero.");

            TimeoutSec = timeoutInSeconds;
            Scope = DistributedLockScope.Default;
            TenantFallbackMode = TenantLockFallbackMode.Throw;
        }
        
        [JsonConstructor]
        public DisableConcurrentExecutionAttribute(string resource, int timeoutSec)
            : this(timeoutSec)
        {
            Resource = resource;
        }

        [CanBeNull]
        public string Resource { get; }
        public int TimeoutSec { get; }
        public DistributedLockScope Scope { get; set; }
        public TenantLockFallbackMode TenantFallbackMode { get; set; }

        public void OnPerforming(PerformingContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var resource = GetResource(context.BackgroundJob.Job);
            var timeout = TimeSpan.FromSeconds(TimeoutSec);
            var scope = Scope == DistributedLockScope.Default
                ? DisableConcurrentExecutionOptions.Current.DefaultScope
                : Scope;

            var distributedLock = scope == DistributedLockScope.Tenant
                ? AcquireTenantLock(context, resource, timeout)
                : context.Connection.AcquireDistributedLock(resource, timeout);
            context.Items["DistributedLock"] = distributedLock;
        }

        public void OnPerformed(PerformedContext context)
        {
            if (!context.Items.TryGetValue("DistributedLock", out var value))
            {
                throw new InvalidOperationException("Can not release a distributed lock: it was not acquired.");
            }

            var distributedLock = (IDisposable)value;
            distributedLock.Dispose();
        }

        private IDisposable AcquireTenantLock(PerformingContext context, string resource, TimeSpan timeout)
        {
            var tenantId = NexusForgeTenantContext.CurrentTenantId;
            if (tenantId == null)
            {
                throw new InvalidOperationException("Tenant-scoped DisableConcurrentExecution requires NexusForgeTenantContext.CurrentTenantId to be set.");
            }

            var fallbackMode = TenantFallbackMode;
            if (fallbackMode == TenantLockFallbackMode.Throw && Scope == DistributedLockScope.Default)
            {
                fallbackMode = DisableConcurrentExecutionOptions.Current.TenantFallbackMode;
            }

            return context.Connection.AcquireTenantDistributedLock(tenantId, resource, timeout, fallbackMode);
        }

        private string GetResource(Job job)
        {
            if (!String.IsNullOrWhiteSpace(Resource))
            {
                try
                {
                    return String.Format(CultureInfo.InvariantCulture, Resource, job.Args.ToArray()).ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    throw new FormatException($"Unable to obtain resource identifier: {ex.Message}");
                }
            }

            return $"{job.Type.ToGenericTypeString()}.{job.Method.Name}";
        }
    }
}
