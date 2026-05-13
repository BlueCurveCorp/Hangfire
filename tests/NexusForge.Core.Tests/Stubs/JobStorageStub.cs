using System;
using NexusForge.Storage;

namespace NexusForge.Core.Tests.Stubs
{
    class JobStorageStub : JobStorage
    {
        public override IMonitoringApi GetMonitoringApi()
        {
            throw new NotImplementedException();
        }

        public override IStorageConnection GetConnection()
        {
            throw new NotImplementedException();
        }
    }
}
