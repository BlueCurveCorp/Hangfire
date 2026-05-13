using System;
using System.Diagnostics.CodeAnalysis;
using NexusForge.Common;
using NexusForge.Server;
using NexusForge.Storage;
using Moq;
using Xunit;

namespace NexusForge.Core.Tests
{
    public class DisableConcurrentExecutionAttributeFacts : IDisposable
    {
        private const string DefaultResource = "DisableConcurrentExecutionAttributeFacts.SampleJob";

        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IDisposable> _lock;
        private readonly PerformingContext _context;

        public DisableConcurrentExecutionAttributeFacts()
        {
            DisableConcurrentExecutionOptions.SetCurrent(new DisableConcurrentExecutionOptions());

            _lock = new Mock<IDisposable>();
            _connection = new Mock<IStorageConnection>();
            _connection
                .Setup(x => x.AcquireDistributedLock(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(_lock.Object);
            _connection
                .Setup(x => x.AcquireTenantDistributedLock(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TenantLockFallbackMode>()))
                .Returns(_lock.Object);

            var backgroundJob = new BackgroundJob(
                "job-id",
                Job.FromExpression(() => SampleJob()),
                DateTime.UtcNow);

            _context = new PerformingContext(new PerformContext(
                _connection.Object,
                backgroundJob,
                Mock.Of<IJobCancellationToken>()));
        }

        public void Dispose()
        {
            DisableConcurrentExecutionOptions.SetCurrent(new DisableConcurrentExecutionOptions());
        }

        [Fact]
        public void OnPerforming_UsesGlobalLock_ByDefault()
        {
            var attribute = new DisableConcurrentExecutionAttribute(10);

            attribute.OnPerforming(_context);

            _connection.Verify(x => x.AcquireDistributedLock(
                DefaultResource,
                TimeSpan.FromSeconds(10)));
            _connection.Verify(x => x.AcquireTenantDistributedLock(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TenantLockFallbackMode>()), Times.Never);
        }

        [Fact]
        public void OnPerforming_UsesTenantLock_WhenAttributeScopeIsTenant()
        {
            var attribute = new DisableConcurrentExecutionAttribute(10) { Scope = DistributedLockScope.Tenant };

            using (NexusForgeTenantContext.Use("tenant-a"))
            {
                attribute.OnPerforming(_context);
            }

            _connection.Verify(x => x.AcquireTenantDistributedLock(
                "tenant-a",
                DefaultResource,
                TimeSpan.FromSeconds(10),
                TenantLockFallbackMode.Throw));
        }

        [Fact]
        public void OnPerforming_UsesTenantLock_WhenDefaultScopeIsTenant()
        {
            DisableConcurrentExecutionOptions.SetCurrent(new DisableConcurrentExecutionOptions
            {
                DefaultScope = DistributedLockScope.Tenant,
                TenantFallbackMode = TenantLockFallbackMode.Global
            });

            var attribute = new DisableConcurrentExecutionAttribute(10);

            using (NexusForgeTenantContext.Use("tenant-a"))
            {
                attribute.OnPerforming(_context);
            }

            _connection.Verify(x => x.AcquireTenantDistributedLock(
                "tenant-a",
                DefaultResource,
                TimeSpan.FromSeconds(10),
                TenantLockFallbackMode.Global));
        }

        [Fact]
        public void OnPerforming_ExplicitGlobalScope_OverridesTenantDefaultScope()
        {
            DisableConcurrentExecutionOptions.SetCurrent(new DisableConcurrentExecutionOptions
            {
                DefaultScope = DistributedLockScope.Tenant
            });

            var attribute = new DisableConcurrentExecutionAttribute(10) { Scope = DistributedLockScope.Global };

            using (NexusForgeTenantContext.Use("tenant-a"))
            {
                attribute.OnPerforming(_context);
            }

            _connection.Verify(x => x.AcquireDistributedLock(
                DefaultResource,
                TimeSpan.FromSeconds(10)));
            _connection.Verify(x => x.AcquireTenantDistributedLock(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TenantLockFallbackMode>()), Times.Never);
        }

        [Fact]
        public void OnPerforming_Throws_WhenTenantScopeRequestedWithoutTenantContext()
        {
            var attribute = new DisableConcurrentExecutionAttribute(10) { Scope = DistributedLockScope.Tenant };

            var exception = Assert.Throws<InvalidOperationException>(() => attribute.OnPerforming(_context));

            Assert.Contains("NexusForgeTenantContext.CurrentTenantId", exception.Message);
        }

        [Fact]
        public void OnPerformed_DisposesAcquiredLock()
        {
            var attribute = new DisableConcurrentExecutionAttribute(10);
            attribute.OnPerforming(_context);

            attribute.OnPerformed(new PerformedContext(_context, null, false, null));

            _lock.Verify(x => x.Dispose());
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void SampleJob()
        {
        }
    }
}
