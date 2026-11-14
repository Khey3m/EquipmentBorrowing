# Campus Equipment Borrowing System — Lab 4 Development Progress Report

**Reporting date:** 14 November 2026

**Coverage:** Sessions 1–4 of the Lab 4 implementation plan

**Repository:** https://github.com/Khey3m/EquipmentBorrowing

**Development branch:** `lab4/postgresql-workflows`

**Implementation baseline reviewed:** `fb81574`

## 1. Overall status

The project has advanced from its Labs 1–3 SQLite implementation to a PostgreSQL-oriented codebase with expanded domain rules, database constraints, operation-specific contexts, transactional single-item borrowing/returns, and updated catalog/active-borrowing screens.

Sessions 1–4 have their planned source/documentation changes implemented and committed. This is a progress report, not a certificate that Lab 4 is complete. The five continuation use cases UC-01–05 are implemented in source, but their PostgreSQL/Avalonia acceptance demonstrations remain pending. The nine new required use cases UC-06–09 and UC-12–16 still require their later-session implementations.

Real PostgreSQL SQL checks succeeded for schema creation, constraints, history retention and transaction rollback. EF migration execution, runtime model/snapshot comparison, startup seeding, Desktop compilation/launch and end-to-end restart demonstrations remain unverified because Windows Application Control blocks required assemblies. Successful SQL checks do not establish that EF or the desktop application executes successfully.

| Work area | Current status | Evidence strength |
|---|---|---|
| Baseline investigation and Git timeline | Implemented and recorded | Git commits, build output and policy logs |
| Domain fields, rules and Application contracts | Implemented; non-Desktop compilation verified | Initial 10 domain cases passed on an earlier Session 2 revision; later execution blocked |
| PostgreSQL configuration and schema | Implemented | SQL applied successfully to clean disposable databases |
| Database constraints | SQL-verified | Actual rejected writes and fresh-connection readback |
| EF migrations and model agreement | Source authored/compiled; execution pending | Tool/test assembly loading blocked |
| Single borrowing and return transactions | Integrated in production source | Compiled Application/Infrastructure; runtime behavior pending |
| Catalog and active-loan screens | Updated in source | XML structure checked; Avalonia build/rendering pending |
| New management, multi-item and history/search workflows | Contracts/domain foundations only | Full implementations remain scheduled |

## 2. Requirements and scope established

Both supplied Lab 4 PDFs were reviewed: the activity handout and the complete Use Case Suite. The plan covers all fourteen required actor-level cases and business rules BR-01–19.

The primary actor is Laboratory Staff / Equipment Custodian. The application remains the existing Campus Equipment Borrowing System, using C#/.NET, Avalonia, CommunityToolkit.Mvvm, EF Core and PostgreSQL/Npgsql.

The architecture preserves this responsibility path:

```text
Avalonia View
    -> ViewModel
    -> Application service
    -> Repository/query/session abstraction
    -> EF Core Infrastructure
    -> PostgreSQL
```

The Desktop composition root references Infrastructure to register implementations. Domain rules do not depend on EF Core or Avalonia. Database connection setup and migration execution are owned by Infrastructure, with an Application startup boundary.

UC-10–11 and UC-17–27 are deferred to Lab 5. UC-28–33 remain future scope. Parallel SQLite operation, importing historical SQLite rows, authentication, hard deletion, maintenance, retirement and cloud deployment were not added.

## 3. Session 1 — baseline, repository and traceability

### Work completed

- Established the `lab4/postgresql-workflows` branch while retaining earlier Git history.
- Preserved the pre-existing SQL examples and agent template in a separate checkpoint.
- Corrected the existing unavailable-equipment test setup: it now borrows the initially available item before attempting to borrow it again.
- Added a session plan, progress ledger, acceptance matrix and copies of the earlier report/review.
- Inspected installed .NET SDKs and PostgreSQL availability.
- Investigated the build/test failures using Windows Code Integrity logs rather than assuming they were application defects.

### Findings

The machine has .NET SDK 10.0.404, with 10.0.400 also installed, and PostgreSQL 18.6. PostgreSQL was running and accepting connections on localhost:5432. At this stage authenticated database access had not yet been verified; that occurred in Session 3.

Desktop builds failed while loading Avalonia's resource-generation task with MSB4061. Test loading also encountered Application Control error 0x800711C7. Code Integrity event 3077 explicitly identified blocked assemblies. Comparing the two installed SDKs did not resolve the build failure. No security policy was disabled or modified.

## 4. Session 2 — domain model and Application contracts

### Student model

Student now carries a stable internal ID, institutional student number, normalized number, full name, optional contact information and eligibility state. New students default to eligible. Required values reject blank input; descriptive text is trimmed, and blank optional contact becomes absent.

Descriptive edits preserve the internal ID and eligibility. Eligibility changes use a separate method that reports whether the requested state actually changed. Every student has a fixed limit of three active items. The rule considers existing active items plus the requested quantity, including invalid negative/zero quantities and overflow-safe limit comparison.

The old variable MaxAllowedBorrowings field became a read-only compatibility property and is excluded from EF mapping. Seed data no longer grants some students a two-item limit.

### Equipment model

Equipment now carries a stable internal ID, staff-facing identifier, normalized identifier, name, optional description and availability. New equipment defaults to available. Descriptive edits cannot alter availability or the internal identity, and blank descriptions clear to absent.

### Borrowing model and calendar policy

BorrowedAt and ExpectedReturnAt now use DateOnly, and ActualReturnAt is nullable DateOnly. The expected return date must be later than checkout. Only an active borrowing may transition to Returned. Actual return cannot precede checkout; same-day and early returns are allowed.

CampusClock derives the local calendar date using Asia/Manila. It supports injected time/timezone dependencies, and entity methods receive explicit dates instead of reading the machine clock. BorrowRequest accepts an expected return date; Session 4 replaced the temporary duration input with a date picker.

### Identity strategy

Random borrowing IDs were removed. New entities start with zero IDs for database generation. The retained in-memory adapter assigns increasing IDs and prevents reassignment. It is a single-process demo/test adapter, not a production concurrency mechanism.

### Contracts prepared for later sessions

Application request/result types were added for student registration/edit/eligibility, equipment registration/edit, multi-item checkout, borrowing search and detailed related records. Management repository interfaces and history/search query interfaces exist as foundations. Their presence does not mean the corresponding use cases are implemented.

Operation outcomes explicitly distinguish Succeeded, Rejected, Conflict, Failed and Unknown.

## 5. Session 3 — PostgreSQL and persistence foundation

### Provider and configuration

The operational SQLite provider was replaced with Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3. EF Core, Relational and Design dependencies use 10.0.12, and the repository tool manifest pins dotnet-ef 10.0.12. Explicit runtime references corrected a dependency conflict in which dependent projects resolved EF 10.0.4 while Infrastructure compiled against 10.0.12.

Runtime and design-time configuration use `EQUIPMENT_BORROWING_CONNECTION_STRING`. Authenticated local access was verified, and the dedicated development database `equipment_borrowing_lab4` was created. Its local connection setting is stored in the Windows user's environment, outside the repository. Credentials were not added to tracked files; a repository scan found no supplied password at the Session 3 checkpoint.

The development database was left empty pending successful EF migration execution. Disposable databases were used separately for SQL verification.

### Migrations and schema

Historical SQLite migration source was retained as `.cs.txt` under `archive/sqlite-migrations`, outside compiled EF migration discovery. A new authored migration, `20261114000100_InitialPostgres`, and a frozen model/snapshot were added. The migration was authored because the CLI could not load the project assembly under Application Control; it must not be described as successfully scaffolded or executed by EF.

The schema includes:

- Generated integer identities and primary keys for Students, Equipment and Borrowings.
- Required descriptive/identity fields and checks rejecting blank values.
- Database-generated canonical identifier columns and unique indexes.
- Whitespace trimming checks using a whitespace set matching .NET Trim, including tabs.
- Restrictive foreign keys from each borrowing to its student and equipment.
- PostgreSQL date columns and expected/actual return consistency checks.
- A partial unique equipment index applying only to Active borrowings, allowing returned history while preventing two active loans for one item.

The database generates canonical keys; callers cannot submit a false canonical value. Domain normalization uses invariant uppercase, while PostgreSQL generates persisted canonical values. Future uniqueness-prevalidation queries must canonicalize input in PostgreSQL too so Unicode/locale differences cannot make application checks disagree with database enforcement.

The three-item student limit and synchronized equipment availability remain workflow responsibilities; the schema alone does not enforce them.

### Context and transaction lifetimes

Standalone repository calls create and dispose fresh contexts. This replaces the earlier root-resolved context that could live for the window's lifetime.

IBorrowingSessionFactory creates one context and a serializable transaction for a complete write operation. Its repository implementations share that context. SaveChanges flushes changes inside the transaction; CommitAsync establishes the commit boundary. Session 4 connected the borrowing and return services to this foundation.

### Startup and initial data

Infrastructure owns migration/seeding through PostgresBootstrapper and DbInitializer. Example students/equipment are inserted only when the corresponding table is empty. Seeds use generated internal IDs and do not overwrite existing records. Repeat-safe startup behavior is implemented but has not yet been demonstrated through EF execution.

## 6. Session 4 — continuation workflows UC-01–05

### UC-01: View Equipment Catalog

The catalog source now displays equipment identifiers, names, descriptions and availability through an Application query service. Loading and connection-error messages are separate from action feedback. Failed loads clear stale selections and disable writes until recovery.

### UC-02: Find Available Equipment

An Available items only option is applied using the Apply filter / Refresh action. The PostgreSQL query requires both an available flag and the absence of an active borrowing. Empty results produce an explicit no-match message.

### UC-03: Borrow Equipment

The screen accepts a student, selected equipment and expected return date. Production execution opens the shared transaction before revalidating dates, current references, eligibility, active count, equipment availability and existing active loans.

Equipment unavailability and the new borrowing are written within that transaction. The service returns success only after commit acknowledgement. Missing/stale records and conflicts receive safe explanations and a read refresh rather than automatic write replay.

### UC-04: View Active Borrowings

The asynchronous related-data query joins borrowings with students and equipment. The screen displays student name/number, equipment name/identifier, checkout date, expected return date and status. Returned records are excluded. Loading, no-active-record and connection-error feedback are distinct.

### UC-05: Return Equipment

Staff must select a borrowing and confirm the return. The service revalidates that the record is active, records the local actual return date and restores availability in the same transaction. Restricted students may still return existing loans. Repeated and missing/stale returns do not produce a successful write.

### Error, recovery and startup behavior

The Infrastructure failure mapper classifies serialization/deadlock/uniqueness/reference conflicts without exposing provider details. Unknown commit outcomes are reported explicitly; no automatic retry is enabled. The affected screen blocks its write command until a successful refresh, after which staff reviews the current state before deciding whether to submit again. Known persistence failures also require refresh before resubmission.

If a write succeeds but subsequent list loading fails, the write-success message remains and the separate load message reports the read failure. Busy flags prevent repeated submission on the same screen.

Startup now creates the window before asynchronous database initialization. An Application startup service returns safe failure feedback and supports Retry startup. Views and ViewModels contain no SQL, connection configuration or direct migration calls.

The optional no-session execution path is retained for in-memory demo/tests. It does not provide a database atomicity guarantee; production DI registers the session factory.

## 7. Verification results and their limits

| Verification | Observed result | Interpretation |
|---|---|---|
| Final Session 4 test-project build and references | Zero warnings/errors | Domain, Application, Infrastructure and test source compile |
| Full solution build | Non-Desktop projects compile; Desktop MSB4061 | Full solution build is not passing |
| Initial Session 2 domain execution | 10 cases passed on that revision | Historical partial runtime evidence; not an all-tests-pass result for current HEAD |
| Added clock-boundary case/final domain revision | Compiled; execution blocked | Not runtime-verified |
| PostgreSQL frozen-model test | Compiled; policy blocked Domain.dll before assertions | Snapshot/runtime equality and migration discovery remain unverified |
| Seven Session 4 workflow tests | Compiled; all blocked loading Application.dll | No passing workflow test count or behavioral result established |
| Edited AXAML parsing | Well-formed XML | Does not prove Avalonia binding compilation or rendering |
| PostgreSQL SQL script | Exit 0; constraint/rollback checks passed | Establishes tested SQL behavior, not EF/UI integration |
| Git whitespace checks | Passed at implementation/documentation checkpoints | Source/document formatting check only |

The seven workflow tests are designed to check successful coordinated commit, invalid requests without writes, no commit after a failed second write, unknown commit without replay, active-loan protection despite stale availability, restricted-student return and repeated-return rejection. Recording fakes do not prove PostgreSQL rollback.

### Actual PostgreSQL SQL checks completed

The final verification database is `equipment_borrowing_verify_20261114030628109733`. The earlier `equipment_borrowing_verify_20261114030103322874` is also retained for inspection.

1. Exact migration Up SQL created the schema in a new database; the verification script checked equality with the migration literal.
2. Case-duplicate student/equipment identifiers were rejected with SQLSTATE 23505.
3. Surrounding-space/tab student identifiers and blank names were rejected with 23514.
4. A borrowing referencing a nonexistent student was rejected with 23503.
5. Equal checkout/expected dates and Returned state without an actual return date were rejected with 23514.
6. A second active borrowing for one item was rejected with 23505.
7. A transaction updated availability and inserted a borrowing before a deliberately conflicting insert failed. Fresh-connection reads confirmed unchanged availability/counts: no partial changes survived.
8. A returned record was preserved and a subsequent active loan for the same item was permitted.

No EF migration-history rows were fabricated. No GUI, restart-persistence or concurrent application-instance success is claimed by these checks.

## 8. Required use-case status

| Use case | Source status | Acceptance status |
|---|---|---|
| UC-01 Catalog | Implemented | A01 pending runtime demonstration |
| UC-02 Available equipment | Implemented | A02 pending runtime demonstration |
| UC-03 Single borrowing | Transactional source integrated | A03 pending EF/UI/readback/restart evidence |
| UC-04 Active borrowings | Joined query and screen implemented | A04 pending runtime demonstration |
| UC-05 Return | Transactional source integrated | A05 pending EF/UI/readback/restart evidence |
| UC-06 Multi-item checkout | Request/rule/session foundations | Implementation and A06 pending |
| UC-07 Student history | Query contract foundation | Implementation and A07 pending |
| UC-08 Equipment history | Query contract foundation | Implementation and A08 pending |
| UC-09 Search/filter/sort | Request/query contract foundation | Implementation and A09 pending |
| UC-12 Register student | Model/request/repository contract foundation | Session 5 implementation and A12 pending |
| UC-13 Update student | Domain edit method and request foundation | Session 5 implementation and A13 pending |
| UC-14 Change eligibility | Domain method and request foundation | Session 5 implementation and A14 pending |
| UC-15 Register equipment | Model/request/repository contract foundation | Session 6 implementation and A15 pending |
| UC-16 Update equipment | Protected domain edit method/request foundation | Session 6 implementation and A16 pending |

A17 has partial SQL integrity evidence, but clean EF migration/history evidence remains pending. A18 still needs real application failure/recovery/concurrency demonstrations. A19 has planning/progress/traceability documents, but the final diagram/catalog/adapted fourteen-case suite, complete evidence matrix and reproducible final setup walkthrough remain unfinished.

## 9. Git development timeline

Every authored project source/documentation change through the reviewed checkpoint was committed and pushed. Generated build outputs, databases and local credentials remain outside Git.

| Commit | Recorded work |
|---|---|
| `5532159` | Preserved earlier SQL examples and agent template |
| `a5fd661` | Fixed unavailable-equipment test arrangement |
| `1a83242` | Session plan, baseline evidence, acceptance map and report/review copies |
| `914674c` | Session 1 pull-request merge into main |
| `1101f1d` | Domain fields, identity/date rules, contracts and tests |
| `4460840` | Session 2 decisions and evidence limits |
| `9b996d4` | PostgreSQL configuration, authored migration, constraints, contexts and SQL verification tool |
| `274a470` | PostgreSQL setup and Session 3 evidence |
| `e7263f7` | Session 3 pull-request merge into main |
| `a710619` | Transactional continuation workflows and updated screens |
| `ba765fd` | Incorporated upstream main's Session 3 checkpoint into development |
| `fb81574` | Session 4 evidence and current documentation |

This report is a later documentation checkpoint; its own commit is discoverable with `git log --oneline -- docs/lab4/development-report-sessions-1-4.md`.

## 10. Main project artifacts

| Artifact | Purpose |
|---|---|
| [session-plan.md](session-plan.md) | Detailed eleven-session implementation sequence |
| [progress.md](progress.md) | Session ledger and current next step |
| [acceptance-matrix.md](acceptance-matrix.md) | Required cases mapped to implementation/evidence |
| [domain-decisions.md](domain-decisions.md) | Domain, identity and calendar decisions |
| [postgres-setup.md](postgres-setup.md) | Local configuration, dependencies, migration commands and context ownership |
| [postgres-schema.sql](postgres-schema.sql) | Exact authored PostgreSQL migration Up SQL |
| [session3-evidence.md](session3-evidence.md) | Observed SQL checks and EF execution limits |
| [session4-evidence.md](session4-evidence.md) | Workflow changes and pending acceptance walkthrough |
| [labs1-3-report.md](labs1-3-report.md), [labs1-3-review.md](labs1-3-review.md) | Historical report and findings retained for context |
| [SQL verification script](../../tools/verify-postgres-schema.py) | Reproducible disposable-database constraint/rollback checks |

## 11. Remaining work and recommended sequence

The next planned implementation checkpoint is Session 5: student registration, locating/editing records and eligibility workflows. Session 6 adds equipment management; Session 7 adds atomic multi-item checkout; Sessions 8–9 add histories and search/filter/sort; Sessions 10–11 complete integrated verification and submission documentation.

In parallel with those source tasks, an approved development environment or administrator-managed policy allowance is required to execute blocked assemblies. Before operational acceptance:

1. Run the frozen-model comparison and confirm no pending model changes.
2. Apply EF migrations to the empty development database and inspect actual migration history.
3. Run startup/seeding twice and verify durable data without overwrites.
4. Build/render the Desktop application and verify bindings, navigation, date selection and confirmation behavior.
5. Execute A01–A05 with fresh database reads and borrow/restart/return/restart evidence.
6. Demonstrate real workflow rollback, conflicts, unknown-outcome handling and recovery using disposable data.
7. Finish remaining required use cases and their acceptance evidence.

No overall completion percentage or passing final grade is assigned: session counts and compiled source do not establish rubric completion. The accurate current description is **Sessions 1–4 source work completed, PostgreSQL SQL integrity checks passed, and runtime acceptance plus later required workflows outstanding**.
