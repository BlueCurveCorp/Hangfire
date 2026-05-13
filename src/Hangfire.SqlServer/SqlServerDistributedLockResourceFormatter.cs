using System;
using System.Security.Cryptography;
using System.Text;

namespace Hangfire.SqlServer
{
    internal sealed class SqlServerDistributedLockResourceFormatter : IDistributedLockResourceFormatter
    {
        internal const int MaxResourceLength = 255;
        private const string TenantMarker = "tenant";
        private const string HashMarker = ":sha256:";
        private const int HashHexLength = 64;

        public string FormatGlobal(string schemaName, string resource)
        {
            if (schemaName == null) throw new ArgumentNullException(nameof(schemaName));
            if (String.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException(nameof(resource));

            return ShortenIfNeeded($"{schemaName}:{resource}");
        }

        public string FormatTenant(string schemaName, string tenantId, string resource)
        {
            if (schemaName == null) throw new ArgumentNullException(nameof(schemaName));
            if (tenantId == null) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException(nameof(resource));

            TenantIdValidator.Validate(nameof(tenantId), tenantId);

            return ShortenIfNeeded($"{schemaName}:{TenantMarker}:{tenantId}:{resource}");
        }

        private static string ShortenIfNeeded(string formattedResource)
        {
            if (formattedResource.Length <= MaxResourceLength)
            {
                return formattedResource;
            }

            var hash = ComputeSha256Hex(formattedResource);
            var suffix = HashMarker + hash;
            var prefixLength = MaxResourceLength - suffix.Length;

            return formattedResource.Substring(0, prefixLength) + suffix;
        }

        private static string ComputeSha256Hex(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(HashHexLength);

                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
