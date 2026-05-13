using System;
using Xunit;
using Xunit.Sdk;

namespace NexusForge.Core.Tests
{
    [XunitTestCaseDiscoverer("NexusForge.Core.Tests.DataCompatibilityRangeTheoryDiscoverer", "NexusForge.Core.Tests")]
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class DataCompatibilityRangeTheoryAttribute : TheoryAttribute
    {
        public DataCompatibilityRangeTheoryAttribute()
        {
            MinLevel = DataCompatibilityRangeFactAttribute.PossibleMinLevel;
            MaxExcludingLevel = DataCompatibilityRangeFactAttribute.PossibleMaxExcludingLevel;
        }

        public CompatibilityLevel MinLevel { get; set; }
        public CompatibilityLevel MaxExcludingLevel { get; set; }
    }
}