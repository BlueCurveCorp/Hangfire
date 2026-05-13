using NexusForge.Dashboard;

namespace NexusForge.Core.Tests.Stubs
{
    class DashboardContextStub : DashboardContext
    {
        public DashboardContextStub(DashboardOptions options) : base(new JobStorageStub(), options)
        {
            Response = new DashboardResponseStub();
        }
    }
}
