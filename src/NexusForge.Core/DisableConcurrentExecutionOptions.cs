using System;

namespace NexusForge
{
    public sealed class DisableConcurrentExecutionOptions
    {
        private static readonly object SyncRoot = new object();
        private static DisableConcurrentExecutionOptions _current = new DisableConcurrentExecutionOptions();

        public DistributedLockScope DefaultScope { get; set; } = DistributedLockScope.Global;
        public TenantLockFallbackMode TenantFallbackMode { get; set; } = TenantLockFallbackMode.Throw;

        public static DisableConcurrentExecutionOptions Current
        {
            get
            {
                lock (SyncRoot)
                {
                    return _current;
                }
            }
        }

        public static void SetCurrent(DisableConcurrentExecutionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            lock (SyncRoot)
            {
                _current = options;
            }
        }
    }
}
