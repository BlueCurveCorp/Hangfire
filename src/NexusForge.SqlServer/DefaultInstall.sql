
-- This file is part of NexusForge. Copyright © 2013-2014 NexusForge OÜ.
-- 
-- NexusForge is free software: you can redistribute it and/or modify
-- it under the terms of the GNU Lesser General Public License as 
-- published by the Free Software Foundation, either version 3 
-- of the License, or any later version.
-- 
-- NexusForge is distributed in the hope that it will be useful,
-- but WITHOUT ANY WARRANTY; without even the implied warranty of
-- MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
-- GNU Lesser General Public License for more details.
-- 
-- You should have received a copy of the GNU Lesser General Public 
-- License along with NexusForge. If not, see <http://www.gnu.org/licenses/>.

SET NOCOUNT ON
SET XACT_ABORT ON
DECLARE @TARGET_SCHEMA_VERSION INT;
DECLARE @DISABLE_HEAVY_MIGRATIONS BIT;
SET @TARGET_SCHEMA_VERSION = 9;
--SET @DISABLE_HEAVY_MIGRATIONS = 1;

PRINT 'Installing NexusForge SQL objects...';

BEGIN TRANSACTION;

-- Acquire exclusive lock to prevent deadlocks caused by schema creation / version update
DECLARE @SchemaLockResult INT;
EXEC @SchemaLockResult = sp_getapplock @Resource = 'NexusForge:SchemaLock', @LockMode = 'Exclusive'

-- Create the database schema if it doesn't exists
IF NOT EXISTS (SELECT [schema_id] FROM [sys].[schemas] WHERE [name] = 'NexusForge')
BEGIN
    EXEC (N'CREATE SCHEMA [NexusForge]');
    PRINT 'Created database schema [NexusForge]';
END
ELSE
    PRINT 'Database schema [NexusForge] already exists';
    
DECLARE @SCHEMA_ID int;
SELECT @SCHEMA_ID = [schema_id] FROM [sys].[schemas] WHERE [name] = 'NexusForge';

-- Create the [NexusForge].Schema table if not exists
IF NOT EXISTS(SELECT [object_id] FROM [sys].[tables] 
    WHERE [name] = 'Schema' AND [schema_id] = @SCHEMA_ID)
BEGIN
    CREATE TABLE [NexusForge].[Schema](
        [Version] [int] NOT NULL,
        CONSTRAINT [PK_NexusForge_Schema] PRIMARY KEY CLUSTERED ([Version] ASC)
    );
    PRINT 'Created table [NexusForge].[Schema]';
END
ELSE
    PRINT 'Table [NexusForge].[Schema] already exists';
    
DECLARE @CURRENT_SCHEMA_VERSION int;
SELECT @CURRENT_SCHEMA_VERSION = [Version] FROM [NexusForge].[Schema];

PRINT 'Current NexusForge schema version: ' + CASE WHEN @CURRENT_SCHEMA_VERSION IS NULL THEN 'none' ELSE CONVERT(nvarchar, @CURRENT_SCHEMA_VERSION) END;

IF @CURRENT_SCHEMA_VERSION IS NOT NULL AND @CURRENT_SCHEMA_VERSION > @TARGET_SCHEMA_VERSION
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'NexusForge current database schema version ' + CAST(@CURRENT_SCHEMA_VERSION AS NVARCHAR) +
          ' is newer than the configured SqlServerStorage schema version ' + CAST(@TARGET_SCHEMA_VERSION AS NVARCHAR) +
          '. Will not apply any migrations.';
    RETURN;
END

-- Install [NexusForge] schema objects
IF @CURRENT_SCHEMA_VERSION IS NULL
BEGIN
    IF @DISABLE_HEAVY_MIGRATIONS = 1
    BEGIN
        SET @DISABLE_HEAVY_MIGRATIONS = 0;
        PRINT 'Enabling HEAVY_MIGRATIONS, because we are installing objects from scratch';
    END

    PRINT 'Installing schema version 1';
        
    -- Create job tables
    CREATE TABLE [NexusForge].[Job] (
        [Id] [int] IDENTITY(1,1) NOT NULL,
		[StateId] [int] NULL,
		[StateName] [nvarchar](20) NULL, -- To speed-up queries.
        [InvocationData] [nvarchar](max) NOT NULL,
        [Arguments] [nvarchar](max) NOT NULL,
        [CreatedAt] [datetime] NOT NULL,
        [ExpireAt] [datetime] NULL,

        CONSTRAINT [PK_NexusForge_Job] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [NexusForge].[Job]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Job_StateName] ON [NexusForge].[Job] ([StateName] ASC);
	PRINT 'Created index [IX_NexusForge_Job_StateName]';
        
    -- Job history table
        
    CREATE TABLE [NexusForge].[State] (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [JobId] [int] NOT NULL,
		[Name] [nvarchar](20) NOT NULL,
		[Reason] [nvarchar](100) NULL,
        [CreatedAt] [datetime] NOT NULL,
        [Data] [nvarchar](max) NULL,
            
        CONSTRAINT [PK_NexusForge_State] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [NexusForge].[State]';

    ALTER TABLE [NexusForge].[State] ADD CONSTRAINT [FK_NexusForge_State_Job] FOREIGN KEY([JobId])
        REFERENCES [NexusForge].[Job] ([Id])
        ON UPDATE CASCADE
        ON DELETE CASCADE;
    PRINT 'Created constraint [FK_NexusForge_State_Job]';
        
    CREATE NONCLUSTERED INDEX [IX_NexusForge_State_JobId] ON [NexusForge].[State] ([JobId] ASC);
    PRINT 'Created index [IX_NexusForge_State_JobId]';
        
    -- Job parameters table
        
    CREATE TABLE [NexusForge].[JobParameter](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [JobId] [int] NOT NULL,
        [Name] [nvarchar](40) NOT NULL,
        [Value] [nvarchar](max) NULL,
            
        CONSTRAINT [PK_NexusForge_JobParameter] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [NexusForge].[JobParameter]';

    ALTER TABLE [NexusForge].[JobParameter] ADD CONSTRAINT [FK_NexusForge_JobParameter_Job] FOREIGN KEY([JobId])
        REFERENCES [NexusForge].[Job] ([Id])
        ON UPDATE CASCADE
        ON DELETE CASCADE;
    PRINT 'Created constraint [FK_NexusForge_JobParameter_Job]';
        
    CREATE NONCLUSTERED INDEX [IX_NexusForge_JobParameter_JobIdAndName] ON [NexusForge].[JobParameter] (
        [JobId] ASC,
        [Name] ASC
    );
    PRINT 'Created index [IX_NexusForge_JobParameter_JobIdAndName]';
        
    -- Job queue table
        
    CREATE TABLE [NexusForge].[JobQueue](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [JobId] [int] NOT NULL,
        [TenantId] [nvarchar](100) NULL,
        [Queue] [nvarchar](20) NOT NULL,
        [FetchedAt] [datetime] NULL,
            
        CONSTRAINT [PK_NexusForge_JobQueue] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [NexusForge].[JobQueue]';
        
    CREATE NONCLUSTERED INDEX [IX_NexusForge_JobQueue_JobIdAndQueue] ON [NexusForge].[JobQueue] (
        [JobId] ASC,
        [Queue] ASC
    );
    PRINT 'Created index [IX_NexusForge_JobQueue_JobIdAndQueue]';
        
    CREATE NONCLUSTERED INDEX [IX_NexusForge_JobQueue_QueueAndFetchedAt] ON [NexusForge].[JobQueue] (
        [Queue] ASC,
        [FetchedAt] ASC
    );
    PRINT 'Created index [IX_NexusForge_JobQueue_QueueAndFetchedAt]';

    -- Servers table
        
    CREATE TABLE [NexusForge].[Server](
        [Id] [nvarchar](200) NOT NULL,
        [Data] [nvarchar](max) NULL,
        [LastHeartbeat] [datetime] NULL,
            
        CONSTRAINT [PK_NexusForge_Server] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [NexusForge].[Server]';
        
    -- Extension tables
        
    CREATE TABLE [NexusForge].[Hash](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Name] [nvarchar](40) NOT NULL,
        [StringValue] [nvarchar](max) NULL,
        [IntValue] [int] NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_NexusForge_Hash] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [NexusForge].[Hash]';
        
    CREATE UNIQUE NONCLUSTERED INDEX [UX_NexusForge_Hash_KeyAndName] ON [NexusForge].[Hash] (
        [Key] ASC,
        [Name] ASC
    );
    PRINT 'Created index [UX_NexusForge_Hash_KeyAndName]';
        
    CREATE TABLE [NexusForge].[List](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Value] [nvarchar](max) NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_NexusForge_List] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [NexusForge].[List]';
        
    CREATE TABLE [NexusForge].[Set](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Score] [float] NOT NULL,
        [Value] [nvarchar](256) NOT NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_NexusForge_Set] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [NexusForge].[Set]';
        
    CREATE UNIQUE NONCLUSTERED INDEX [UX_NexusForge_Set_KeyAndValue] ON [NexusForge].[Set] (
        [Key] ASC,
        [Value] ASC
    );
    PRINT 'Created index [UX_NexusForge_Set_KeyAndValue]';
        
    CREATE TABLE [NexusForge].[Value](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [StringValue] [nvarchar](max) NULL,
        [IntValue] [int] NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_NexusForge_Value] PRIMARY KEY CLUSTERED (
            [Id] ASC
        )
    );
    PRINT 'Created table [NexusForge].[Value]';
        
    CREATE UNIQUE NONCLUSTERED INDEX [UX_NexusForge_Value_Key] ON [NexusForge].[Value] (
        [Key] ASC
    );
    PRINT 'Created index [UX_NexusForge_Value_Key]';

	CREATE TABLE [NexusForge].[Counter](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Key] [nvarchar](100) NOT NULL,
		[Value] [tinyint] NOT NULL,
		[ExpireAt] [datetime] NULL,

		CONSTRAINT [PK_NexusForge_Counter] PRIMARY KEY CLUSTERED ([Id] ASC)
	);
	PRINT 'Created table [NexusForge].[Counter]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Counter_Key] ON [NexusForge].[Counter] ([Key] ASC)
	INCLUDE ([Value]);
	PRINT 'Created index [IX_NexusForge_Counter_Key]';

	SET @CURRENT_SCHEMA_VERSION = 1;
END

IF @CURRENT_SCHEMA_VERSION = 1
BEGIN
	PRINT 'Installing schema version 2';

	-- https://github.com/odinserj/NexusForge/issues/83

	DROP INDEX [IX_NexusForge_Counter_Key] ON [NexusForge].[Counter];

	ALTER TABLE [NexusForge].[Counter] ALTER COLUMN [Value] SMALLINT NOT NULL;

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Counter_Key] ON [NexusForge].[Counter] ([Key] ASC)
	INCLUDE ([Value]);
	PRINT 'Index [IX_NexusForge_Counter_Key] re-created';

	DROP TABLE [NexusForge].[Value];
	DROP TABLE [NexusForge].[Hash];
	PRINT 'Dropped tables [NexusForge].[Value] and [NexusForge].[Hash]'

	DELETE FROM [NexusForge].[Server] WHERE [LastHeartbeat] IS NULL;
	ALTER TABLE [NexusForge].[Server] ALTER COLUMN [LastHeartbeat] DATETIME NOT NULL;

	SET @CURRENT_SCHEMA_VERSION = 2;
END

IF @CURRENT_SCHEMA_VERSION = 2
BEGIN
	PRINT 'Installing schema version 3';

	DROP INDEX [IX_NexusForge_JobQueue_JobIdAndQueue] ON [NexusForge].[JobQueue];
	PRINT 'Dropped index [IX_NexusForge_JobQueue_JobIdAndQueue]';

	CREATE TABLE [NexusForge].[Hash](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Key] [nvarchar](100) NOT NULL,
		[Field] [nvarchar](100) NOT NULL,
		[Value] [nvarchar](max) NULL,
		[ExpireAt] [datetime2](7) NULL,
		
		CONSTRAINT [PK_NexusForge_Hash] PRIMARY KEY CLUSTERED ([Id] ASC)
	);
	PRINT 'Created table [NexusForge].[Hash]';

	CREATE UNIQUE NONCLUSTERED INDEX [UX_NexusForge_Hash_Key_Field] ON [NexusForge].[Hash] (
		[Key] ASC,
		[Field] ASC
	);
	PRINT 'Created index [UX_NexusForge_Hash_Key_Field]';

	SET @CURRENT_SCHEMA_VERSION = 3;
END

IF @CURRENT_SCHEMA_VERSION = 3
BEGIN
	PRINT 'Installing schema version 4';

	CREATE TABLE [NexusForge].[AggregatedCounter] (
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Key] [nvarchar](100) NOT NULL,
		[Value] [bigint] NOT NULL,
		[ExpireAt] [datetime] NULL,

		CONSTRAINT [PK_NexusForge_CounterAggregated] PRIMARY KEY CLUSTERED ([Id] ASC)
	);
	PRINT 'Created table [NexusForge].[AggregatedCounter]';

	CREATE UNIQUE NONCLUSTERED INDEX [UX_NexusForge_CounterAggregated_Key] ON [NexusForge].[AggregatedCounter] (
		[Key] ASC
	) INCLUDE ([Value]);
	PRINT 'Created index [UX_NexusForge_CounterAggregated_Key]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Hash_ExpireAt] ON [NexusForge].[Hash] ([ExpireAt])
	INCLUDE ([Id]);

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Job_ExpireAt] ON [NexusForge].[Job] ([ExpireAt])
	INCLUDE ([Id]);

	CREATE NONCLUSTERED INDEX [IX_NexusForge_List_ExpireAt] ON [NexusForge].[List] ([ExpireAt])
	INCLUDE ([Id]);

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Set_ExpireAt] ON [NexusForge].[Set] ([ExpireAt])
	INCLUDE ([Id]);

	PRINT 'Created indexes for [ExpireAt] columns';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Hash_Key] ON [NexusForge].[Hash] ([Key] ASC)
	INCLUDE ([ExpireAt]);
	PRINT 'Created index [IX_NexusForge_Hash_Key]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_List_Key] ON [NexusForge].[List] ([Key] ASC)
	INCLUDE ([ExpireAt], [Value]);
	PRINT 'Created index [IX_NexusForge_List_Key]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Set_Key] ON [NexusForge].[Set] ([Key] ASC)
	INCLUDE ([ExpireAt], [Value]);
	PRINT 'Created index [IX_NexusForge_Set_Key]';

	SET @CURRENT_SCHEMA_VERSION = 4;
END

IF @CURRENT_SCHEMA_VERSION = 4
BEGIN
	PRINT 'Installing schema version 5';

	DROP INDEX [IX_NexusForge_JobQueue_QueueAndFetchedAt] ON [NexusForge].[JobQueue];
	PRINT 'Dropped index [IX_NexusForge_JobQueue_QueueAndFetchedAt] to modify the [NexusForge].[JobQueue].[Queue] column';

	ALTER TABLE [NexusForge].[JobQueue] ALTER COLUMN [Queue] NVARCHAR (50) NOT NULL;
	PRINT 'Modified [NexusForge].[JobQueue].[Queue] length to 50';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_JobQueue_QueueAndFetchedAt] ON [NexusForge].[JobQueue] (
        [Queue] ASC,
        [FetchedAt] ASC
    );
    PRINT 'Re-created index [IX_NexusForge_JobQueue_QueueAndFetchedAt]';

	ALTER TABLE [NexusForge].[Server] DROP CONSTRAINT [PK_NexusForge_Server]
    PRINT 'Dropped constraint [PK_NexusForge_Server] to modify the [NexusForge].[Server].[Id] column';

	ALTER TABLE [NexusForge].[Server] ALTER COLUMN [Id] NVARCHAR (200) NOT NULL;
	PRINT 'Modified [NexusForge].[Server].[Id] length to 200';

	ALTER TABLE [NexusForge].[Server] ADD  CONSTRAINT [PK_NexusForge_Server] PRIMARY KEY CLUSTERED
	(
		[Id] ASC
	);
	PRINT 'Re-created constraint [PK_NexusForge_Server]';

	SET @CURRENT_SCHEMA_VERSION = 5;
END

IF @CURRENT_SCHEMA_VERSION = 5 AND @DISABLE_HEAVY_MIGRATIONS = 1
BEGIN
    PRINT 'Migration process STOPPED at schema version ' + CAST(@CURRENT_SCHEMA_VERSION AS NVARCHAR) +
          '. WILL NOT upgrade to schema version ' + CAST(@TARGET_SCHEMA_VERSION AS NVARCHAR) +
          ', because @DISABLE_HEAVY_MIGRATIONS option is set.';
END
ELSE IF @CURRENT_SCHEMA_VERSION = 5
BEGIN
	PRINT 'Installing schema version 6';

	-- First, we will drop all the secondary indexes on the NexusForge.Set table, because we will
	-- modify that table, and unknown indexes may be added there (see https://github.com/NexusForgeIO/NexusForge/issues/844).
	-- So, we'll drop all of them, and then re-create the required index with a well-known name.

	DECLARE @dropIndexSql NVARCHAR(MAX) = N'';
	SELECT @dropIndexSql += N'DROP INDEX ' + QUOTENAME(SCHEMA_NAME(o.[schema_id])) + '.' + QUOTENAME(o.name) + '.' + QUOTENAME(i.name) + ';'
	FROM sys.indexes AS i
	INNER JOIN sys.tables AS o
	ON i.[object_id] = o.[object_id]
	WHERE i.is_primary_key = 0
	AND i.index_id <> 0
	AND o.is_ms_shipped = 0
	AND SCHEMA_NAME(o.[schema_id]) = 'NexusForge'
	AND o.name = 'Set';

	EXEC sp_executesql @dropIndexSql;
	PRINT 'Dropped all secondary indexes on the [Set] table';

	-- Next, we'll remove the unnecessary indexes. They were unnecessary in the previous schema,
	-- and are unnecessary in the new schema as well. We'll not re-create them.

	DROP INDEX [IX_NexusForge_Hash_Key] ON [NexusForge].[Hash];
	PRINT 'Dropped unnecessary index [IX_NexusForge_Hash_Key]';

	-- Next, all the indexes that cover expiration will be filtered, to include only non-null values. This
	-- will prevent unnecessary index modifications – we are seeking these indexes only for non-null
	-- expiration time. Also, they include the Id column by a mistake. So we'll re-create them later in the
	-- migration.

	DROP INDEX [IX_NexusForge_Hash_ExpireAt] ON [NexusForge].[Hash];
	PRINT 'Dropped index [IX_NexusForge_Hash_ExpireAt]';

	DROP INDEX [IX_NexusForge_Job_ExpireAt] ON [NexusForge].[Job];
	PRINT 'Dropped index [IX_NexusForge_Job_ExpireAt]';

	DROP INDEX [IX_NexusForge_List_ExpireAt] ON [NexusForge].[List];
	PRINT 'Dropped index [IX_NexusForge_List_ExpireAt]';

	-- IX_NexusForge_Job_StateName index can also be optimized, since we are querying it only with a
	-- non-null state name. This will decrease the number of operations, when creating a background job.
	-- It will be recreated later in the migration.

	DROP INDEX [IX_NexusForge_Job_StateName] ON [NexusForge].Job;
	PRINT 'Dropped index [IX_NexusForge_Job_StateName]';

	-- Dropping foreign key constraints based on the JobId column, because we need to modify the underlying
	-- column type of the clustered index to BIGINT. We'll recreate them later in the migration.

	ALTER TABLE [NexusForge].[JobParameter] DROP CONSTRAINT [FK_NexusForge_JobParameter_Job];
	PRINT 'Dropped constraint [FK_NexusForge_JobParameter_Job]';

	ALTER TABLE [NexusForge].[State] DROP CONSTRAINT [FK_NexusForge_State_Job];
	PRINT 'Dropped constraint [FK_NexusForge_State_Job]';

	-- We are going to create composite clustered indexes that are more natural for the following tables,
	-- so the following indexes will be unnecessary. Natural sorting will keep related data close to each
	-- other, and simplify the index modifications by the cost of fragmentation and additional page splits.

	DROP INDEX [UX_NexusForge_CounterAggregated_Key] ON [NexusForge].[AggregatedCounter];
	PRINT 'Dropped index [UX_NexusForge_CounterAggregated_Key]';

	DROP INDEX [IX_NexusForge_Counter_Key] ON [NexusForge].[Counter];
	PRINT 'Dropped index [IX_NexusForge_Counter_Key]';

	DROP INDEX [IX_NexusForge_JobParameter_JobIdAndName] ON [NexusForge].[JobParameter];
	PRINT 'Dropped index [IX_NexusForge_JobParameter_JobIdAndName]';

	DROP INDEX [IX_NexusForge_JobQueue_QueueAndFetchedAt] ON [NexusForge].[JobQueue];
	PRINT 'Dropped index [IX_NexusForge_JobQueue_QueueAndFetchedAt]';

	DROP INDEX [UX_NexusForge_Hash_Key_Field] ON [NexusForge].[Hash];
	PRINT 'Dropped index [UX_NexusForge_Hash_Key_Field]';

	DROP INDEX [IX_NexusForge_List_Key] ON [NexusForge].[List];
	PRINT 'Dropped index [IX_NexusForge_List_Key]';

	DROP INDEX [IX_NexusForge_State_JobId] ON [NexusForge].[State];
	PRINT 'Dropped index [IX_NexusForge_State_JobId]';

	-- Then, we need to drop the primary key constraints, to modify id columns to the BIGINT type. Some of them
	-- will be re-created later in the migration. But some of them would be removed forever, because their
	-- uniqueness property sometimes unnecessary.

	ALTER TABLE [NexusForge].[AggregatedCounter] DROP CONSTRAINT [PK_NexusForge_CounterAggregated];
	PRINT 'Dropped constraint [PK_NexusForge_CounterAggregated]';

	ALTER TABLE [NexusForge].[Counter] DROP CONSTRAINT [PK_NexusForge_Counter];
	PRINT 'Dropped constraint [PK_NexusForge_Counter]';

	ALTER TABLE [NexusForge].[Hash] DROP CONSTRAINT [PK_NexusForge_Hash];
	PRINT 'Dropped constraint [PK_NexusForge_Hash]';

	ALTER TABLE [NexusForge].[Job] DROP CONSTRAINT [PK_NexusForge_Job];
	PRINT 'Dropped constraint [PK_NexusForge_Job]';

	ALTER TABLE [NexusForge].[JobParameter] DROP CONSTRAINT [PK_NexusForge_JobParameter];
	PRINT 'Dropped constraint [PK_NexusForge_JobParameter]';

	ALTER TABLE [NexusForge].[JobQueue] DROP CONSTRAINT [PK_NexusForge_JobQueue];
	PRINT 'Dropped constraint [PK_NexusForge_JobQueue]';

	ALTER TABLE [NexusForge].[List] DROP CONSTRAINT [PK_NexusForge_List];
	PRINT 'Dropped constraint [PK_NexusForge_List]';

	ALTER TABLE [NexusForge].[Set] DROP CONSTRAINT [PK_NexusForge_Set];
	PRINT 'Dropped constraint [PK_NexusForge_Set]';

	ALTER TABLE [NexusForge].[State] DROP CONSTRAINT [PK_NexusForge_State];
	PRINT 'Dropped constraint [PK_NexusForge_State]';

	-- We are removing identity columns of the following tables completely, their clustered
	-- index will be based on natural values. So, instead of modifying them to BIGINT, we
	-- are dropping them.

	ALTER TABLE [NexusForge].[AggregatedCounter] DROP COLUMN [Id];
	PRINT 'Dropped [AggregatedCounter].[Id] column, we will cluster on [Key] column with uniqufier';

	ALTER TABLE [NexusForge].[Counter] DROP COLUMN [Id];
	PRINT 'Dropped [Counter].[Id] column, we will cluster on [Key] column';

	ALTER TABLE [NexusForge].[Hash] DROP COLUMN [Id];
	PRINT 'Dropped [Hash].[Id] column, we will cluster on [Key]/[Field] columns';

	ALTER TABLE [NexusForge].[Set] DROP COLUMN [Id];
	PRINT 'Dropped [Set].[Id] column, we will cluster on [Key]/[Value] columns';

	ALTER TABLE [NexusForge].[JobParameter] DROP COLUMN [Id];
	PRINT 'Dropped [JobParameter].[Id] column, we will cluster on [JobId]/[Name] columns';

	-- Then we need to modify all the remaining Id columns to be of type BIGINT.

	ALTER TABLE [NexusForge].[List] ALTER COLUMN [Id] BIGINT NOT NULL;
	PRINT 'Modified [List].[Id] type to BIGINT';

	ALTER TABLE [NexusForge].[Job] ALTER COLUMN [Id] BIGINT NOT NULL;
	PRINT 'Modified [Job].[Id] type to BIGINT';

	ALTER TABLE [NexusForge].[Job] ALTER COLUMN [StateId] BIGINT NULL;
	PRINT 'Modified [Job].[StateId] type to BIGINT';

	ALTER TABLE [NexusForge].[JobParameter] ALTER COLUMN [JobId] BIGINT NOT NULL;
	PRINT 'Modified [JobParameter].[JobId] type to BIGINT';

	ALTER TABLE [NexusForge].[JobQueue] ALTER COLUMN [JobId] BIGINT NOT NULL;
	PRINT 'Modified [JobQueue].[JobId] type to BIGINT';

	ALTER TABLE [NexusForge].[JobQueue] ALTER COLUMN [Id] BIGINT NOT NULL;
	PRINT 'Modified [JobQueue].[Id] type to BIGINT';

	ALTER TABLE [NexusForge].[State] ALTER COLUMN [Id] BIGINT NOT NULL;
	PRINT 'Modified [State].[Id] type to BIGINT';

	ALTER TABLE [NexusForge].[State] ALTER COLUMN [JobId] BIGINT NOT NULL;
	PRINT 'Modified [State].[JobId] type to BIGINT';

	ALTER TABLE [NexusForge].[Counter] ALTER COLUMN [Value] INT NOT NULL;
	PRINT 'Modified [Counter].[Value] type to INT';

	-- Adding back all the Primary Key constraints or clustered indexes where PKs aren't appropriate.

	ALTER TABLE [NexusForge].[AggregatedCounter] ADD CONSTRAINT [PK_NexusForge_CounterAggregated] PRIMARY KEY CLUSTERED (
		[Key] ASC
	);
	PRINT 'Re-created constraint [PK_NexusForge_CounterAggregated]';

	CREATE CLUSTERED INDEX [CX_NexusForge_Counter] ON [NexusForge].[Counter] ([Key]);
	PRINT 'Created clustered index [CX_NexusForge_Counter]';

	ALTER TABLE [NexusForge].[Hash] ADD CONSTRAINT [PK_NexusForge_Hash] PRIMARY KEY CLUSTERED (
		[Key] ASC,
		[Field] ASC
	);
	PRINT 'Re-created constraint [PK_NexusForge_Hash]';

	ALTER TABLE [NexusForge].[Job] ADD CONSTRAINT [PK_NexusForge_Job] PRIMARY KEY CLUSTERED ([Id] ASC);
	PRINT 'Re-created constraint [PK_NexusForge_Job]';
	
	ALTER TABLE [NexusForge].[JobParameter] ADD CONSTRAINT [PK_NexusForge_JobParameter] PRIMARY KEY CLUSTERED (
		[JobId] ASC,
		[Name] ASC
	);
	PRINT 'Re-created constraint [PK_NexusForge_JobParameter]';

	ALTER TABLE [NexusForge].[JobQueue] ADD CONSTRAINT [PK_NexusForge_JobQueue] PRIMARY KEY CLUSTERED (
		[Queue] ASC,
		[Id] ASC
	);
	PRINT 'Re-created constraint [PK_NexusForge_JobQueue]';

	ALTER TABLE [NexusForge].[List] ADD CONSTRAINT [PK_NexusForge_List] PRIMARY KEY CLUSTERED (
		[Key] ASC,
		[Id] ASC
	);
	PRINT 'Re-created constraint [PK_NexusForge_List]';

	ALTER TABLE [NexusForge].[Set] ADD CONSTRAINT [PK_NexusForge_Set] PRIMARY KEY CLUSTERED (
		[Key] ASC,
		[Value] ASC
	);
	PRINT 'Re-created constraint [PK_NexusForge_Set]';

	ALTER TABLE [NexusForge].[State] ADD CONSTRAINT [PK_NexusForge_State] PRIMARY KEY CLUSTERED (
		[JobId] ASC,
		[Id]
	);
	PRINT 'Re-created constraint [PK_NexusForge_State]';

	-- Creating secondary, nonclustered indexes

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Job_StateName] ON [NexusForge].[Job] ([StateName])
	WHERE [StateName] IS NOT NULL;
	PRINT 'Re-created index [IX_NexusForge_Job_StateName]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Set_Score] ON [NexusForge].[Set] ([Score])
	WHERE [Score] IS NOT NULL;
	PRINT 'Created index [IX_NexusForge_Set_Score]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Server_LastHeartbeat] ON [NexusForge].[Server] ([LastHeartbeat]);
	PRINT 'Created index [IX_NexusForge_Server_LastHeartbeat]';

	-- Creating filtered indexes for ExpireAt columns

	CREATE NONCLUSTERED INDEX [IX_NexusForge_AggregatedCounter_ExpireAt] ON [NexusForge].[AggregatedCounter] ([ExpireAt])
	WHERE [ExpireAt] IS NOT NULL;
	PRINT 'Created index [IX_NexusForge_AggregatedCounter_ExpireAt]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Hash_ExpireAt] ON [NexusForge].[Hash] ([ExpireAt])
	WHERE [ExpireAt] IS NOT NULL;
	PRINT 'Re-created index [IX_NexusForge_Hash_ExpireAt]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Job_ExpireAt] ON [NexusForge].[Job] ([ExpireAt])
	INCLUDE ([StateName])
	WHERE [ExpireAt] IS NOT NULL;
	PRINT 'Re-created index [IX_NexusForge_Job_ExpireAt]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_List_ExpireAt] ON [NexusForge].[List] ([ExpireAt])
	WHERE [ExpireAt] IS NOT NULL;
	PRINT 'Re-created index [IX_NexusForge_List_ExpireAt]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Set_ExpireAt] ON [NexusForge].[Set] ([ExpireAt])
	WHERE [ExpireAt] IS NOT NULL;
	PRINT 'Re-created index [IX_NexusForge_Set_ExpireAt]';

	-- Restoring foreign keys

	ALTER TABLE [NexusForge].[State] ADD CONSTRAINT [FK_NexusForge_State_Job] FOREIGN KEY([JobId])
		REFERENCES [NexusForge].[Job] ([Id])
		ON UPDATE CASCADE
		ON DELETE CASCADE;
	PRINT 'Re-created constraint [FK_NexusForge_State_Job]';

	ALTER TABLE [NexusForge].[JobParameter] ADD CONSTRAINT [FK_NexusForge_JobParameter_Job] FOREIGN KEY([JobId])
		REFERENCES [NexusForge].[Job] ([Id])
		ON UPDATE CASCADE
		ON DELETE CASCADE;
	PRINT 'Re-created constraint [FK_NexusForge_JobParameter_Job]';

	SET @CURRENT_SCHEMA_VERSION = 6;
END

IF @CURRENT_SCHEMA_VERSION = 6
BEGIN
	PRINT 'Installing schema version 7';

	DROP INDEX [IX_NexusForge_Set_Score] ON [NexusForge].[Set];
	PRINT 'Dropped index [IX_NexusForge_Set_Score]';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_Set_Score] ON [NexusForge].[Set] ([Key], [Score]);
	PRINT 'Created index [IX_NexusForge_Set_Score] with the proper composite key';

	SET @CURRENT_SCHEMA_VERSION = 7;
END

IF @CURRENT_SCHEMA_VERSION = 7 AND @DISABLE_HEAVY_MIGRATIONS = 1
BEGIN
	PRINT 'Migration process STOPPED at schema version ' + CAST(@CURRENT_SCHEMA_VERSION AS NVARCHAR) +
		'. WILL NOT upgrade to schema version ' + CAST(@TARGET_SCHEMA_VERSION AS NVARCHAR) +
		', because @DISABLE_HEAVY_MIGRATIONS option is set.';
END
ELSE IF @CURRENT_SCHEMA_VERSION = 7
BEGIN
	PRINT 'Installing schema version 8';

	ALTER TABLE [NexusForge].[Server] DROP CONSTRAINT [PK_NexusForge_Server]
	PRINT 'Dropped constraint [PK_NexusForge_Server] to modify the [NexusForge].[Server].[Id] column';

	ALTER TABLE [NexusForge].[Server] ALTER COLUMN [Id] NVARCHAR (200) NOT NULL;
	PRINT 'Modified [NexusForge].[Server].[Id] length to 200';

	ALTER TABLE [NexusForge].[Server] ADD  CONSTRAINT [PK_NexusForge_Server] PRIMARY KEY CLUSTERED ([Id] ASC);
	PRINT 'Re-created constraint [PK_NexusForge_Server]';

	-- Nothing complicated - we just collecting all the secondary indexes and primary key names to delete them.
	-- We should expect nothing here, because custom columns and indexes can be applied for the [Counter] table
	-- to make replication work on Microsoft Azure, like in the issue below.
	-- https://github.com/NexusForgeIO/NexusForge/issues/1500
	DECLARE @dropIndexSql2 NVARCHAR(MAX) = N'';
	SELECT @dropIndexSql2 += N'DROP INDEX ' + QUOTENAME(SCHEMA_NAME(o.[schema_id])) + '.' + QUOTENAME(o.name) + '.' + QUOTENAME(i.name) + ';'
	FROM sys.indexes AS i
	INNER JOIN sys.tables AS o
	ON i.[object_id] = o.[object_id]
	WHERE i.is_primary_key = 0
	AND i.index_id <> 0
	AND o.is_ms_shipped = 0
	AND SCHEMA_NAME(o.[schema_id]) = 'NexusForge'
	AND o.name = 'Counter';

	SELECT @dropIndexSql2 += N'ALTER TABLE' + QUOTENAME(SCHEMA_NAME(o.[schema_id])) + '.' + QUOTENAME(o.name) + ' DROP CONSTRAINT ' + QUOTENAME(c.name) + ';'
	FROM sys.key_constraints c
	INNER JOIN sys.tables AS o
	ON c.[parent_object_id] = o.[object_id]
	WHERE o.is_ms_shipped = 0
	AND SCHEMA_NAME(o.[schema_id]) = 'NexusForge'
	AND o.name = 'Counter'

	EXEC sp_executesql @dropIndexSql2;
	PRINT 'Dropped all indexes on the [NexusForge].[Counter] table';

	-- [Counter].[Id] column can already be added to make replication work as written above, so we will re-create it
	-- to ensure it is in the expected format.
	PRINT 'Checking for existence of the [NexusForge].[Counter].[Id] column';
	IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Counter' AND COLUMN_NAME = 'Id' AND TABLE_SCHEMA='NexusForge')
	BEGIN
		ALTER TABLE [NexusForge].[Counter] DROP COLUMN [Id];
		PRINT 'Dropped [NexusForge].[Counter].[Id] column';
	END

	ALTER TABLE [NexusForge].[Counter] ADD [Id] BIGINT IDENTITY(1, 1);
	PRINT 'Created [NexusForge].[Counter].[Id] column';

	ALTER TABLE [NexusForge].[Counter] ADD  CONSTRAINT [PK_NexusForge_Counter] PRIMARY KEY CLUSTERED (
		[Key] ASC,
		[Id] ASC
	);
	PRINT 'Created clustered primary key PK_NexusForge_Counter ([Key], [Id])';

	-- SqlServerStorageOptions.UseIgnoreDupKeyOption will yield much better results with INSERT/UPDATE operators
	-- instead of MERGE for [Set] and [Hash] tables. This change is also compatible with older clients, since
	-- MERGE operator is used there, so those clients are forward-compatible with these changes.
	ALTER TABLE [NexusForge].[Set] REBUILD WITH (IGNORE_DUP_KEY = ON);
	PRINT 'Enabled IGNORE_DUP_KEY option for [NexusForge].[Set] table';

	ALTER TABLE [NexusForge].[Hash] REBUILD WITH (IGNORE_DUP_KEY = ON);
	PRINT 'Enabled IGNORE_DUP_KEY option for [NexusForge].[Hash] table';

	ALTER TABLE [NexusForge].[JobQueue] DROP CONSTRAINT [PK_NexusForge_JobQueue];
	PRINT 'Dropped constraint [PK_NexusForge_JobQueue] to modify the [NexusForge].[JobQueue].[Id] column';

	ALTER TABLE [NexusForge].[JobQueue] ALTER COLUMN [Id] BIGINT NOT NULL;
	PRINT 'Changed [NexusForge].[JobQueue].[Id] column type to BIGINT';

	ALTER TABLE [NexusForge].[JobQueue] ADD CONSTRAINT [PK_NexusForge_JobQueue] PRIMARY KEY CLUSTERED (
		[Queue] ASC,
		[Id] ASC
	);
	PRINT 'Re-created constraint [PK_NexusForge_JobQueue]';

	SET @CURRENT_SCHEMA_VERSION = 8;
END

IF @CURRENT_SCHEMA_VERSION = 8
BEGIN
	PRINT 'Installing schema version 9';

	CREATE NONCLUSTERED INDEX [IX_NexusForge_State_CreatedAt] ON [NexusForge].[State] ([CreatedAt] ASC)

	SET @CURRENT_SCHEMA_VERSION = 9;
END

IF @CURRENT_SCHEMA_VERSION = 9
BEGIN
	PRINT 'Installing schema version 10';

	IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JobQueue' AND COLUMN_NAME = 'TenantId' AND TABLE_SCHEMA='NexusForge')
	BEGIN
		ALTER TABLE [NexusForge].[JobQueue] ADD [TenantId] NVARCHAR(100) NULL;
		PRINT 'Created [NexusForge].[JobQueue].[TenantId] column';
	END

	IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NexusForge_JobQueue_Tenant_QueueAndFetchedAt' AND object_id = OBJECT_ID('[NexusForge].[JobQueue]'))
	BEGIN
		CREATE NONCLUSTERED INDEX [IX_NexusForge_JobQueue_Tenant_QueueAndFetchedAt] ON [NexusForge].[JobQueue] (
			[TenantId] ASC,
			[Queue] ASC,
			[FetchedAt] ASC
		) INCLUDE ([JobId]);
		PRINT 'Created index [IX_NexusForge_JobQueue_Tenant_QueueAndFetchedAt]';
	END

	SET @CURRENT_SCHEMA_VERSION = 10;
END

UPDATE [NexusForge].[Schema] SET [Version] = @CURRENT_SCHEMA_VERSION
IF @@ROWCOUNT = 0 
	INSERT INTO [NexusForge].[Schema] ([Version]) VALUES (@CURRENT_SCHEMA_VERSION)        

PRINT 'NexusForge database schema installed';

COMMIT TRANSACTION;
PRINT 'NexusForge SQL objects installed';
