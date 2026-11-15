# Campus Equipment Borrowing System — Lab 4 development report

Reporting date: 14 November 2026. Coverage: implementation Sessions 1–10 and Session 11 documentation/handoff. This report describes delivered work and observed verification; it does not claim completion of unperformed manual submission actions.

## Executive result

The Labs 1–3 application was continued with PostgreSQL persistence, expanded student/equipment identities and management, atomic coordinated checkout, related histories and borrowing retrieval controls. All fourteen required Lab 4 use cases have source implementations. Earlier Git history and useful laboratory documentation are preserved.

The verified code baseline is `1e5328dd14e0774ea789c002b3c80cabc1d40f2e`. [GitHub Actions run 43558553901](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43558553901) establishes:

- Complete Release solution, including Avalonia Desktop: zero warnings and zero errors.
- PostgreSQL-enabled backend suite: 76 passed, zero failed/skipped.
- Avalonia Headless suite: 3 passed, zero failed/skipped.
- Five separate service-process stages: init, borrow, readback, return and readback all passed.

Session 11 adds requirement/submission documentation without altering those application behaviors. The complete staff visual/usability walkthrough, actual Desktop close/reopen and private submitter details remain explicit [handoff items](submission-handoff.md).

## Scope delivered

| Area | Delivered user goals |
|---|---|
| Continuation | UC-01 catalog, UC-02 available items, UC-03 single borrowing, UC-04 active list, UC-05 return |
| Coordinated borrowing | UC-06 checkout of multiple distinct items as one atomic request |
| Histories and retrieval | UC-07 student history, UC-08 equipment history, UC-09 text/status/date search/filter/sort/reset |
| Student management | UC-12 registration, UC-13 descriptive edits, UC-14 permit/restrict eligibility |
| Equipment management | UC-15 registration, UC-16 descriptive edits with transaction fields protected |

The actor is Laboratory Staff / Equipment Custodian. Students are stakeholders rather than a self-service actor. PostgreSQL supports all fourteen cases. UC-10–11 and UC-17–27 remain Lab 5; UC-28–33 are future scope. Internal validation is not a separate use case or an unnecessary include/extend relationship.

## Session-by-session work

| Session | Work completed |
|---|---|
| 1 | Preserved pre-existing changes/history; established branch and requirements plan; corrected unavailable-equipment test arrangement; inspected SDK/PostgreSQL and diagnosed Windows assembly policy blocks |
| 2 | Added staff identifiers/canonical values, optional contact/description, protected edits, three-item rule, DateOnly checkout/expected/actual dates, injectable campus clock, reliable IDs and Application contracts |
| 3 | Replaced operational SQLite with Npgsql; authored PostgreSQL migration/frozen model, schema constraints, generated identities, fresh contexts, bootstrap/seeding and serializable write-session foundation; verified SQL constraints/rollback in disposable databases |
| 4 | Integrated transaction boundaries into single borrowing/return; added related active queries, date picker, availability filter, confirmation and safe loading/error/Unknown/startup retry behavior |
| 5 | Added student registration/find/edit/eligibility services, PostgreSQL uniqueness prevalidation, stale snapshots, Students screen and service tests |
| 6 | Added equipment registration/find/edit services and screen, description clearing, protected availability/history and stale-availability checks |
| 7 | Added coordinated multi-item service/screen, shared single/multi checkout validation, all-item prevalidation, controlled after-write test seam, recording and real EF/PostgreSQL success/rollback tests |
| 8 | Added student/equipment history services/queries and master-detail screen, explicit empty/missing/error states, cancellation/version guards and actual PostgreSQL membership/date tests |
| 9 | Added database-side literal partial search across three fields, status/date criteria, deterministic ties, combined/reset controls and controlled PostgreSQL assertions |
| 10 | Added Linux CI, real integrated lifecycle/concurrency/recovery checks, headless UI tests and separate-process persistence probe; fixed full-build warnings and test-host/test-assumption defects; obtained the successful 79-test/five-stage result |
| 11 | Assembled the current README, preserved historical README/matrix, retained the full supplied fourteen-case field structure, added scope catalog/diagrams/business rules, architecture/ERD, final traceability, report, history export, demonstration script and handoff checklist |

## Domain and Application changes

Student has a stable generated ID, institutional number and normalized key, full name, optional contact and eligibility. Registration defaults to eligible. Edits can change number/name/contact while preserving internal identity; eligibility uses a separate action. A fixed maximum of three Active items counts both existing loans and every requested item. Restriction blocks new issues without cancelling loans or preventing returns.

Equipment has a stable generated ID, editable staff identifier and normalized key, name, optional description and availability. New items are available. The descriptive edit request cannot replace availability, borrowing status or return dates. Snapshot checks detect a selected record that changed before submission, including an availability change.

Borrowing stores student/equipment links, local checkout/expected/actual dates and Active/Returned status. Expected return must follow checkout; actual return cannot precede checkout, but same-day and early return are allowed. CampusClock uses Asia/Manila by default and supports injected time/timezone dependencies. Random borrowing IDs were replaced by database generation and a sequential in-memory adapter for learning/tests.

Application services express the user goals and safe outcomes. Query/session/repository interfaces keep EF and provider details outside Domain/ViewModels. The in-memory ConsoleDemo path remains historical/demo behavior and does not provide a production database atomicity guarantee.

## PostgreSQL transition and schema

Operational configuration uses EQUIPMENT_BORROWING_CONNECTION_STRING. Npgsql EF provider 10.0.3 is paired with explicit EF Core/Relational/Design 10.0.12 dependencies; dotnet-ef is pinned to 10.0.12. This corrected a transitive runtime-version mismatch during implementation.

InitialPostgres was authored when local CLI scaffolding was blocked. Its frozen snapshot is checked against the runtime model and applied to clean databases in CI with genuine __EFMigrationsHistory records. SQLite migration source is retained outside compiled discovery. The activity did not require historical row import or simultaneous provider support.

The relational schema enforces generated primary keys, restrictive foreign keys, required/nonblank fields, canonical student/equipment identifier uniqueness, date/state checks and a partial unique Active borrowing per equipment. Canonical values are generated by PostgreSQL; prevalidation canonicalizes input in PostgreSQL too. Database constraints supplement Application rules. The per-student limit and availability synchronization are coordinated by transactional workflows, not a standalone schema quota constraint.

Startup applies migrations and seeds example student/equipment records only when the corresponding table is empty, without overwriting custom rows. Generated internal IDs avoid explicit-ID sequence collisions. CI verifies repeat initialization and preservation of edited data.

## Transaction, concurrency and recovery design

Standalone queries/repository calls own short-lived contexts. Write sessions share one context and serializable transaction across current-state validation and repository saves. Multi-item checkout copies request IDs, rejects duplicate/fewer/invalid selections, rechecks every reference/rule and validates all items before the first mutation. One Active borrowing per item and every availability change commit together.

Known pre-commit failures leave no partial committed request changes. The rollback integration test deliberately throws after the first equipment update/borrowing insert has actually flushed, then verifies unchanged state through a fresh context. Competing-request tests check quotas and one Active item loan under overlapping tasks; they do not force every possible interleaving or replace load testing.

Conflicts are reported with refresh/review. No policy automatically retries writes. A lost/uncertain commit acknowledgement is represented as Unknown, not false success or asserted rollback. Typed provider mapping and injected service outcome tests cover this path; a real network interruption precisely at commit acknowledgement was not induced. Failed connection and restored-access behavior are verified against a safe unavailable endpoint and a separate working database factory.

## Histories, search and UI behavior

Related history queries filter the selected stable student/item ID and retain both Active and Returned loans, including checkout/expected/actual dates and related identifiers/names. They distinguish missing subjects from valid subjects with no history. Names shown are the current related descriptive values; immutable historical name snapshots are not a supplied Lab 4 requirement.

Search uses trimmed case-insensitive literal partial text over student name, institutional number or equipment name. LIKE wildcard characters are escaped. Statuses and either-date/direction sorting combine server-side; borrowing ID ascending is the stable tie-breaker. Clear restores empty search, All status and default sort.

History/search screens expose loading, empty/missing/error and retry states. Advisory cancellation is paired with request versions so late results cannot replace newer selection/criteria. Navigation invalidates outstanding reads. Management forms copy fields rather than editing selected entities directly, and no View/ViewModel runs SQL, configures connections or executes migrations.

Headless tests exercise actual control layouts, selected bindings, asynchronous search response ordering/reset and history selection/empty/missing behavior with fake reads. These are distinct from all user actions against a visible Desktop window and from pixel/accessibility/usability review.

## Evidence and practical limits

The initial Windows machine intermittently blocked unsigned assemblies and Avalonia tasks with 0x800711C7/MSB4061; Code Integrity logs identified policy blocks. No security setting was disabled. Linux CI provided reproducible full compilation and database/UI automation. Earlier blocked, failed, fixed and cancelled runs remain inspectable in Git/Actions.

The verified final run includes real clean EF migrations/model checks, domain/service cases, controlled database search/history/rollback tests, integrated registration-edit-borrow-restrict-return-permit behavior, repeat-safe seeds, competing requests, connection recovery, headless controls and five service-process persistence stages. Actual database readbacks use fresh contexts/connections. Passwords were not committed or sent to CI; CI uses unrelated disposable credentials.

The PersistenceProbe starts five separate processes and proves Active/unavailable and then retained Returned/available state survive producer-process exit. It uses actual Application services and EF, but is not the actual Avalonia Desktop executable. The local operational development database was not overwritten by these tests.

[Final traceability](final-traceability.md) records the remaining per-case branches rather than claiming every acceptance item passed. In particular, actual Desktop restart, complete visible staff walkthrough, post-history student editing, direct displayed catalog/available/active-list comparisons and visual/accessibility checks remain manual. No completion grade or blanket acceptance percentage is asserted.

## Documentation and Git handoff

The [current README](../../README.md) is standalone and reproducible; earlier reflections are archived. The [suite](use-case-suite.md) preserves fourteen cases times eighteen supplied fields, including actors, flows, exceptions, rules and acceptance outcomes, with implementation notes. The [catalog](use-case-catalog.md) preserves all 33 scope IDs. Mermaid/PlantUML diagrams contain exactly the required fourteen goals. BR-01–19, architecture/schema and final evidence mapping are linked together.

Meaningful backend/UI/tests/evidence commits were pushed separately throughout implementation. From Session 5 onward, titles and bodies explicitly identify the progress added and observed validation limits, as requested. The [history export](git-history.txt) provides a concise archive companion; the actual repository retains full history.

The [submission handoff](submission-handoff.md) inventories the package and unchecked actions. Each student must still submit as instructed and supply their own/both partners' full names and IDs privately. Those details were not supplied and were not invented. No course-platform submission, deployment or final grading decision was made by this work.
