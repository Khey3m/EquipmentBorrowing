# Lab 6 progress

Planning checkpoint prepared on 2026-11-18. Sessions 1-2 completed on the Lab 6 branch: test infrastructure/baseline audit followed by five read objectives, without production application changes. TC6-U01/U02/U04/U07/U08 passed; Session 3 completed six more objectives. Session 4 brought the total to 15 passed objectives. Session 5 brought the total to 17. Session 6 brought the total to 19. Session 7 brought the total to 21. Session 8 completed all 22 automated objectives. Both manual checks subsequently passed in the supervised walkthrough.

| Week | Session | Deliverable | Status |
|---|---|---|---|
| 1 | 1 | Baseline reconciliation, assertion audit, real HTTP xUnit host, fixed clock and isolated fixture | Complete |
| 1 | 2 | Catalog, filtering, active lists and histories | Complete |
| 1 | 3 | Management, eligibility, optional/protected fields | Complete |
| 1 | 4 | Borrowing, returns, multi-checkout and exact limits | Complete |
| 1 | 5 | Search, protocol errors, all sixteen API contracts/OpenAPI | Complete |
| 2 | 6 | PostgreSQL constraints, uniqueness and stale conflicts | Complete |
| 2 | 7 | After-write rollback and E07a/b/c failure handling | Complete |
| 2 | 8 | Fresh-connection persistence, actual API restart and CI | Complete |
| 2 | 9 | MA6-01/02 and corrections/regression | Complete |
| 2 | 10 | Traceability, evidence, reproducibility and final tag | Complete |

## Session 1 - completed

Pulled upstream with `--ff-only`; already up to date. Added `EquipmentBorrowing.ApiTests` to the solution with a real ASP.NET pipeline, fixed D=2026-11-18 and isolated owned PostgreSQL databases. Tests verify exact relative fixture state, routing/JSON smoke behavior, date injection on a real mutation, empty/reset/reseed, independent simultaneous scenarios and owned-database cleanup. Production source, migrations, clock and API configuration remain unchanged.

Release build: zero warnings/errors. Three new foundation tests, 76 backend tests and 23 UI/client tests passed; zero failures/skips. Implementation commit: `cbd1373`, pushed to origin. [Actual execution and evidence](test-execution.md#session-1---foundation-and-baseline-regression). Baseline audit reconciles all 60 supplied screenshot figures and explicitly retains the report's remaining qualifications and unavailable submitted-tag provenance.

## Session 2 - completed

On 2026-11-20, fetched origin and pulled the Lab 6 branch with `--ff-only`; already up to date. Added `ReadApiTests` for TC6-U01/U02/U04/U07/U08: exact catalog/availability, no-available/empty cases, active loans, related fields/dates/IDs, student/equipment detail/history and missing-versus-empty outcomes. Each tested GET asserts unchanged fresh physical PostgreSQL state; each scenario/subcase is independently seeded/reset.

Full Release build: 0 warnings/errors. All eight API tests passed with no failures/skips (five objectives plus three foundation regressions). Recorded 20 GETs and 20 unchanged before/after pairs; nine owned databases created/dropped. Source commit: `95a882c`. [Execution and scope](test-execution.md#session-2---catalog-available-items-active-loans-and-histories), [summary](evidence/session2-summary.json) and [raw TRX](evidence/session2-trx.zip). No production fix was needed; the 99 older backend/UI tests were not rerun because only the new test file changed.

## Session 3 - completed

U12/U13/U14/U15/U16/E06 passed in the real HTTP and PostgreSQL suite. The suite passed 14 tests with zero failures or skips. Source commit `b85b40c`. [Execution record](test-execution.md#session-3---management-eligibility-and-protected-edits).

## Session 4 - completed

U03/U05/U06/E02 passed. All 20 API tests passed with zero failures or skips. The limit tests confirmed that exactly three active loans are allowed and over-limit checkout leaves both items available. Source commit `7a1dc99`. [Execution record](test-execution.md#session-4---borrowing-lifecycle-and-active-loan-limits).

## Session 5 - completed

U09 and E03 passed. All 23 API tests passed with zero failures or skips. The live OpenAPI matched the reviewed contract for all sixteen business operations. Source commit `c7fdf61`. [Execution record](test-execution.md#session-5---search-protocol-responses-and-openapi).

## Session 6 - completed

E01 and E05 passed through the real API and direct PostgreSQL checks. All 25 API tests passed with zero failures or skips. Source commit `3112f89`. [Execution record](test-execution.md#session-6---postgresql-constraints-and-stale-checkout).

## Session 7 - completed

E04 and all E07 subcases passed. The full retest passed 28 API tests with zero failures or skips. Source commit `bbdc298`. [Execution record](test-execution.md#session-7---rollback-and-uncertain-outcomes).

## Session 8 - completed

E08 passed across three API processes and fresh PostgreSQL reads. The local Release regression passed all 128 tests. The Lab 6 workflow now runs on branch pushes and rejects skipped API tests. Source commit `c19b67b`. [Execution record](test-execution.md#session-8---persistence-across-api-process-restarts).

## Session 9 - completed

The supervised native Windows walkthrough passed MA6-01 and MA6-02. The user confirmed loading and responsiveness, provided the outage screenshot, confirmed recovery, and confirmed the external name change after refresh. The script verified the API contract, unchanged recovery state and the persisted edit through fresh SQL. [Run instructions and evidence](test-execution.md#session-9---supervised-desktop-acceptance).

## Session 10 - final handoff

Added the Lab 6 source-package option and submission inventory. Reconciled stale pending notes in traceability and documented history consolidation without changing original execution captures. The review package records all automated objectives and the two pending manual checks. [Handoff instructions](submission-handoff.md).

The user completed the supervised observations. The final handoff uses the annotated lab6-submitted revision and its generated package.

Generated and verified the review archive at e69bb7d. [Package record](evidence/session10-package.json). The execution record includes the Session 10 audit.

The supervised run saved the actual user observations, API checks and independent SQL findings. [Manual evidence](evidence/session9-manual-observations.json).
