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

using NexusForge.Annotations;
using NexusForge.Common;

namespace NexusForge
{
    public interface IRecurringJobManagerV2 : IRecurringJobManager
    {
        [NotNull]
        JobStorage Storage { get; }

        [CanBeNull]
        string TriggerJob([NotNull] string recurringJobId);
    }

    public interface IRecurringJobManager
    {
        void AddOrUpdate(
            [NotNull] string recurringJobId, 
            [NotNull] Job job, 
            [NotNull] string cronExpression, 
            [NotNull] RecurringJobOptions options);

        void Trigger([NotNull] string recurringJobId);
        void RemoveIfExists([NotNull] string recurringJobId);
    }
}