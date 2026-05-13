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
using System.Globalization;
using NexusForge.Common;
using NexusForge.States;

namespace NexusForge
{
    public sealed class StatisticsHistoryAttribute : JobFilterAttribute, IElectStateFilter
    {
        public StatisticsHistoryAttribute()
        {
            Order = 30;
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState.Name == SucceededState.StateName)
            {
                context.Transaction.IncrementCounter(
                    $"stats:succeeded:{DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}",
                    DateTime.UtcNow.AddMonths(1) - DateTime.UtcNow);

                context.Transaction.IncrementCounter(
                    $"stats:succeeded:{DateTime.UtcNow.ToString("yyyy-MM-dd-HH", CultureInfo.InvariantCulture)}",
                    TimeSpan.FromDays(1));
            }
            else if (context.CandidateState.Name == FailedState.StateName)
            {
                context.Transaction.IncrementCounter(
                    $"stats:failed:{DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}",
                    DateTime.UtcNow.AddMonths(1) - DateTime.UtcNow);

                context.Transaction.IncrementCounter(
                    $"stats:failed:{DateTime.UtcNow.ToString("yyyy-MM-dd-HH", CultureInfo.InvariantCulture)}",
                    TimeSpan.FromDays(1));
            }
            else if (context.CandidateState is DeletedState)
            {
                context.Transaction.IncrementCounter(
                    $"stats:deleted:{DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}",
                    DateTime.UtcNow.AddMonths(1) - DateTime.UtcNow);

                context.Transaction.IncrementCounter(
                    $"stats:deleted:{DateTime.UtcNow.ToString("yyyy-MM-dd-HH", CultureInfo.InvariantCulture)}",
                    TimeSpan.FromDays(1));
            }
        }
    }
}
