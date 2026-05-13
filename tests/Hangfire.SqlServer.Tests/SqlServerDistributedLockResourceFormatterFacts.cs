using System;
using System.Linq;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerDistributedLockResourceFormatterFacts
    {
        private readonly SqlServerDistributedLockResourceFormatter _formatter = new SqlServerDistributedLockResourceFormatter();

        [Fact]
        public void FormatGlobal_PrefixesResourceWithSchema()
        {
            var result = _formatter.FormatGlobal("HangFire", "MyService.Import");

            Assert.Equal("HangFire:MyService.Import", result);
        }

        [Fact]
        public void FormatTenant_PrefixesResourceWithSchemaAndTenant()
        {
            var result = _formatter.FormatTenant("HangFire", "tenant-a", "MyService.Import");

            Assert.Equal("HangFire:tenant:tenant-a:MyService.Import", result);
        }

        [Fact]
        public void FormatTenant_ValidatesTenantId()
        {
            Assert.Throws<ArgumentException>(() => _formatter.FormatTenant("HangFire", "Tenant-A", "resource"));
        }

        [Fact]
        public void FormatTenant_PreservesResourceCasing()
        {
            var result = _formatter.FormatTenant("HangFire", "tenant-a", "CaseSensitiveResource");

            Assert.EndsWith(":CaseSensitiveResource", result);
        }

        [Fact]
        public void FormatTenant_ShortensLongResources()
        {
            var result = _formatter.FormatTenant("HangFire", "tenant-a", new string('a', 300));

            Assert.True(result.Length <= SqlServerDistributedLockResourceFormatter.MaxResourceLength);
            Assert.Contains(":sha256:", result);
        }

        [Fact]
        public void FormatTenant_UsesDifferentHashesForDifferentLongResources()
        {
            var prefix = new string('a', 300);

            var first = _formatter.FormatTenant("HangFire", "tenant-a", prefix + "1");
            var second = _formatter.FormatTenant("HangFire", "tenant-a", prefix + "2");

            Assert.NotEqual(first, second);
            Assert.Equal(64, first.Split(new[] { ":sha256:" }, StringSplitOptions.None).Last().Length);
            Assert.Equal(64, second.Split(new[] { ":sha256:" }, StringSplitOptions.None).Last().Length);
        }
    }
}
