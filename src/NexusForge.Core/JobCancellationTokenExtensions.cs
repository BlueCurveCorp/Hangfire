// This file is part of NexusForge. Copyright © 2019 NexusForge OÜ.
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

using NexusForge.Server;

namespace NexusForge
{
    internal static class JobCancellationTokenExtensions
    {
        public static bool IsAborted(this IJobCancellationToken jobCancellationToken)
        {
            if (jobCancellationToken is ServerJobCancellationToken serverJobCancellationToken)
            {
                // for ServerJobCancellationToken we may simply check IsAborted property
                // to prevent unnecessary creation of the linked CancellationTokenSource
                return serverJobCancellationToken.IsAborted;
            }
            
            return false;
        }
    }
}
