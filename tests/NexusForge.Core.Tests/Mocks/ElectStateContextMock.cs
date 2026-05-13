using System;
using NexusForge.States;

namespace NexusForge.Core.Tests
{
    public class ElectStateContextMock
    {
        private readonly Lazy<ElectStateContext> _context;

        public ElectStateContextMock()
        {
            ApplyContext = new ApplyStateContextMock();

            _context = new Lazy<ElectStateContext>(
                () => new ElectStateContext(ApplyContext.Object));
        }

        public ApplyStateContextMock ApplyContext { get; set; }

        public ElectStateContext Object => _context.Value;
    }
}
