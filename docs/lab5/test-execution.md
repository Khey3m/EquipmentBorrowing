# Consolidated Lab 5 acceptance execution record

Session 1 operational smoke evidence is separate and does not satisfy any complete supplied acceptance case. Use this one document for actual executions, failures, corrections and retests in later sessions.

| Test | Objective | Status | Reason / evidence |
|---|---|---|---|
| T01 | Independent business reads, OpenAPI, empty/missing/methods | Not run | Business endpoints pending; foundation 404/405/OpenAPI smoke in session1-evidence.md only |
| T02 | Student management and eligibility | Not run | Session 2 |
| T03 | Equipment management and protection | Not run | Session 3 |
| T04 | Issue, return and persistence | Not run | Session 4 |
| T05 | Multi-checkout, limit boundaries and after-write rollback | Not run | Session 5 |
| T06 | Histories/search/filter/sort | Not run | Session 5 |
| T07 | HTTP-only responsive Desktop, independent API and refresh | Not run | Sessions 6-8 |
| T08 | Transport versus server failure, recovery and restart | Not run | Session 9 |

## Execution template

For each actual run record Test ID/date/environment and campus clock; controlled dataset/reset and discovered IDs; expected result; observed result; pass/fail; issue/diagnosis; correction; retest outcome; and evidence references. Retain before/after PostgreSQL reads and actual HTTP captures. Mark unperformed branches explicitly; never turn an unexecuted expectation into an observation.
