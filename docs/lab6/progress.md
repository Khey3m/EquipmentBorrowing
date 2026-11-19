# Lab 6 progress

Planning checkpoint prepared on 2026-11-18. Sessions 1-2 completed on the Lab 6 branch: test infrastructure/baseline audit followed by five read objectives, without production application changes. TC6-U01/U02/U04/U07/U08 passed; Session 3 completed six more objectives. Eleven automated objectives and manual acceptance remain pending.

| Week | Session | Deliverable | Status |
|---|---|---|---|
| 1 | 1 | Baseline reconciliation, assertion audit, real HTTP xUnit host, fixed clock and isolated fixture | Complete |
| 1 | 2 | Catalog, filtering, active lists and histories | Complete |
| 1 | 3 | Management, eligibility, optional/protected fields | Complete |
| 1 | 4 | Borrowing, returns, multi-checkout and exact limits | Not started |
| 1 | 5 | Search, protocol errors, all sixteen API contracts/OpenAPI | Not started |
| 2 | 6 | PostgreSQL constraints, uniqueness and stale conflicts | Not started |
| 2 | 7 | After-write rollback and E07a/b/c failure handling | Not started |
| 2 | 8 | Fresh-connection persistence, actual API restart and CI | Not started |
| 2 | 9 | MA6-01/02 and corrections/regression | Not started |
| 2 | 10 | Traceability, evidence, reproducibility and final tag | Not started |

## Session 1 - completed

Pulled upstream with `--ff-only`; already up to date. Added `EquipmentBorrowing.ApiTests` to the solution with a real ASP.NET pipeline, fixed D=2026-11-18 and isolated owned PostgreSQL databases. Tests verify exact relative fixture state, routing/JSON smoke behavior, date injection on a real mutation, empty/reset/reseed, independent simultaneous scenarios and owned-database cleanup. Production source, migrations, clock and API configuration remain unchanged.

Release build: zero warnings/errors. Three new foundation tests, 76 backend tests and 23 UI/client tests passed; zero failures/skips. Implementation commit: `cbd1373`, pushed to origin. [Actual execution and evidence](test-execution.md#session-1---foundation-and-baseline-regression). Baseline audit reconciles all 60 supplied screenshot figures and explicitly retains the report's remaining qualifications and unavailable submitted-tag provenance.

## Session 2 - completed

On 2026-11-20, fetched origin and pulled the Lab 6 branch with `--ff-only`; already up to date. Added `ReadApiTests` for TC6-U01/U02/U04/U07/U08: exact catalog/availability, no-available/empty cases, active loans, related fields/dates/IDs, student/equipment detail/history and missing-versus-empty outcomes. Each tested GET asserts unchanged fresh physical PostgreSQL state; each scenario/subcase is independently seeded/reset.

Full Release build: 0 warnings/errors. All eight API tests passed with no failures/skips (five objectives plus three foundation regressions). Recorded 20 GETs and 20 unchanged before/after pairs; nine owned databases created/dropped. Source commit: `95a882c`. [Execution and scope](test-execution.md#session-2---catalog-available-items-active-loans-and-histories), [summary](evidence/session2-summary.json) and [raw TRX](evidence/session2-trx.zip). No production fix was needed; the 99 older backend/UI tests were not rerun because only the new test file changed.

## Session 3 - completed

U12/U13/U14/U15/U16/E06 passed in the real HTTP and PostgreSQL suite. The suite passed 14 tests with zero failures or skips. Source commit `b85b40c`. [Execution record](test-execution.md#session-3---management-eligibility-and-protected-edits).

Next checkpoint is Session 4, covering borrowing, returns, multi-item checkout and the active loan limit.
