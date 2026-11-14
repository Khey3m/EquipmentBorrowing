# PostgreSQL setup and Session 3 decisions

## Prerequisites and configuration

Use .NET 10 SDK, PostgreSQL (verified with 18.6), and psql. No Docker/cloud service required. Dependencies: Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 and EF Core/Relational/Design 10.0.12. Explicit runtime references prevent dependent projects resolving EF 10.0.4 through Npgsql while Infrastructure compiles against 10.0.12. The repository tool manifest pins dotnet-ef 10.0.12.

Runtime/design-time configuration comes only from `EQUIPMENT_BORROWING_CONNECTION_STRING`. Shape only; replace placeholders locally:

```text
Host=localhost;Port=5432;Database=equipment_borrowing_lab4;Username=<local-role>;Password=<local-secret>;Include Error Detail=false
```

Session 3 authenticated locally and created the dedicated development database. Its setting is in the current Windows user's environment, outside Git. Restart the IDE to inherit it, or load it into the current PowerShell process:

```powershell
$env:EQUIPMENT_BORROWING_CONNECTION_STRING = [Environment]::GetEnvironmentVariable('EQUIPMENT_BORROWING_CONNECTION_STRING', 'User')
```

On another machine, create an empty development database and set the variable locally. Local setup currently uses the supplied administrative role; provisioning a limited role was not part of this session. Normal application operation does not require server-superuser rights. Do not commit credentials or publish them in logs/screenshots.

## Migrate and run

From the repository root on an approved development environment:

```powershell
dotnet tool restore
dotnet restore EquipmentBorrowing.slnx
dotnet ef migrations has-pending-model-changes --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Infrastructure
dotnet ef database update --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Infrastructure
dotnet run --project src/EquipmentBorrowing.Desktop
```

Infrastructure is the design-time startup project, avoiding Avalonia for migration commands. Runtime bootstrap delegates migration/seeding to Infrastructure. Seed data is inserted only when the corresponding table is empty; existing records are not overwritten. Generated IDs avoid sequence collisions; example staff identifiers remain STU-001..003 and EQ-101..103.

**Current limitation:** Windows Application Control blocks project assembly loading and Avalonia build tasks. EF migration execution, snapshot comparison, seeding and desktop launch remain unverified. The development database is still empty pending EF execution; no fake migration-history entries were written. Run the commands above in an approved environment or after an administrator-managed development allowance. Security policy was not changed.

`20261114000100_InitialPostgres` is an authored EF migration because CLI scaffolding was blocked. Its target model is frozen in InitialPostgresModel, separately from live configurations. PostgresModelTests checks snapshot/runtime agreement and migration discovery without connecting to a database; it must pass before EF reproduction is considered verified.

Historical SQLite migrations are retained as `.cs.txt` under `archive/sqlite-migrations`, outside compiled migration discovery. No operational SQLite fallback, simultaneous support or historical row import is provided.

## Integrity and normalization

Generated integer IDs, restrictive foreign keys, required values, nonblank checks and date/state checks are configured. Expected return must follow checkout; active records have no actual return, while returned records require an actual date no earlier than checkout. A partial unique equipment index permits only one Active loan per item while preserving returned history.

PostgreSQL generates normalized keys with `upper(btrim(..., <Unicode whitespace set>))` and enforces uniqueness on them, preventing SQL callers from supplying a false canonical key. Identifier checks reject surrounding whitespace matching .NET Trim; case duplicates conflict with unique indexes. Domain input also trims and uppercases invariantly. Future uniqueness-query implementations must canonicalize search input in PostgreSQL too, so locale/Unicode casing cannot cause prevalidation to disagree with database enforcement. Database constraints remain authoritative.

The database does not independently enforce the per-student three-item limit or synchronize availability with loans; transactional workflow revalidation is still required.

## Context and transaction ownership

IDbContextFactory creates fresh contexts. Standalone repository calls own/dispose their context, so there is no window-lifetime root-scoped context. Returned objects are detached after disposal.

IBorrowingSessionFactory provides a shared context and serializable transaction for a complete write operation, exposing repository interfaces to Application. Repository SaveChanges flushes inside that transaction; CommitAsync is the single commit boundary. Disposal without commit releases/rolls back uncommitted work.

**Session 4 now consumes the session in BorrowEquipmentService and ReturnEquipmentService.** Production validation and writes share a transaction, with conflict refresh and explicit unknown-outcome feedback. Startup now opens the window before calling the Application startup service and provides Retry startup on failure. See [Session 4 evidence](session4-evidence.md). SQL rollback evidence alone does not prove application atomicity; actual EF/UI acceptance remains pending under the policy blocker.

No automatic retry policy is enabled. Explicit PostgreSQL rejection remains a known unsuccessful outcome; transport/cancellation failures during commit become CommitOutcomeUnknownException. Consumers must refresh after reconnection and let staff decide before replaying. These runtime paths remain to be verified.

## Reproduce SQL checks

`tools/verify-postgres-schema.py` needs Python, psql, locally configured PGHOST/PGPORT/PGUSER/PGPASSWORD, and optionally PSQL_PATH. On this machine psql is `C:/Program Files/PostgreSQL/18/bin/psql.exe`.

```powershell
python tools/verify-postgres-schema.py
```

The configured role needs permission to create a disposable database. Each run creates a unique `equipment_borrowing_verify_*` database and retains it for inspection. It never deletes a database or changes the development database. It checks exact migration Up SQL, constraints and rollback with fresh connections; it does not execute EF or populate EF history. See [observed evidence](session3-evidence.md).

## Primary references

- [Npgsql provider setup](https://www.npgsql.org/efcore/)
- [Npgsql EF 10 release notes](https://www.npgsql.org/efcore/release-notes/10.0.html)
- [Microsoft: context lifetimes/factories](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
