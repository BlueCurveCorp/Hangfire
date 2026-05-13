# Tenant-Aware Distributed Locks Specification

## Summary

NexusForge SQL Server storage currently uses SQL Server application locks for distributed coordination. These locks are logical resource names acquired with `sp_getapplock` and released with `sp_releaseapplock`. In a multi-tenant deployment, tenant-owned work should not contend with unrelated tenant-owned work only because both tenants use the same logical resource name.

This specification introduces explicit tenant-aware lock resource formatting while preserving the existing SQL Server application lock mechanism. The goal is to scope tenant-owned locks to a tenant when the operation is tenant-owned, while keeping scheduler, schema, expiration, aggregation, and other storage-wide coordination locks global.

## Goals

- Allow tenant A and tenant B to acquire the same tenant-owned logical lock independently.
- Preserve existing behavior for global storage coordination locks.
- Keep SQL Server application locks as the SQL Server storage default.
- Avoid making all calls to `AcquireDistributedLock` implicitly tenant-scoped.
- Make lock scope visible and testable at the call site.
- Preserve current reentrant lock behavior for the same connection and same resource.
- Preserve session-owned SQL lock lifetime semantics.

## Non-Goals

- Do not replace SQL Server application locks with Redis or another external lock provider.
- Do not make schema installation or storage maintenance tenant-scoped.
- Do not introduce per-tenant SQL schemas or per-tenant databases.
- Do not change SQL Server row, page, or table locking semantics.
- Do not change queue fetch filtering; tenant-aware queue fetch remains based on `JobQueue.TenantId`.
- Do not make recurring jobs tenant-aware unless the recurring job model itself is updated to store and enumerate tenant identity.

## Current Behavior

SQL Server storage calls `AcquireDistributedLock(resource, timeout)` and prefixes the resource with the storage schema name:

```text
NexusForge:<resource>
```

Examples:

```text
NexusForge:locks:schedulepoller
NexusForge:lock:recurring-job:nightly-cleanup
NexusForge:job:123:state-lock
NexusForge:List:Lock
NexusForge:List:recurring-jobs:Lock
```

The SQL lock owner is the SQL session. Each `SqlServerConnection` creates a dedicated SQL connection when it first acquires a distributed lock. The same `SqlServerConnection` tracks locally acquired resources, so acquiring the same resource twice on the same connection is reentrant and only releases the SQL lock after the last local holder is disposed.

This is safe for single-tenant storage-wide coordination, but it can create unnecessary cross-tenant contention when a lock protects tenant-owned work.

## Proposed Model

Introduce an explicit lock resource formatter with two scopes:

```text
Global: NexusForge:<resource>
Tenant: NexusForge:tenant:<tenantId>:<resource>
```

The formatter must not alter the SQL Server application lock mechanism. It only changes the resource string passed to `sp_getapplock`.

Tenant scoping must be explicit. A caller should choose either:

- Global lock: protects storage-wide or cross-tenant invariants.
- Tenant lock: protects tenant-owned state or tenant-owned execution.

The default `AcquireDistributedLock(resource, timeout)` behavior should remain global unless a separate tenant-aware API is introduced and used by a tenant-owned call site.

## API Shape

Recommended internal API:

```csharp
internal enum DistributedLockResourceScope
{
    Global,
    Tenant
}

internal interface IDistributedLockResourceFormatter
{
    string FormatGlobal(string schemaName, string resource);
    string FormatTenant(string schemaName, string tenantId, string resource);
}
```

Recommended Core-visible API:

```csharp
public enum TenantLockFallbackMode
{
    Throw,
    Global
}

public interface IStorageConnection
{
    IDisposable AcquireDistributedLock(string resource, TimeSpan timeout);

    IDisposable AcquireTenantDistributedLock(
        string tenantId,
        string resource,
        TimeSpan timeout,
        TenantLockFallbackMode fallbackMode = TenantLockFallbackMode.Throw);
}

public abstract class JobStorageConnection : IStorageConnection
{
    public abstract IDisposable AcquireDistributedLock(string resource, TimeSpan timeout);

    public virtual IDisposable AcquireTenantDistributedLock(
        string tenantId,
        string resource,
        TimeSpan timeout,
        TenantLockFallbackMode fallbackMode = TenantLockFallbackMode.Throw);
}
```

Recommended transaction method:

```csharp
public abstract class JobStorageTransaction
{
    public virtual void AcquireTenantDistributedLock(
        string tenantId,
        string resource,
        TimeSpan timeout,
        TenantLockFallbackMode fallbackMode = TenantLockFallbackMode.Throw);
}
```

The tenant-aware methods need to be part of the Core storage contracts, or exposed by Core extension methods that can reliably detect provider support, because call sites such as `DisableConcurrentExecutionAttribute` only know about `IStorageConnection` and `JobStorageTransaction`.

Default fallback behavior must be fail-closed:

- `TenantLockFallbackMode.Throw`: throw a storage feature exception when tenant-aware distributed locks are unsupported.
- `TenantLockFallbackMode.Global`: explicitly fall back to `AcquireDistributedLock`.

There should be no `NoLock` fallback mode for built-in locking behavior. Skipping a distributed lock would silently remove correctness guarantees.

Recommended feature flags:

```text
Connection.TenantAwareDistributedLock
Transaction.TenantAwareDistributedLock
```

The existing `AcquireDistributedLock` method must remain global.

Recommended `DisableConcurrentExecution` option:

```csharp
public sealed class DisableConcurrentExecutionOptions
{
    public DistributedLockScope DefaultScope { get; set; } = DistributedLockScope.Global;
    public TenantLockFallbackMode TenantFallbackMode { get; set; } = TenantLockFallbackMode.Throw;
}
```

`DisableConcurrentExecutionAttribute` should use the configured tenant-aware default scope:

- When the global default is `DistributedLockScope.Global`, use `AcquireDistributedLock`.
- When the global default is `DistributedLockScope.Tenant`, use `AcquireTenantDistributedLock`.
- Allow individual attributes to override the configured default scope.

Tenant-aware scope should use `TenantLockFallbackMode.Throw` by default. Falling back to a global lock must be an explicit compatibility setting, because global fallback preserves correctness but can reintroduce cross-tenant contention in deployments that expect tenant isolation.

## Tenant Context

`NexusForgeTenantContext.CurrentTenantId` is an ambient context and may be useful at some call sites, but the lock formatter should not blindly read it for all locks. Ambient tenant context is safe only when the call site is already known to be tenant-owned.

Workers should set tenant context while performing a tenant-fetched job. This lets tenant-owned job filters, including `DisableConcurrentExecution`, acquire tenant-scoped locks without requiring every filter to parse queue metadata.

Recommended worker behavior:

- If a worker was configured with `BackgroundJobServerOptions.TenantId`, set `NexusForgeTenantContext` during job performance and server filter execution.
- If a future worker mode can process jobs from multiple tenants, set tenant context from the fetched job or job parameter tenant identity instead of the server option.
- Clear/restore the previous tenant context after the job finishes.
- Do not set tenant context for global workers.

## Tenant-Scoped Lock Candidates

The following locks SHOULD become tenant-scoped when a tenant id is known and the protected state is tenant-owned.

### DisableConcurrentExecution

`DisableConcurrentExecutionAttribute` currently derives a resource from either the configured resource template or the job method name. For tenant-owned jobs, the same logical resource should be independent per tenant.

Current logical resources:

```text
<custom resource>
<job type>.<method name>
```

Tenant-aware examples:

```text
NexusForge:tenant:tenant-a:MyService.Import
NexusForge:tenant:tenant-b:MyService.Import
```

Tenant A and tenant B should be able to run the same `[DisableConcurrentExecution]` job concurrently. Two workers in tenant A should still contend.

### Tenant-Owned Job State Locks

Job state changes use:

```text
job:<jobId>:state-lock
```

Since job identifiers are globally unique in a shared NexusForge storage, tenant scoping is not required for correctness. Tenant scoping MAY be used when the tenant id is available, but it is not expected to improve contention significantly because different jobs already have different lock resources.

This lock SHOULD remain global by default. Tenant-scoping adds complexity without meaningful contention reduction, and state changes or continuations may involve cross-tenant relationships unless those are explicitly forbidden elsewhere.

### Tenant-Owned Recurring Jobs

Recurring job locks use:

```text
lock:recurring-job:<recurringJobId>
recurring-jobs:lock
```

Per-recurring-job locks SHOULD become tenant-scoped only after recurring job storage is tenant-aware. The recurring job list, recurring job hash keys, dashboard enumeration, and scheduler selection must all carry tenant identity before the lock is changed.

Until recurring jobs are tenant-aware, these locks MUST remain global.

### Tenant-Owned List, Set, and Hash Keys

Write transactions acquire logical locks for list, set, and hash operations. With global locks enabled, the lock may be broad:

```text
List
Set
Hash
```

With fine-grained locking enabled, the lock may include the key:

```text
List:<key>
Set:<key>
Hash:<key>
```

Tenant-scoped list, set, and hash locks are safe only when the protected data is also tenant-partitioned. The implementation MUST NOT tenant-scope a lock for a physical key that remains shared across tenants. Tenant-owned keys SHOULD include tenant identity in the key itself, or the SQL operation must otherwise be tenant-filtered by schema-supported tenant data.

The implementation must avoid tenant-scoping storage-wide keys such as `recurring-jobs` until their data model is tenant-aware.

### Tenant-Owned Queue or Resource Control Locks

Any future lock that protects a tenant-specific queue command, resource command, drain command, or availability calculation SHOULD be tenant-scoped when the command or state is tenant-specific.

Server-wide resource commands MUST remain global to the server unless commands become tenant-addressed.

## Locks That Must Remain Global

The following lock types MUST remain global even when a resource formatter exists and even when an ambient tenant context is set.

### Schema and Installation Locks

Schema locks protect physical database objects and migrations, not tenant-owned data.

Known resource patterns:

```text
NexusForge:SchemaLock
<schema>:SchemaLock
```

These MUST remain global.

### Delayed Job Scheduler Poller Lock

The delayed scheduler lock coordinates movement of scheduled jobs to enqueueable state.

Known resource:

```text
locks:schedulepoller
```

This MUST remain global until scheduled job storage is partitioned by tenant and the scheduler enumerates per tenant. If tenant-scoped too early, multiple tenants could run independent pollers over shared scheduled sets and double-process or over-contend on the same global scheduled data.

### Expiration Manager Lock

Expiration cleanup removes expired storage records across the shared storage.

Known resource:

```text
locks:expirationmanager
```

This MUST remain global because cleanup operates over storage-wide tables and indexes.

### Counter Aggregation Lock

Counter aggregation compacts global counter records and updates aggregate counters.

There is no current SQL Server application lock for `CountersAggregator`; it relies on SQL statements, transactions, and lock hints. If a distributed lock is introduced later for counter aggregation, it MUST remain global.

Possible future resource patterns:

```text
locks:counters
locks:countersaggregator
```

These MUST remain global unless counter storage is explicitly partitioned by tenant and aggregation enumerates tenant partitions independently.

### Storage-Wide Maintenance Locks

Any lock used by a storage-wide background process MUST remain global. Some current maintenance paths do not use distributed application locks today; this list applies to existing locks and any future locks introduced for these processes.

This includes maintenance for:

- Expired job deletion.
- Counter aggregation.
- Server watchdog cleanup.
- Set/list/hash cleanup that scans shared keys.
- Global statistics updates.
- Schema version checks or migrations.

### Global Recurring Job Schedule Locks

Until recurring jobs have a tenant-aware data model, these MUST remain global:

```text
recurring-jobs:lock
lock:recurring-job:<recurringJobId>
```

The recurring job set and recurring job hashes are currently shared by logical recurring job id. Tenant-scoping only the lock without tenant-scoping the data would create a false sense of isolation.

### Global List, Set, and Hash Locks

The broad lock names used when global locks are enabled MUST remain global:

```text
List
Set
Hash
```

Tenant scoping these broad locks would change their meaning and could allow concurrent writers into shared keys that still expect global serialization.

### Shared Storage Keys

Fine-grained locks for shared keys MUST remain global. Known shared keys include:

```text
recurring-jobs
schedule
servers
stats
```

Any key that is not explicitly tenant-owned MUST be treated as shared and therefore globally locked.

### Server Identity and Server Resource Locks

Locks that protect a physical server identity or server-wide resource state MUST remain global. A `BackgroundJobServer` can have a tenant id, but server identity, heartbeat, drain/resume commands, and server resource telemetry are server-level data unless the command/state model explicitly adds tenant addressing.

Known data areas:

- Server heartbeat and announcement.
- Server watchdog cleanup.
- Server drain/resume commands.
- Server resource events.
- Server queue allocation metadata.

### Cross-Tenant Coordination Locks

Any lock that intentionally coordinates across tenants MUST remain global. The formatter should support this explicitly rather than relying on an absent tenant id.

## Locks That Should Remain Global By Default

The following lock types SHOULD remain global unless an implementation explicitly proves they protect tenant-owned state only.

- Job state locks, because job ids are already unique.
- Continuation parent job locks, because parent job ids are already unique and continuations may cross tenant boundaries unless forbidden elsewhere.
- Queue provider implementation locks, unless the queue provider key includes tenant identity.
- Dashboard command locks, unless the command route and storage model are tenant-aware.
- Any lock created by extension packages, unless the extension opts into tenant-aware formatting.

## Resource Name Rules

Formatted lock resource names must:

- Include the schema prefix first.
- Include tenant id only for tenant-scoped locks.
- Use a stable delimiter that cannot confuse schema, tenant id, and logical resource.
- Validate tenant id with `TenantIdValidator`.
- Preserve the logical resource string after the tenant prefix.
- Never pass a resource longer than 255 UTF-16 characters to SQL Server `sp_getapplock`.
- Use a deterministic hash suffix when the formatted resource would exceed the SQL Server limit.
- Avoid collisions between truncated resources.
- Preserve current case-sensitive resource semantics.

SQL Server application lock resources are binary-compared and therefore case-sensitive regardless of database collation. The formatter must not lowercase or otherwise normalize caller-provided resource names unless the specific built-in resource already does so today. Tenant ids are already lowercase by `TenantIdValidator`, but custom resource names must keep their existing casing.

SQL Server application lock identity includes:

- Current database id.
- Database principal passed through `@DbPrincipal`.
- Resource name passed through `@Resource`.

The formatter only changes the resource name component. It must not rely on database principal changes for tenant isolation.

Recommended format:

```text
<schema>:tenant:<tenantId>:<resource>
```

Examples:

```text
NexusForge:tenant:tenant-a:MyService.Import
NexusForge:tenant:tenant-a:List:tenant-owned-key:Lock
NexusForge:locks:schedulepoller
```

If the formatter has to shorten a resource, it should preserve a readable prefix and append a stable hash of the full unshortened resource. The hash input must include schema, tenant id, scope, and logical resource.

Recommended shortening algorithm:

- Compute SHA-256 over the UTF-8 bytes of the canonical unshortened formatted resource.
- Encode the hash as lowercase hex.
- Append a suffix of the form `:sha256:<64 hex chars>`.
- Truncate the readable prefix so the final string is at most 255 UTF-16 characters.
- Ensure the first 32 characters remain useful for diagnostics where possible, because SQL Server exposes only the first 32 characters of an acquired application lock resource in plain text and hashes the remainder internally.

The implementation must perform this shortening before calling `sp_getapplock`; relying on SQL Server's own truncation is forbidden because it can collapse distinct tenant resources into the same lock name.

## SQL Server 2025 Notes

SQL Server 2025 does not remove the need for tenant-aware application lock resource names.

Relevant SQL Server behavior:

- `sp_getapplock` remains the correct primitive for SQL Server application locks.
- `@Resource` is `nvarchar(255)`.
- Resource names longer than 255 characters are truncated by SQL Server.
- Resource names are binary-compared and case-sensitive regardless of database collation.
- Only the first 32 characters of an acquired application lock resource can be retrieved in plain text; the rest is represented internally by a hash.
- Application lock identity is scoped by database id, database principal, and resource name.
- `sp_releaseapplock` must be called the same number of times as `sp_getapplock` for the same resource and owner when the lock is explicitly acquired multiple times.

SQL Server 2025 optimized locking improves DML row/page lock behavior, lock memory usage, and lock escalation behavior. It does not replace application locks and does not remove the need for correct application lock resource names. Tenant-aware distributed locks should therefore continue to use `sp_getapplock`/`sp_releaseapplock` and treat optimized locking as an independent database-engine improvement.

## Compatibility

Existing deployments should keep existing lock names unless they opt into tenant-aware lock call sites.

Recommended rollout:

- Add the formatter and tenant-aware internal APIs.
- Add Core-visible tenant-aware lock APIs or extension points.
- Keep `AcquireDistributedLock` global.
- Update tenant-owned call sites one by one.
- Add tests for both tenant-isolated and global behavior.
- Document that custom filters using `AcquireDistributedLock` directly remain global unless they use the tenant-aware API.

## Testing Requirements

Tests MUST cover:

- Two different tenants can acquire the same tenant-scoped logical resource concurrently.
- Two callers for the same tenant and same logical resource contend.
- Reentrant acquisition still works for the same connection and same tenant-scoped resource.
- Global locks still contend even when ambient tenant contexts differ.
- `DisableConcurrentExecution` uses tenant-scoped resources when the configured or explicit scope is tenant and a tenant context is active.
- `DisableConcurrentExecution` uses global resources when the configured default scope is global.
- A `DisableConcurrentExecution` attribute with explicit global scope overrides a tenant global default.
- A `DisableConcurrentExecution` attribute with explicit tenant scope overrides a global default.
- `locks:schedulepoller` remains global when a tenant context is active.
- `locks:expirationmanager` remains global when a tenant context is active.
- Transactional tenant-scoped locks release correctly on commit and dispose.
- The formatter validates tenant ids and rejects invalid tenant ids.
- The formatter never passes a resource longer than 255 UTF-16 characters to SQL Server.
- Long resources with the same readable prefix but different full values produce different shortened names.
- Resource casing is preserved for caller-provided resource names.

SQL Server integration tests SHOULD verify the actual `sp_getapplock` behavior with two SQL connections. They SHOULD use `APPLOCK_MODE` and `APPLOCK_TEST` where possible to assert lock mode and grantability directly instead of relying only on timing behavior.

## Decisions

### Tenant-Aware Lock API Visibility

Tenant-aware lock APIs should be Core-visible. They can be public virtual members on Core storage abstractions or Core extension methods backed by provider capability checks, but they must be callable from Core features such as `DisableConcurrentExecutionAttribute`.

The first implementation should prefer Core storage abstraction members because they give storage providers an explicit override point and avoid reflection or SQL-specific casts.

### DisableConcurrentExecution Scope

`DisableConcurrentExecutionAttribute` should support a global default scope option. This lets deployments opt into tenant-aware `DisableConcurrentExecution` behavior centrally instead of annotating every job.

Individual attributes must still be able to override the global default. This is required for jobs that protect shared resources and must remain globally serialized even when tenant-aware locking is enabled by default.

Recommended API shape:

```csharp
public enum DistributedLockScope
{
    Default,
    Global,
    Tenant
}

public DistributedLockScope Scope { get; }
public TenantLockFallbackMode TenantFallbackMode { get; }
```

Default behavior:

- Existing constructors keep `Scope = DistributedLockScope.Default`.
- `DistributedLockScope.Default` resolves from the global `DisableConcurrentExecution` lock scope option.
- The global option default is `DistributedLockScope.Global` unless the application configures tenant-aware behavior.
- Multi-tenant deployments may configure the global option to `DistributedLockScope.Tenant`.
- Individual attributes may set `Scope = DistributedLockScope.Global` for shared-resource jobs.
- Individual attributes may set `Scope = DistributedLockScope.Tenant` when tenant scope is required regardless of the global default.
- Tenant scope requires `NexusForgeTenantContext.CurrentTenantId` during job performance.
- If tenant scope is requested and no tenant context exists, throw a clear exception.
- If tenant scope is requested but storage does not support tenant-aware locks, use `TenantFallbackMode`.

This makes tenant-aware locking convenient for multi-tenant deployments while preserving an explicit global-lock escape hatch for jobs that coordinate shared state.

### Tenant-Owned List, Set, and Hash Locks

Tenant-owned list, set, and hash locks should be explicit, not convention-based.

The first implementation should not try to infer tenant ownership from arbitrary key strings. Tenant-owned storage keys should either:

- Include tenant identity in the key by construction, or
- Use a future explicit tenant-aware storage operation whose SQL predicates include tenant identity.

The formatter must not tenant-scope a lock for a key unless the caller explicitly identifies the operation as tenant-owned.

### Recurring Jobs

Recurring jobs should remain a separate feature.

Tenant-aware distributed locks may provide the primitives needed by future tenant-aware recurring jobs, but this specification does not change recurring job storage, recurring job ids, recurring job scheduler enumeration, or dashboard recurring job views.

Until recurring jobs are tenant-aware end-to-end, recurring job locks remain global.

When recurring jobs become tenant-aware, tenant identity should be stored as first-class recurring job data, not encoded only in the user-visible recurring job id. A tenant-aware recurring job should be logically identified by:

- `TenantId`
- `RecurringJobId`

This allows two tenants to use the same recurring job id without conflict and avoids making dashboards, monitoring APIs, filters, and logs parse tenant identity out of an opaque id string.

Storage providers may still derive internal keys from both `TenantId` and `RecurringJobId` when their physical model requires a single unique key, but that derived key is an implementation detail. Public APIs and dashboard surfaces should treat tenant identity as structured data.

### Dashboard Lock Diagnostics

Dashboard lock scope diagnostics are not required for the first implementation.

The first implementation should focus on correct lock behavior and tests. Diagnostics may be added later if operators need to inspect tenant lock names or troubleshoot cross-tenant contention.

Tenant ids SHOULD be visible in lock-related logs and diagnostics. Tenant id visibility is operationally useful for investigating contention and should be treated as part of the tenant-aware lock contract.

Custom lock resource names may contain application-specific values. Logging should prefer structured fields:

- `LockScope`
- `TenantId`
- `Resource`
- `FormattedResource`

If a custom resource is very long, logs may include the shortened formatted resource plus the stable SHA-256 hash suffix defined above. Implementations should avoid ad hoc truncation that removes the hash and makes two different resources look identical.

## Remaining Open Questions

No open questions remain for the first tenant-aware distributed lock specification.
