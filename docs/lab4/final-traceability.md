# Final use-case and acceptance traceability

Verified code baseline: `1e5328dd14e0774ea789c002b3c80cabc1d40f2e`. [CI run 43558553901](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43558553901): full Release build, zero warnings/errors, 76 backend tests, 3 headless UI tests, zero failed/skipped, and five successful separate-process persistence stages. Session 11 changes are documentation-only.

**Evidence interpretation:** implemented does not mean every staff demonstration branch was performed. PostgreSQL integration, service recording fakes, headless controls and actual Desktop manual checks have different scopes. Remaining checks below are not reported as passed. Setup/action/expected/observed details are in [Session 10 evidence](session10-evidence.md), the linked test code and TRX artifacts on the run page. Historical Session 3 SQL constraint checks remain supporting evidence.

## Evidence key

| Key | Concrete source / observed evidence |
|---|---|
| L | [IntegratedLifecyclePostgresTests](../../tests/EquipmentBorrowing.Tests/IntegratedLifecyclePostgresTests.cs): real lifecycle, repeat seeding, duplicate rejection, stable identities, eligibility/return/permit, quota, histories/search, competing requests and failure/recovery |
| M | [MultiItemPostgresTests](../../tests/EquipmentBorrowing.Tests/MultiItemPostgresTests.cs): real two-item success and rollback after the first item's update/insert, verified through fresh contexts |
| H | [BorrowingHistoryPostgresTests](../../tests/EquipmentBorrowing.Tests/BorrowingHistoryPostgresTests.cs): distinguishable student/item histories, statuses/dates, missing and no-history subjects, unchanged counts |
| Q | [BorrowingSearchPostgresTests](../../tests/EquipmentBorrowing.Tests/BorrowingSearchPostgresTests.cs): all required fields/statuses/date directions, ties, combined/reset/no-match and literal wildcard text |
| U | [WorkflowUiTests](../../tests/EquipmentBorrowing.UiTests/WorkflowUiTests.cs): all seven view layouts, bound search Apply/Clear/latest result, history selection/empty/missing states on Avalonia's in-memory platform |
| P | [PersistenceProbe](../../tools/EquipmentBorrowing.PersistenceProbe/Program.cs): init, borrow, readback, return, readback in five distinct processes; real services/database, not the real Desktop executable |
| F | [PostgresModelTests](../../tests/EquipmentBorrowing.Tests/PostgresModelTests.cs), actual EF migration/history from integration tests, [migration](../../src/EquipmentBorrowing.Infrastructure/Persistence/Migrations/20261114000100_InitialPostgres.cs) |
| S | [SQL verification script](../../tools/verify-postgres-schema.py) and [Session 3 results](session3-evidence.md): required/unique/FK/date/state/active-item enforcement and fresh-read rollback; SQL-only evidence |
| V | Service/domain tests: [borrow](../../tests/EquipmentBorrowing.Tests/BorrowEquipmentServiceTests.cs), [workflow sessions](../../tests/EquipmentBorrowing.Tests/WorkflowSessionTests.cs), [multi-item](../../tests/EquipmentBorrowing.Tests/MultiItemCheckoutTests.cs), [student](../../tests/EquipmentBorrowing.Tests/StudentManagementServiceTests.cs), [equipment](../../tests/EquipmentBorrowing.Tests/EquipmentManagementServiceTests.cs), [history](../../tests/EquipmentBorrowing.Tests/BorrowingHistoryTests.cs), [search](../../tests/EquipmentBorrowing.Tests/BorrowingSearchTests.cs), [domain](../../tests/EquipmentBorrowing.Tests/Lab4DomainTests.cs). Recording fakes do not prove database rollback/concurrency |

## Fourteen cases and cross-cutting acceptance

| Acceptance / case | Implementation | Verified evidence and practical scope | Remaining demonstration / criterion |
|---|---|---|---|
| A01 / UC-01 Catalog | EquipmentViewModel -> ExistingWorkflowQueryService -> EfExistingWorkflowQueries | L checks restored catalog access; U loads/layouts the catalog view; schema/source inspection confirms projected fields | Full displayed catalog-versus-database comparison and empty-catalog visual branch. R1 |
| A02 / UC-02 Available items | Available-only filter requires IsAvailable and no Active loan | Source query implements both predicates; L/M verify the states produced by borrowing/return; U loads the view | Direct available-filter membership, no-match and connection-error staff demonstration. R1 |
| A03 / UC-03 Borrow | BorrowEquipmentService + shared CheckoutValidation + serializable session | L real issue/quota/restriction/readback; V validates rejection and no-success/error paths; P preserves Active/unavailable state across separate processes | Complete single-item invalid/missing/stale UI branches and actual Desktop restart. R1/R2 |
| A04 / UC-04 Active list | Joined EfExistingWorkflowQueries and BorrowingsViewModel | Source excludes Returned and projects related identities/dates; U loads/layouts view; L validates underlying states | Direct displayed-list membership/empty/error comparison with fresh database records. R1 |
| A05 / UC-05 Return | ReturnEquipmentService, confirmation and transactional state/date update | L real restricted-student return and retained dates/history; V repeat rejection; P durable Returned/available state across process boundaries | Full confirmation/stale-selection/repeat staff interaction and actual Desktop restart. R1/R2 |
| A06 / UC-06 Multi-item | MultiItemCheckoutService, complete prevalidation, one shared transaction | M real success and after-write rollback; L quota/same-item competition; V duplicates/fewer/missing/ineligible/date/limit/availability cases | Full multi-select UI demo and every invalid case with before/after readback in the staff demonstration. R3 |
| A07 / UC-07 Student history | Student history mode, BorrowingHistoryService / EfHistoryQueries | H real subject membership/status/date/empty/missing; V cancellation/error/version tests; U actual selection replacement | Complete staff slow-load/connection-error/visual date demonstration. R6/R7 |
| A08 / UC-08 Equipment history | Equipment history mode, same async query boundary | H independent item histories; U mode/selection/empty/missing; V advisory-cancellation/stale-completion safeguards | Complete staff equipment-mode slow-load/error/visual demonstration. R6/R7 |
| A09 / UC-09 Search/filter/sort | BorrowingSearchService / EfBorrowingSearchQueries, Apply/Retry/Clear | Q all required query combinations and stable order; V empty/error/cancellation; U TextBox binding, late results and Clear | Full visual control choices/date formatting/error/retry walkthrough. R6/R7 |
| A12 / UC-12 Register student | StudentManagementService.RegisterAsync / management session | L real generated identity, uniqueness and durable records; V blanks/defaults/trim; P student created in init and found in later processes; S database constraints | Full registration UI and rejection branches after actual Desktop restart. R4/R2 |
| A13 / UC-13 Edit student | FindAsync + UpdateAsync, original-value snapshot | L real edit/contact clearing before issue; V identity/eligibility protection and stale edit; F/S stable links | Explicit edit of a student that already has history, invalid/duplicate edit branches and Desktop restart. R4 |
| A14 / UC-14 Eligibility | Separate permit/restrict request, no borrowing mutation | L restrict/new-issue rejection/return/permit/valid issue; V repeated-state no-write feedback | Staff click-through and persistent eligibility after actual Desktop restart. R4 |
| A15 / UC-15 Register equipment | EquipmentManagementService.RegisterAsync | L real available item with generated ID and duplicate rejection; V blanks/default/description; P durable registration; S uniqueness | Full form/rejection staff demonstration and Desktop restart. R5/R2 |
| A16 / UC-16 Edit equipment | Descriptive request excludes replacement transaction fields; snapshot includes expected availability | L edits a currently borrowed item and preserves unavailable state/history; V stale/missing/duplicate/description cases; U loads view | Demonstrate protected fields and invalid edits through form; Desktop restart/readback. R5 |
| A17 / integrity | Npgsql mappings, authored migration, frozen snapshot and DB constraints | F clean EF migrations/model agreement; S explicit invalid FK/duplicates/second Active item; real generated identities and fresh reads throughout L/M/H/Q | Include live schema/history explanation in walkthrough; no fake history. R2 |
| A18 / recovery | Fresh contexts, serializable session, provider-safe outcome mapping, explicit refresh | L failed connection and restored access; M controlled after-write rollback; V Unknown/no replay; U query response ordering | Real rendered error/retry feedback and source explanation. Actual commit-acknowledgement loss was not induced; no such success is claimed. R3/R7/R8 |
| A19 / documents | Current README, suite, catalog, diagrams, schema, architecture, this matrix, history and handoff | Session 11 consistency/link/field checks; inspectable Git history, CI logs/TRX and reproduction commands | Add any instructor-specific recording and submitter/partner details privately. R8 |

## Business-rule coverage index

| Rules | Implementation / evidence |
|---|---|
| BR-01–04 | Student domain/management and generated normalized key; L/V/S; eligibility never cancels loans |
| BR-05–07 | Equipment domain/management, unique key and protected edit contract; L/V/S |
| BR-08–12 | CheckoutValidation, BorrowEquipmentService/MultiItemCheckoutService, partial unique index; L/M/V/S |
| BR-13–14 | Return transition, actual dates, restrictive FKs and retained history; L/H/P/V/S |
| BR-15–16 | Serializable sessions, prevalidation, database constraints, model/migrations; L/M/F/S |
| BR-17 | SQL partial search/status/order/ties/reset; Q/U/V |
| BR-18 | Safe conflict/failure/Unknown results, no write replay, refresh; L/M/U/V and source walkthrough |
| BR-19 | Async reads and version guards; durable EF state across fresh contexts and separate processes; H/Q/U/P |

## Verification conclusion

All fourteen required cases are implemented. Automated evidence is substantial but not a claim that every manual acceptance branch is complete. Follow the [demonstration script](demonstration-script.md), record remaining observations using the [manual checklist](manual-verification-checklist.md), and retain the tested commit/run identity. No grade or blanket acceptance percentage is assigned.
