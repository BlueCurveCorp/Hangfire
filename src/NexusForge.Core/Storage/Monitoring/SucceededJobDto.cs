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
using System.Collections.Generic;
using NexusForge.Common;

namespace NexusForge.Storage.Monitoring
{
    public class SucceededJobDto
    {
        public SucceededJobDto()
        {
            InSucceededState = true;
        }

        public Job Job { get; set; }
        public JobLoadException LoadException { get; set; }
        public InvocationData InvocationData { get; set; }
        public object Result { get; set; }
        public long? TotalDuration { get; set; }
        public DateTime? SucceededAt { get; set; }
        public bool InSucceededState { get; set; }
        public IDictionary<string, string> StateData { get; set; }
    }
}