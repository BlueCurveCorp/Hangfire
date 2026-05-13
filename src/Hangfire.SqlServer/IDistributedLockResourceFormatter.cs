namespace Hangfire.SqlServer
{
    internal interface IDistributedLockResourceFormatter
    {
        string FormatGlobal(string schemaName, string resource);
        string FormatTenant(string schemaName, string tenantId, string resource);
    }
}
