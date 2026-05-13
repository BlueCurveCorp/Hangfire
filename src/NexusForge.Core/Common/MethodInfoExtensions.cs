// This file is part of NexusForge. Copyright © 2017 NexusForge OÜ.
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

using System.Linq;
using System.Reflection;

namespace NexusForge.Common
{
    internal static class MethodInfoExtensions
    {
        public static string GetNormalizedName(this MethodInfo methodInfo)
        {
            // Method names containing '.' are considered explicitly implemented interface methods
            // https://stackoverflow.com/a/17854048/1398672
            return methodInfo.Name.Contains('.') && methodInfo.IsFinal && methodInfo.IsPrivate
                ? methodInfo.Name.Split('.').Last()
                : methodInfo.Name;
        }
    }
}