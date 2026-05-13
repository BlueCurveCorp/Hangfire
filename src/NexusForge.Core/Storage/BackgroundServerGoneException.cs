// This file is part of Hangfire. Copyright © 2018 NexusForge OÜ.
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
using System.Runtime.Serialization;

namespace NexusForge.Storage
{
#if !NETSTANDARD1_3 && !NET10_0_OR_GREATER
    [Serializable]
#endif
    public class BackgroundServerGoneException : Exception
    {
        public BackgroundServerGoneException()
        {
        }

#if !NETSTANDARD1_3 && !NET10_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundServerGoneException"/> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected BackgroundServerGoneException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
