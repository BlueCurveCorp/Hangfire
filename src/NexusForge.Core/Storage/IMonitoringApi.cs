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
using System.Collections.Generic;
using NexusForge.Storage.Monitoring;

namespace NexusForge.Storage
{
    public interface IMonitoringApi
    {
        IList<QueueWithTopEnqueuedJobsDto> Queues();
        IList<QueueAvailabilityDto> QueueAvailability();
        IList<ServerDto> Servers();
        IList<ServerResourceEvent> ResourceEvents(string serverId, int from, int count);
        IList<ServerResourceEvent> ResourceEvents(DateTime from, DateTime to);
        JobDetailsDto JobDetails(string jobId);
        StatisticsDto GetStatistics();

        JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage);
        JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage);

        JobList<ProcessingJobDto> ProcessingJobs(int from, int count);
        JobList<ScheduledJobDto> ScheduledJobs(int from, int count);
        JobList<SucceededJobDto> SucceededJobs(int from, int count);
        JobList<FailedJobDto> FailedJobs(int from, int count);
        JobList<DeletedJobDto> DeletedJobs(int from, int count);

        long ScheduledCount();
        long EnqueuedCount(string queue);
        long FetchedCount(string queue);
        long FailedCount();
        long ProcessingCount();

        long SucceededListCount();
        long DeletedListCount();
        
        IDictionary<DateTime, long> SucceededByDatesCount();
        IDictionary<DateTime, long> FailedByDatesCount();
        IDictionary<DateTime, long> HourlySucceededJobs();
        IDictionary<DateTime, long> HourlyFailedJobs();
    }
}
