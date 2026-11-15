# Consolidated Lab 5 acceptance execution record

Session 1 operational smoke evidence is separate and does not satisfy any complete supplied acceptance case. Use this one document for actual executions, failures, corrections and retests in later sessions.

| Test | Objective | Status | Reason / evidence |
|---|---|---|---|
| T01 | Independent business reads, OpenAPI, empty/missing/methods | Pass | Session 5: independent reads, full OpenAPI, safe 404/405, fresh snapshots; session5-query-evidence.json |
| T02 | Student management and eligibility | Pass | Session 4: 43 HTTP checks, including the formerly pending restrict/issue/return/permit/issue sequence; session4-student-evidence.json |
| T03 | Equipment management and protection | Pass | Session 3: 35 HTTP requests, fresh before/after state, protected availability/history and API restart; session3-evidence.md |
| T04 | Issue, return and persistence | Pass | Session 4: 37 HTTP requests, fresh saved-state checks and API restart; session4-evidence.md |
| T05 | Multi-checkout, limit boundaries and after-write rollback | Pass | Session 5: reset subcases, exact-three boundaries, safe after-insert 500 and no partial state; session5-checkout-evidence.json |
| T06 | Histories/search/filter/sort | Pass | Session 5: fresh dataset and independently derived membership/display/order; session5-query-evidence.json |
| T07 | HTTP-only responsive Desktop, independent API and refresh | Partial | Session 8: complete HTTP-only composition, all real ViewModels, independent refresh, native catalog and clean runtime dependencies; full rendered staff walkthrough pending |
| T08 | Transport versus server failure, recovery and restart | Partial supporting checks | Session 2: safe DB-failure 500/recovery and API restart; no-response/Desktop branches pending Session 9 |

## Execution template

For each actual run record Test ID/date/environment and campus clock; controlled dataset/reset and discovered IDs; expected result; observed result; pass/fail; issue/diagnosis; correction; retest outcome; and evidence references. Retain before/after PostgreSQL reads and actual HTTP captures. Mark unperformed branches explicitly; never turn an unexecuted expectation into an observation.

## Session 2 execution entry

Environment: local Windows/.NET 10/PostgreSQL 18; campus date and unique database recorded in session2-http-evidence.json. Expected: student endpoint statuses and values, same identity/history, no changed state on rejection/no-op, API restart durability and safe 500/recovery. Observed: all 35 requests and state assertions passed. Issue/diagnosis: none during this HTTP run. Correction/retest: no corrective retest needed for this run; foundation verifier updated for newly implemented routes. Evidence: session2-http-evidence.json, session2-evidence.md and openapi-current.json. Full T02 remains pending its borrowing/return branches.

## Session 3 execution entry

T03: expected/observed and reproduction in session3-evidence.md; wire requests/status/bodies and fresh before/after state in session3-http-evidence.json. Passed all 35 equipment calls. No observed defect/correction/retest in this run. Separate 35-call student regression passed after shared error mapping/query changes (session3-student-regression.json). All 76 backend tests pass; OpenAPI required fields/query parameters verified. T01/T02/T07/T08 remain incomplete as described in the ledger.

## Session 4 execution entry

T02 and T04: expected/observed and exact environment/dataset in session4-evidence.md. T02 passes all 43 calls; T04 passes all 37 calls against final code after status enum/schema refinement. Evidence: session4-student-evidence.json, session4-http-evidence.json, openapi-current.json. No observed failing branch; final contract refinement prompted OpenAPI/T04 retest, which passed. Full Release build clean; 76 existing backend tests pass with PostgreSQL. T05/T06/T07 and full T01/T08 remain pending.

## Session 5 execution entry

T01/T06: 57 read/query requests with reset between the supplied cases, correct relational responses, all filters/sorts/ties/clear/literal matching, empty/missing and invalid routes/methods. T05: 57 requests with reset before each subcase, both exact-at-three paths, over-limit and individual rejection branches, and AFTER INSERT fault with safe 500 and identical fresh state. Expected/observed, environment, fault definition and actual artifacts in session5-evidence.md. All passed; no failing assertion/correction required in those runs. OpenAPI enrichment was followed by clean build and schema recheck. Regression: 76 backend plus four UI tests pass. T07/full T08 still pending.

## Session 6 execution entry

T07 supporting execution: actual management ViewModels use HTTP, independently changed records appear after refresh, selected stale revisions reject 409, and fresh grouped database snapshots confirm final state/history. Scope excludes full Desktop composition and remaining direct workflows. T08 supporting tests distinguish no response/500 and unknown mutation without replay; full real Desktop transport/recovery remains pending. Diagnosis: precheck-to-write gap identified in review; correction: optional revision header plus server/transaction checks; retest: fresh real probe with stale student/eligibility/equipment rejection passed. Final build clean; 76 backend and eleven UI/client tests pass. See session6-evidence.md and session6-client-evidence.json.

## Session 7 execution entry

T07 supporting execution: single issue, explicit confirmed return, multi checkout, Active/catalog refresh, stale-item and over-limit rejection pass through real HTTP-backed ViewModels. Actual capture: session7-client-evidence.json; fresh grouped SQL confirms old history unchanged and correct new returned/active loans. Unknown/malformed response and no-replay behavior covered by focused client checks, not a live lost-ack test. One nullable compiler warning was corrected; clean build, 76 backend and seventeen UI/client cases pass. Full T07/T08 pending remaining retrieval/dependency migration and manual UI evidence.

## Session 8 execution entry

T07 supporting execution: all seven screen ViewModels resolve through the actual App composition, with histories/search over HTTP; real lifecycle/retrieval/refresh checks pass without database settings in the probe. Actual native Desktop starts without DB settings, shows synthetic API catalog and responds; screenshot and forbidden-dependency scan captured. Diagnosis/correction: two nullable-flow warnings fixed; shell helper extraction resolves capture-command quoting before final execution. Retest: final clean build, 76 backend and 23 UI/client tests pass; 69 captured completed HTTP exchanges. Full rendered staff workflow and consolidated T08 stay pending. See session8-evidence.md and session8-client-evidence.json/png.
