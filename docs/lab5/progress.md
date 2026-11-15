# Lab 5 progress

## Planning checkpoint

Both supplied PDFs read; two-week ten-session plan and Lab 4 package provenance recorded. Working branch: lab5/api-integration. No application code or database configuration changed. API-01?16 implementation and T01?T08 execution remain pending.

Next: Session 1 ? baseline, transport contracts and independent API host foundation. See [session plan](session-plan.md) and [baseline](baseline.md).

## Session 1 - delivered

API/Contracts foundation committed and pushed in 0e6dcd9. Independent Production HTTP checks observed 200 host status, 200 generated OpenAPI, safe 404 and 405 with Desktop closed and no DB setting in the API environment. Full solution Release build passes with zero warnings/errors. See [Session 1 evidence](session1-evidence.md), [target contracts](endpoint-contract.md), and [consolidated acceptance ledger](test-execution.md).

Generated OpenAPI covers only the operational route so far; API-01-16 and T01-T08 remain pending. Structured business failure categorization will be added with student endpoints rather than changing unused result types. No Desktop/database behavior changed. Next: Session 2 student API.

## Session 2 - delivered

Student API-02-06 implemented in 1788c68. Structured student error categories preserve Desktop behavior while mapping 400/404/409/500 and repeated eligibility 200. Full Release build is clean; 76 backend tests pass without skips; 35 real HTTP calls with fresh PostgreSQL before/after state pass, including identity/history/contact/no-op/restart and safe database 500/recovery. See [Session 2 evidence](session2-evidence.md) and [current OpenAPI](openapi-current.json). T02 remains partial until issue/return endpoints exist. Next: Session 3 equipment endpoints API-01/07-09.

## Session 3 - delivered

Equipment API-01/07-09 implemented in c1b69d9. Catalog/available filter, detail, create/Location and complete descriptive edit/null clearing preserve protected state. Structured equipment failure categories share HTTP mapping with students. Clean full solution Release build, 76 passing backend tests, 35 equipment and 35 student regression HTTP requests with fresh state. T03 passed; T01/T02/T07/T08 remain incomplete. See [Session 3 evidence](session3-evidence.md). Next: Session 4 single borrowing, detail and return API-10/12/13.

## Session 4 - delivered

API-10/12/13 implemented in 8ab98f1, preserving serializable service transactions and adding explicit validation/missing/business-conflict outcomes. Full Release build clean; 76 backend tests pass; 37 T04 borrowing/return and 43 complete T02 student HTTP requests pass with fresh PostgreSQL state and API restart evidence. T02 and T04 are now Pass for these executions. See [Session 4 evidence](session4-evidence.md). Next: Session 5 multi-item checkout and complete query/history API-11/14/15/16.

## Session 5 - delivered

All sixteen endpoints implemented in 387276a. API-11/14/15/16 preserve atomic multi-checkout, combined SQL-backed queries and relational histories. Full Release build clean; 76 backend and four UI tests pass; 57 T01/T06 and 57 reset T05 HTTP requests pass with independent state and controlled after-insert rollback. T01-T06 now have passing executions; T07/full T08 pending. See [Session 5 evidence](session5-evidence.md), [traceability](traceability.md) and [OpenAPI](openapi-current.json). Next: Session 6 asynchronous HTTP client and first Desktop screens.

## Session 6 - delivered

HTTP client/workflow interfaces and first management/catalog migration in cfb379a, real probe/tests in c347792, concurrency guard in 96fed32. Build clean; 76 backend and eleven UI/client tests pass. Actual HTTP-backed management ViewModels refresh independent edits; no PostgreSQL settings in the probe process. T07 is partial: unmigrated Desktop workflows still directly use the same database until Sessions 7-8. See [Session 6 evidence](session6-evidence.md). Next: Session 7 single/multi checkout and active returns over HTTP.
