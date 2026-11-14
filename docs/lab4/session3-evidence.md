# Session 3 evidence — 2026-11-14

Windows x64, .NET SDK 10.0.404, PostgreSQL 18.6 on localhost:5432. Credentials omitted. Fetch/pull completed before changes.

| Action | Expected | Observed |
|---|---|---|
| Authenticate and create dedicated development DB if absent | Connection and empty DB | Passed; equipment_borrowing_lab4 created |
| Build Infrastructure | Compile provider/schema/session code | Passed, zero warnings/errors |
| Build full solution after package alignment | Compile | Non-Desktop projects compiled; Desktop failed existing Avalonia MSB4061 |
| Scaffold EF migration using Infrastructure startup | Load model and scaffold | Blocked loading Infrastructure; Code Integrity event 3077 confirms policy block |
| Execute FrozenMigrationModelMatchesRuntimeModel | No pending changes; discover only InitialPostgres | Compiled, then failed before assertions: Application Control blocked Domain.dll (0x800711C7) |
| Run SQL verification script | Real constraint/rollback checks | Exit 0, all listed SQL checks passed |

An EF 10.0.4/10.0.12 dependency conflict found during test compilation was corrected using explicit EF Core/Relational 10.0.12 runtime references. Local dotnet-ef 10.0.12 was then pinned for reproducibility.

## SQL verification

Disposable database retained: `equipment_borrowing_verify_20261114030628109733`.

1. Applied exact InitialPostgres Up SQL on a clean PostgreSQL database: success.
2. Inserted one student and two equipment items with generated IDs.
3. Case duplicate student/equipment identifiers rejected with 23505; surrounding-space/tab student numbers and blank name rejected with 23514.
4. Invalid student foreign key rejected with 23503.
5. Equal checkout/expected dates and Returned without actual return rejected with 23514.
6. Second active borrowing for one item rejected with 23505.
7. Transaction updated the second item's availability and inserted its borrowing before a deliberately conflicting active-item insert failed. Fresh-connection reads showed identical availability and borrowing counts before/after: no partial changes.
8. Returned the first borrowing with actual date, retained it, and issued the same item again: both historical/current records remain.

The final run also checks whitespace including tabs and asserts the SQL copy exactly matches the migration literal. The earlier verification database equipment_borrowing_verify_20261114030103322874 is retained from the first run. Repeat with tools/verify-postgres-schema.py; each run creates a new disposable DB. No screenshots alone were used to establish persistence/atomicity. No EF history was fabricated.

## Remaining acceptance work

A17 is partially evidenced by real SQL constraints. EF clean migration execution/history, frozen-model comparison and repeat-safe startup seeding remain pending under the assembly policy blocker. The development database is empty. A18 application transaction/error handling and A01–A05/A06 workflow acceptance are not established by SQL verification.

Before operational use, run snapshot comparison, EF update, startup twice and fresh seed readback on an approved environment; then integrate the write session in Session 4 and verify actual workflows/restarts. No security policy was modified.
