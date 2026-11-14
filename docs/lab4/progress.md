# Lab 4 progress ledger

Current consolidated report: [Lab 4 development report through Session 11](development-report-lab4.md). Earlier [Sessions 1–4 report](development-report-sessions-1-4.md) and session-specific evidence are retained as historical records. Full manual acceptance is not claimed.

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

Planned Sessions 1–11 source/documentation work is delivered. Complete the unchecked [submission handoff](submission-handoff.md) and [manual walkthrough](manual-verification-checklist.md): real Desktop visual/usability/close-reopen evidence, instructor-specific demonstration and private submitter/partner details. Local Windows policy is unchanged; no course-platform submission or grading claim was made.

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

## Session 5 — student-management source implemented; runtime acceptance pending

Date: 2026-11-14. Added UC-12 registration/default eligibility, UC-13 lookup/edit/contact clearing and UC-14 permit/restrict workflows. Backend `de8d5bd` adds serializable management sessions, PostgreSQL-canonical uniqueness prevalidation and stale-selection snapshots. UI `34009f8` adds Students navigation, search/form/actions and loading/error/unknown-outcome recovery. Added ten service test cases and [Session 5 evidence](session5-evidence.md).

Test project and Domain/Application/Infrastructure compile with zero warnings/errors. Targeted test discovery is blocked by Application Control loading Tests.dll (0x800711C7). Desktop still fails at Avalonia MSB4061. New/edited AXAML is well-formed XML, but bindings/rendering and A12–A14 PostgreSQL/restart evidence remain unverified. No schema/database changes were made in this session.

User's commit preference: action-oriented titles naming the progress added, with detailed bodies explaining the session, changed behavior and actual validation/limits. Commit/push backend, UI, tests and evidence as separate coherent parts. Do not rewrite earlier commits to change their wording.

## Session 6 — equipment-management source implemented; runtime acceptance pending

Date: 2026-11-14. Backend `e6e9b20` implements UC-15 registration with available defaults and UC-16 descriptive updates through serializable sessions, PostgreSQL uniqueness prevalidation and stale snapshots including availability. UI `f9c7ceb` adds Manage Equipment navigation, lookup, registration/edit forms, description clearing and read-only availability. Tests `e4760d7` add ten service cases. Each part was committed/pushed independently with descriptive titles and detailed progress/validation bodies.

Non-Desktop projects and tests compile with zero warnings/errors on the targeted build. Test discovery remains blocked by Application Control loading Tests.dll (0x800711C7); Desktop still fails at Avalonia MSB4061. Edited AXAML parses as XML, but bindings/rendering and A15–A16 database/restart evidence remain pending. No schema/database change or security-policy modification was made. Details: [Session 6 evidence](session6-evidence.md).

## Session 7 — multi-item checkout source implemented; A06 runtime evidence pending

Date: 2026-11-14. Backend `43bb1a0` adds shared single/multi-item validation and coordinated writes in one serializable session; every selected item is validated before writing. UI `b1169c3` adds Checkout multiple, checkboxes/count, student/date selection and recovery states. Tests `62ece26` add 15 recording cases and two opt-in disposable PostgreSQL success/rollback tests with a failure observer after actual persisted writes. No production failure switch is registered.

Non-Desktop code/tests compile. Targeted recording tests failed before assertions due to Domain.dll Application Control loading; integration tests initially skipped without opt-in configuration. An explicit locally configured integration run was attempted and both cases were also blocked loading Domain.dll. No database/rollback pass is claimed. Desktop still fails Avalonia MSB4061; AXAML is well-formed XML but binding/rendering remains unverified. Details and A06 walkthrough: [Session 7 evidence](session7-evidence.md).

## Session 8 — histories implemented; backend suite and PostgreSQL tests pass

Date: 2026-11-14. Queries/services `5f84474` add subject lists and student/equipment-specific histories with both statuses, joined identities/dates and distinct empty/missing/error outcomes. UI `add03d6` adds the Histories screen with mode/subject selection, loading/retry, cancellation/version protection and navigation invalidation. Tests `c1d0c2e` add service/coordinator and real PostgreSQL membership/date/no-history checks.

Observed results: targeted history run passed 7/7; complete suite with local PostgreSQL integration enabled passed 66/66, zero failures/skips. Earlier model comparison, EF disposable clean migrations and actual checkout success/rollback tests now pass too. Fresh psql reads confirm migration history and borrowing counts in retained disposable databases. This supersedes earlier backend test-loading blocks for current binaries; no security policy was changed. Desktop still fails Avalonia MSB4061. AXAML is well-formed XML but rendering/bindings/UI responsiveness remain unverified. Development DB startup/seeding and broader restart/concurrency checks remain pending. Details: [Session 8 evidence](session8-evidence.md).

## Session 9 — search/filter/sort source implemented; current runtime blocked

Date: 2026-11-14. Backend `358b2bb` adds trimmed literal case-insensitive partial search over the three required fields, composable statuses and either-date/direction sorting with ID ascending tie-breaks, all executed in PostgreSQL. UI `dff3d7f` adds Search borrowings, Apply/Retry, clear/reset, cancellation/latest-result guards and distinct result/error states. Tests `a289aae` add six service cases and one opt-in PostgreSQL case covering controlled matching, literal wildcard inputs, all statuses/sorts/ties, combined criteria and reset.

Non-Desktop code/Tests compile with zero warnings/errors. Initial targeted search execution had seven assembly-load failures. Final full-suite execution was unsuccessful under Application Control; an isolated PostgreSQL search test confirmed Domain.dll blocked before assertions (0x800711C7). No current search or full-suite pass is claimed. Session 8's 66/66 result remains historical for its revision. Desktop still fails Avalonia MSB4061; edited AXAML is well-formed XML but rendering is unverified. No schema/database/security-policy changes were performed. Details: [Session 9 evidence](session9-evidence.md).

## Session 10 — integrated PostgreSQL and full-build verification

Date: 2026-11-14. Added GitHub Actions Linux verification (`64d6f4a`) to build the complete solution and run PostgreSQL tests independently of local Windows policy. The first CI run passed all 73 existing tests and compiled Desktop. Integrated lifecycle/concurrency/recovery tests (`d8bb810`) then passed with the complete 76-test backend suite. Repeat-safe startup seeding, registration/edits, restriction/return/permit, quota and active-item competition, histories/search and connection recovery now have real PostgreSQL evidence.

Added headless UI tests (`2da327d`) and a five-process persistence probe (`8ad740b`). CI exposed and prompted fixes for four Desktop warnings (`9109b66`), test-app namespace ambiguity (`9399d10`), xUnit v3 executable-host configuration (`3de02f3`) and two headless test assumptions (`1e5328d`). Zero-warning full builds and 76 backend passes are confirmed; the corrected combined UI/process run is recorded in [Session 10 evidence](session10-evidence.md).

Final combined CI run [43558553901](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43558553901) at `1e5328d` passed the complete Release build with zero warnings/errors, all 76 backend tests, all 3 headless UI tests (79 total; zero failed/skipped), and all five separate-process init/borrow/readback/return/readback stages. TRX evidence is retained on the run. This closes automated current-search, headless selection/binding and service-process persistence gaps; full manual real-Desktop visual/restart acceptance remains separate.

No Windows security policy was changed and the local development database was not overwritten. Automated headless/control/process checks do not replace manual pixel/usability review or restarting the real Desktop executable. Use [continuous verification instructions](continuous-verification.md) and the [remaining manual checklist](manual-verification-checklist.md). Preserve earlier failed/blocked runs as historical evidence rather than presenting them as current code defects or passes.

## Session 11 — final requirements, report and submission documentation delivered

Date: 2026-11-14. Requirements commit `1496ee8` adds the adapted fourteen-case suite, all 33 catalog classifications, matching Mermaid/PlantUML diagrams, BR-01–19, architecture/ERD and final acceptance traceability. Handoff commit `c181edc` adds the clean reproducible README, consolidated report, demonstration script, submission inventory and concise Git history, preserving previous README/matrix versions in archive.

Validation compared all 14 cases times 18 fields (252 original values) against the supplied suite PDF; all names/classifications in the 33-case catalog and all 19 rule IDs were checked; both diagrams contain the same fourteen required goals; current links/code fences were checked; no supplied password was found in repository files. The first catalog-validation attempt matched an earlier narrative reference rather than the Appendix heading; the checker was corrected and the complete catalog comparison passed. Application behavior was not changed, so the Session 10 zero-warning/79-test/five-process-stage code baseline remains applicable; no redundant runtime run was required for documentation-only edits.

Upstream main advanced to `bb2d22b` (Session 10 PR merge) during documentation pushes and the remote feature branch was deleted. The first push encountered a ref-lock error, the next recreated the branch with both documentation commits, and upstream main was incorporated through a normal merge without history rewriting. Final commits and remote synchronization are verified at handoff.

Current entry points: [README](../../README.md), [final development report](development-report-lab4.md), [suite](use-case-suite.md), [traceability](final-traceability.md), [demonstration](demonstration-script.md), [submission handoff](submission-handoff.md). Manual visual/real-Desktop restart and personal submission actions remain unchecked rather than falsely marked complete.

Continuation verification: checked 28 current Markdown documents and 152 local links with no missing targets or unclosed code fences; confirmed all fourteen cases contain eighteen required fields, all 33 catalog classifications, BR-01–19 and matching required IDs in both diagrams. Source, tests, tools, workflow, solution and EF tool manifest have no changes from verified code baseline `1e5328d`. Fetch/fast-forward synchronization found the feature branch current. The final ledger update is committed separately; the documentation PR targets main without merging or marking manual acceptance complete.

## Session status

1. Baseline/traceability: done with documented external execution blocker.
2. Domain/contracts: implemented and compiled; partial runtime verification, external policy blocker remains.
3. PostgreSQL/schema/lifetimes: clean EF migrations/model/constraints and repeat-safe bootstrap verified in disposable CI databases; local execution policy remains restricted.
4. Existing workflows: full source builds in CI; integrated PostgreSQL lifecycle passes; complete staff visual/restart walkthrough remains pending.
5. Student management: service and real integrated lifecycle verification passes; full staff UI acceptance remains pending.
6. Equipment management: service and real integrated lifecycle verification passes; full staff UI acceptance remains pending.
7. Atomic multi-item checkout: real success/after-write rollback and competing-request integrity tests pass; manual UI/real Desktop restart acceptance remains pending.
8. Histories: backend service/coordination and PostgreSQL integration verified; headless/manual UI evidence in Session 10.
9. Search/filter/sort: actual PostgreSQL matching/filtering/all sort directions/ties/reset pass in CI; headless/manual UI evidence in Session 10.
10. Integrated verification: successful full zero-warning build, 79 tests and five persistence process stages in CI; remaining manual visual/real-Desktop restart checks documented.
11. Submission documentation: delivered and validated; manual demonstrations/private submitter actions remain in the handoff checklist.

## Interface refresh after Session 11

Modern CampusEQ theme and seven-screen presentation delivered. The complete Release build passes with zero warnings/errors; four headless UI tests now use the production theme and pass. The actual Desktop window was launched and the catalog appearance inspected. See [UI refresh evidence](ui-refresh.md). An updated committed-revision package supersedes the earlier UI package; full staff/manual acceptance remains pending.

## Disposable demo environment

Created and migrated a separate local demonstration database, launched Desktop with a process-specific connection, and verified three seeded students, three items and zero borrowings through a fresh connection. Saved development configuration remains unchanged. See [demo database and restart command](demo-database.md). Synthetic walkthrough records remain for manual registration.
