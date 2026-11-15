# Consolidated Lab 5 acceptance execution record

Session 1 operational smoke evidence is separate and does not satisfy any complete supplied acceptance case. Use this one document for actual executions, failures, corrections and retests in later sessions.

| Test | Objective | Status | Reason / evidence |
|---|---|---|---|
| T01 | Independent business reads, OpenAPI, empty/missing/methods | Not run | Business endpoints pending; foundation 404/405/OpenAPI smoke in session1-evidence.md only |
| T02 | Student management and eligibility | Partial | Session 2: 35 HTTP checks with fresh state; restrict/issue/return/permit/issue awaits API-10/12. See session2-evidence.md and session2-http-evidence.json |
| T03 | Equipment management and protection | Pass | Session 3: 35 HTTP requests, fresh before/after state, protected availability/history and API restart; session3-evidence.md |
| T04 | Issue, return and persistence | Not run | Session 4 |
| T05 | Multi-checkout, limit boundaries and after-write rollback | Not run | Session 5 |
| T06 | Histories/search/filter/sort | Not run | Session 5 |
| T07 | HTTP-only responsive Desktop, independent API and refresh | Not run | Sessions 6-8 |
| T08 | Transport versus server failure, recovery and restart | Partial supporting checks | Session 2: safe DB-failure 500/recovery and API restart; no-response/Desktop branches pending Session 9 |

## Execution template

For each actual run record Test ID/date/environment and campus clock; controlled dataset/reset and discovered IDs; expected result; observed result; pass/fail; issue/diagnosis; correction; retest outcome; and evidence references. Retain before/after PostgreSQL reads and actual HTTP captures. Mark unperformed branches explicitly; never turn an unexecuted expectation into an observation.

## Session 2 execution entry

Environment: local Windows/.NET 10/PostgreSQL 18; campus date and unique database recorded in session2-http-evidence.json. Expected: student endpoint statuses and values, same identity/history, no changed state on rejection/no-op, API restart durability and safe 500/recovery. Observed: all 35 requests and state assertions passed. Issue/diagnosis: none during this HTTP run. Correction/retest: no corrective retest needed for this run; foundation verifier updated for newly implemented routes. Evidence: session2-http-evidence.json, session2-evidence.md and openapi-current.json. Full T02 remains pending its borrowing/return branches.

## Session 3 execution entry

T03: expected/observed and reproduction in session3-evidence.md; wire requests/status/bodies and fresh before/after state in session3-http-evidence.json. Passed all 35 equipment calls. No observed defect/correction/retest in this run. Separate 35-call student regression passed after shared error mapping/query changes (session3-student-regression.json). All 76 backend tests pass; OpenAPI required fields/query parameters verified. T01/T02/T07/T08 remain incomplete as described in the ledger.
