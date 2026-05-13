// This file is part of Hangfire. Copyright © 2022 NexusForge OÜ.
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

namespace NexusForge
{
    internal static class ExceptionTypeHelper
    {
#if !NETSTANDARD1_3
        private static readonly Type StackOverflowType = typeof(StackOverflowException);
#endif
        private static readonly Type OutOfMemoryType = typeof(OutOfMemoryException);
 
        internal static bool IsCatchableExceptionType(this Exception e)
        {
            var type = e.GetType();
            return
#if !NETSTANDARD1_3
                type != StackOverflowType &&
#endif
                type != OutOfMemoryType;
        }
    }
}