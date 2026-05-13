extern alias ReferencedDapper;

using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using ReferencedDapper::Dapper;
using Xunit.Sdk;

namespace NexusForge.SqlServer.Tests
{
    public class CleanDatabaseAttribute : BeforeAfterTestAttribute
    {
        private static readonly object GlobalLock = new object();
        private static bool _sqlObjectInstalled;
        
        public CleanDatabaseAttribute()
        {
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            Monitor.Enter(GlobalLock);

            if (!_sqlObjectInstalled)
            {
                CreateAndInitializeDatabaseIfNotExists();
                _sqlObjectInstalled = true;
            }

            using (var connection = new SqlConnection(
                ConnectionUtils.GetConnectionString()))
            {
                connection.Execute(@"
truncate table [NexusForge].[AggregatedCounter];
truncate table [NexusForge].[Counter];
truncate table [NexusForge].[Hash];
delete from [NexusForge].[Job];
dbcc checkident('NexusForge.Job', RESEED, 0);
truncate table [NexusForge].[List];
truncate table [NexusForge].[JobQueue];
truncate table [NexusForge].[Set];
truncate table [NexusForge].[Server];
");
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Monitor.Exit(GlobalLock);
        }

        private static void CreateAndInitializeDatabaseIfNotExists()
        {
            var recreateDatabaseSql = String.Format(
                @"if db_id('{0}') is null create database [{0}] COLLATE SQL_Latin1_General_CP1_CS_AS",
                ConnectionUtils.GetDatabaseName());

            using (var connection = new SqlConnection(
                ConnectionUtils.GetMasterConnectionString()))
            {
                connection.Execute(recreateDatabaseSql);
            }

            using (var connection = new SqlConnection(
                ConnectionUtils.GetConnectionString()))
            {
                SqlServerObjectsInstaller.Install(connection, null, true);
            }
        }
    }
}
