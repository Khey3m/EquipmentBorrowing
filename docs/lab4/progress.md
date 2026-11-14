# Lab 4 progress ledger

Consolidated report: [Sessions 1–4 development report](development-report-sessions-1-4.md). This report documents implemented source, observed verification and remaining acceptance work; it does not mark Lab 4 complete.

## Session 1 — baseline established with an external execution blocker

Date: 2026-11-14. Branch: `lab4/postgresql-workflows`. Prior history retained; origin/main was already current at `2970cdb`. Fetch and fast-forward pull succeeded. Feature branch pushes succeeded.

### Git timeline

- `5532159`: preserved pre-existing SQL examples and agent template without changing their content. SQL remains historical SQLite examples, not PostgreSQL acceptance evidence.
- `a5fd661`: corrected unavailable-equipment test arrangement by first successfully borrowing the item, then attempting it again.
- Documentation checkpoint: this ledger, acceptance matrix, session plan and historical report/review copies. Find the exact commit with `git log --oneline -- docs/lab4`.

Every authored source/documentation change is committed. Generated build output, local credentials and database files do not belong in Git. Use explicit staging and separate coherent commits; push each completed part and verify remote synchronization.

### Baseline checks and observed results

| Check | Observed result |
|---|---|
| `dotnet --info` | SDK 10.0.404 / MSBuild 18.9.15, Windows x64; SDK 10.0.400 also installed; no global.json |
| `dotnet build EquipmentBorrowing.slnx --nologo` | Non-Desktop projects compiled; Desktop failed MSB4061 loading Avalonia GenerateAvaloniaResourcesTask |
| SDK 10.0.400 MSBuild comparison | Same Desktop MSB4061 failure; switching SDK did not resolve it |
| `dotnet test tests/EquipmentBorrowing.Tests/EquipmentBorrowing.Tests.csproj --nologo --no-restore` after fixture fix | Test assembly compiled; discovery/execution blocked by Application Control 0x800711C7; no passing test count established |
| Code Integrity Operational log, event 3077 | Explicitly reports Avalonia.Build.Tasks.dll blocked by signing/code-integrity policy; also reports test DLL blocked. This establishes an environment cause, not a proven project-code defect |
| PostgreSQL Windows service | `postgresql-x64-18` Running |
| PostgreSQL client | `C:/Program Files/PostgreSQL/18/bin/psql.exe`, version 18.6; bin directory is not currently on PATH |
| `pg_isready -h localhost -p 5432` | Accepting connections; readiness does not establish authenticated database access |
| Applicable AGENTS.md search | No applicable files found in project or checked ancestor paths |

### External requirements and limits

The managed Windows policy needs an administrator-approved development allowance for the blocked build/test assemblies, or verification must run on an approved development machine/runner. Security settings were not modified. Do not substitute a successful compile for runtime or persistence evidence.

PostgreSQL database name, role and authenticated access remain unverified. Session 3 will use a dedicated local development database and secret-free configuration. Proposed environment variable: `EQUIPMENT_BORROWING_CONNECTION_STRING`; it is a configuration contract to implement in Session 3, not a currently supported setting. Example shape only: `Host=localhost;Port=5432;Database=equipment_borrowing_lab4;Username=<local-role>;Password=<local-secret>`. Set credentials locally, never in tracked files or chat logs. No database was created or modified in Session 1.

Historical report/review files under this directory describe the earlier implementation and review state; later session results belong in this ledger and evidence, rather than silently rewriting historical claims.

### Next session

Session 5: student registration, locate/edit and eligibility workflows (UC-12–14). Before operational acceptance, complete pending EF model/migration/seed verification and A01–A05/A18 walkthrough on an approved environment. Desktop and workflow test execution remain blocked by Windows policy.

## Session 2 — domain and contract foundations completed

Date: 2026-11-14. Session 1 was merged upstream as `914674c`; the working branch fast-forwarded to that merge. The remote feature branch had been deleted after merge, so it was recreated by the Session 2 push. Earlier history is preserved.

Implementation commit: `1101f1d` (pushed). Added staff-facing identifiers and canonical keys, optional contact/description and protected descriptive edits, three-item eligibility rule, DateOnly checkout/expected/actual dates, injectable Manila calendar clock, database-generated borrowing ID strategy and sequential in-memory identities. Added management/query/request/outcome contracts and updated seed/caller code.

Validation: test project and ConsoleDemo compile. An initial execution passed 10 domain cases while the three existing service tests were blocked loading Infrastructure.dll. After adding the clock test/contracts, recompilation succeeded but the rebuilt test DLL was blocked during discovery; no final all-tests-pass claim. Desktop still fails MSB4061 under the previously identified Application Control policy.

Important: the model intentionally advances beyond the historical SQLite schema. Do not treat this intermediate commit as database-runnable; clean PostgreSQL migrations are Session 3. Atomic repository writes and concurrency guarantees remain later-session work. Details and exact evidence limits: [domain-decisions.md](domain-decisions.md).

## Session 3 — PostgreSQL foundation implemented; SQL verified, EF execution pending

Date: 2026-11-14. Implementation commit `9b996d4` pushed. Added Npgsql 10.0.3, aligned EF runtime dependencies to 10.0.12, pinned the local EF tool, and archived historical SQLite migrations outside compiled discovery. Added authored InitialPostgres migration/frozen target model, date/required/normalized-unique/foreign-key/active-item constraints and fresh-context repositories. Registered the Infrastructure bootstrap and serializable write-session factory; the existing borrowing/return services do not consume the session yet (Session 4).

Verified supplied credentials against local PostgreSQL without printing or committing them. Created empty equipment_borrowing_lab4 and saved its connection setting in the current Windows user's environment. Restart IDE or load that user variable into the process before running tools. No credential file was created; repository scan found no supplied password.

Validation: test project and references compile with zero warnings/errors after package alignment. Full solution's non-Desktop projects compile; Desktop remains blocked at Avalonia MSB4061. EF scaffolding and the model-comparison test hit confirmed Application Control assembly-load failures. Migration/history/model agreement, startup seeding and desktop operation remain unverified.

Real PostgreSQL verification applied exact migration Up SQL in a separate disposable database and passed uniqueness (case variants), whitespace/blank checks (including tabs), FK/date/state checks, single-active-item protection, history retention and rollback after attempted writes with fresh-connection readback. No EF history was fabricated. The development database is still empty pending approved EF execution. Details: [setup](postgres-setup.md) and [observed evidence](session3-evidence.md).

README was converted from UTF-16 to UTF-8 for editable current setup links; historical content remains. Every source/documentation change is committed; generated output and local secrets remain outside Git.

## Session 4 — existing workflow source integration completed; runtime acceptance pending

Date: 2026-11-14. Implementation `a710619` pushed. Borrowing/return validation and all writes now use the shared serializable transaction in production and report success only after commit. Added active-item revalidation, safe conflict/failure/unknown outcomes, read refresh without write replay, joined asynchronous queries, date selection, available filter, return confirmation, loading/empty/error states and startup retry. ViewModels now call Application services.

Build of Tests/Domain/Application/Infrastructure passed with zero warnings/errors. Full solution still fails at Avalonia task loading. Seven workflow recording-session tests compiled but were blocked by Application Control loading Application.dll; no passing workflow test count. Edited AXAML is well-formed XML, but bindings/rendering remain unverified. No new database operations or schema changes were performed in this session. See [Session 4 evidence and pending walkthrough](session4-evidence.md).

GitHub main advanced to Session 3 merge `e7263f7` while work was underway and the remote feature branch was deleted after that merge. The implementation push recreated it; a normal merge then incorporated upstream main without conflicts or history rewriting. All implementation and documentation changes are committed/pushed.

## Session status

1. Baseline/traceability: done with documented external execution blocker.
2. Domain/contracts: implemented and compiled; partial runtime verification, external policy blocker remains.
3. PostgreSQL/schema/lifetimes: implemented, SQL verified; EF/runtime acceptance pending external policy blocker.
4. Existing workflows: source integrated and non-Desktop code compiled; runtime acceptance blocked/pending.
5. Student management: pending.
6. Equipment management: pending.
7. Atomic multi-item checkout: pending.
8. Histories: pending.
9. Search/filter/sort: pending.
10. Integrated verification: pending.
11. Submission documentation: pending.
