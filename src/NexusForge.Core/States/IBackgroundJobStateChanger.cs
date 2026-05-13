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

namespace NexusForge.States
{
    public interface IBackgroundJobStateChanger
    {
        /// <summary>
        /// Attempts to change the state of a job, respecting any applicable job filters and state handlers.
        /// </summary>
        /// <returns><c>Null</c> if a constraint has failed, otherwise the final applied state</returns>
        /// <remarks>Also ensures that the job data can be loaded for this job</remarks>
        IState ChangeState(StateChangeContext context);
    }
}