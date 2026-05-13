// This file is part of Hangfire. Copyright © 2015 NexusForge OÜ.
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
using System.Threading;

namespace NexusForge
{
    public abstract class JobActivatorScope : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ThreadLocal<JobActivatorScope> _current
            = new ThreadLocal<JobActivatorScope>(trackAllValues: false);

        protected JobActivatorScope()
        {
            _current.Value = this;
        }

        public static JobActivatorScope Current => _current.Value;

        [Obsolete("This property wasn't implemented and will be removed in NexusForge 2.0.0.")]
        public object InnerScope { get; set; }

        public abstract object Resolve(Type type);

        public virtual void DisposeScope()
        {
        }

        public void Dispose()
        {
            try
            {
                DisposeScope();
            }
            finally
            {
                _current.Value = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}