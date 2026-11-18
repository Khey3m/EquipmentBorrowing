# Lab 6 progress

Planning checkpoint prepared on 2026-11-18. Session 1 subsequently completed on the Lab 6 branch: real HTTP/PostgreSQL test infrastructure and baseline audit, without production application changes. Full TC6 objectives and manual acceptance remain pending.

| Week | Session | Deliverable | Status |
|---|---|---|---|
| 1 | 1 | Baseline reconciliation, assertion audit, real HTTP xUnit host, fixed clock and isolated fixture | Complete |
| 1 | 2 | Catalog, filtering, active lists and histories | Not started |
| 1 | 3 | Management, eligibility, optional/protected fields | Not started |
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

Next checkpoint: Session 2, exact catalog/available/active/history API objectives. Infrastructure smoke checks do not complete entire TC6 scenarios. Explain each session before implementation and report actual checks, corrections, remaining limits and Git commit/push afterward. Maintain this ledger instead of redundant session reports.
