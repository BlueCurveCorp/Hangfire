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
using NexusForge.States;

namespace NexusForge
{
    public class RecurringJobOptions
    {
        private TimeZoneInfo _timeZone;
        private string _queueName;

        public RecurringJobOptions()
        {
            TimeZone = TimeZoneInfo.Utc;
#pragma warning disable 618
            QueueName = EnqueuedState.DefaultQueue;
#pragma warning restore 618
            MisfireHandling = MisfireHandlingMode.Relaxed;
        }

        [NotNull]
        public TimeZoneInfo TimeZone
        {
            get { return _timeZone; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));

                _timeZone = value;
            }
        }

        [Obsolete("Please use non-obsolete AddOrUpdate with the explicit `queue` parameter instead. Will be removed in 2.0.0.")]
        [NotNull]
        public string QueueName
        {
            get { return _queueName; }
            set
            {
                EnqueuedState.ValidateQueueName(nameof(value), value);
                _queueName = value;
            }
        }

        public MisfireHandlingMode MisfireHandling { get; set; }
    }
}
