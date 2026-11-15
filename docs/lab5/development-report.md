# Lab 5 development report - API integration and acceptance

## Outcome and current acceptance

The Campus Equipment Borrowing System now runs as an independent ASP.NET Core REST API plus an Avalonia HTTP client. All sixteen required operations expose the preserved fourteen Lab 4 workflows and nineteen business rules. PostgreSQL schema/migrations and existing history are preserved. No authentication, Docker/cloud deployment, paid service or extra web/mobile client was introduced.

Source and documentation are delivered through Session 10. T01-T06 and T08 have passing recorded executions; T07 has passing automated HTTP workflow/composition and native startup evidence, but the complete rendered staff registration/edit/issue/return/history sequence remains pending. The review package and absent lab5-submitted tag reflect that limitation honestly.

## Session delivery

| Session | Delivered progress | Actual validation |
|---|---|---|
| 1 | Independent API host, shared DTOs, OpenAPI and sixteen-route target | API-only status/OpenAPI plus safe 404/405; business acceptance initially Not run |
| 2 | Student search/detail/create/edit/eligibility, structured failures | Real HTTP plus fresh SQL; T02 initially partial until issue/return existed |
| 3 | Equipment/catalog/filter/create/edit and protected fields | T03 passed, normalized duplicate and active-history protection |
| 4 | Single issue/detail/return and server-owned dates | T04 passed; remaining T02 restrict/issue/return/permit/issue passed |
| 5 | Atomic checkout, borrowing queries and subject histories | T01/T05/T06 passed, both exact-three boundaries and after-insert rollback |
| 6 | Asynchronous HTTP client and management/catalog migration | Actual ViewModels/independent update refresh; no-response/500/unknown behavior; optional revision guard |
| 7 | HTTP single/multi checkout, Active loans and returns | Confirmation, stale/limit rejection, refreshed flags and preserved history |
| 8 | HTTP histories/search and removal of direct Desktop persistence | All real ViewModels, 23 UI/client tests and actual native startup without DB settings; dependency scan clean |
| 9 | Integrated CI, controlled server/transport failures and recovery | T08 passed with same-client Retry, native feedback and unchanged failed-write state |
| 10 | Architecture/report/handoff, retained raw CI artifact and review packaging | Source/doc/OpenAPI/evidence/link/archive checks; remaining T07/manual/tag actions explicit |

Each session fetched/pulled before work and pushed coherent descriptive commits. Main's authentic merge was incorporated normally when the feature branch was deleted/recreated. Earlier failures/limits remain historical, not overwritten with invented passes. The original functional suite is linked rather than duplicated.

## Contracts and implementation

API-01-09 cover student/equipment/catalog operations; API-10/12/13 cover issue/return/detail; API-11 coordinated checkout; API-14 search/status/sort; API-15/16 related histories. Routes use /api/v1, UTF-8 JSON and stable returned IDs. Endpoint catalog, generated OpenAPI, implementation methods and actual evidence are mapped in [traceability](traceability.md).

Server remains authoritative for validation and dates. Required editable JSON cannot set IDs/availability/status/dates arbitrarily. Complete updates clear optional fields with null. Empty lists differ from missing resources; error statuses/body are consistent. Desktop now references workflow contracts/HTTP adapters rather than EF repositories or SQL.

## Transactions and continuity

Short-lived contexts and a shared serializable session preserve atomic availability/borrowing writes. Generated canonical keys and existing PostgreSQL constraints remain unchanged. Known multi-item failures are verified through fresh before/after SQL. A temporary AFTER INSERT trigger in a newly created test database proves post-write rollback; no public production failure switch exists. Unknown commit is a distinct outcome and does not auto-replay writes.

Lab 4 baseline: 51f47ea5c127a09de16dbd95bd21c63197080823, identified from the user-designated prepared ZIP and verified checksum. Actual Classroom turn-in is not independently inspected. Full continuity/provenance and completed boundary are documented in [baseline](baseline.md) and [architecture](architecture.md).

## Integrated verification

Verified runtime/tools checkpoint: dc1e54186f35600bc8cad15ed8ec6897ccdbb167. [CI run 43710719990](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43710719990) passed a zero-warning full Release build, 76 backend plus 23 UI/client cases (99 total, zero failed/skipped), five separate-process persistence stages and all seven controlled HTTP scenarios. Those scenarios captured 327 exchanges/attempts including expected rejection/failure/cancellation conditions.

Windows complements CI with native catalog and no-response/500/restored screenshots. Linux explicitly omits native Windows capture while retaining actual HTTP ViewModel and dependency checks. Raw TRX/OpenAPI/HTTP evidence has been downloaded and checksum-recorded locally in [retained artifact](evidence/ci-43710719990.zip), with [inventory](evidence/ci-43710719990.json). Runtime behavior is unchanged by this final documentation/packaging session.

The consolidated [T01-T08 execution record](test-execution.md) is authoritative for expected/observed outcomes, diagnosis/correction/retests and limits. A passing build, screenshot or status alone was never treated as proof of persistence or rollback.

## Remaining before final submission

Complete and record the rendered T07 staff sequence using synthetic data, add actual observations/evidence to the consolidated record, then create/push lab5-submitted on that completed revision and regenerate the package. Include both partners' identities privately; each partner submits individually. No course-platform upload, grading claim or final tag has been performed by this report.
