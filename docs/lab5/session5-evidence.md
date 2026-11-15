# Session 5 - complete API, atomic checkout and queries

Date: 2026-11-15. Implementation commit: 387276a. All sixteen required operations API-01-16 now exist. This completes the planned Week 1 API checkpoint, not the full Lab 5 submission. Desktop remains the Lab 4 direct-database client; T07/full T08 wait for Sessions 6-9.

## Actual execution

| Check | Observed result |
|---|---|
| Full solution Release build | Zero warnings/errors |
| Existing backend tests, PostgreSQL enabled | 76 passed, zero failed/skipped |
| Existing headless UI tests | Four passed, zero failed/skipped; historical direct-DB Desktop, not T07 |
| T01/T06 HTTP reads and queries | 57 requests passed with fresh independent PostgreSQL snapshots |
| T05 reset checkout cases | 57 requests passed with fresh independent PostgreSQL snapshots |
| Machine-readable OpenAPI | All sixteen operation IDs, required request fields, response statuses, string status enum, query values/defaults and creation Location metadata verified |
| Known after-write failure | Received safe 500; fresh snapshots match before state exactly |

Read/query database: `equipment_borrowing_verify_lab5_1115_175025`. Checkout database: `equipment_borrowing_verify_lab5_1115_175045`. Both created anew. Campus date: 2026-11-15; reference historical/future offsets shifted consistently. No Desktop was running during independent API checks. No development or Lab 4 demo database was overwritten.

## T01 - independent API reads

Expected: catalog/available filter, students, entity/loan details, active and relational histories; empty arrays for known empty subjects, 404 for missing IDs/routes, 405 for wrong methods and a complete retrievable OpenAPI contract.

Observed: required reads/statuses pass; available excludes E1/E4/E5, empty S4/E2 histories are arrays, missing specific student/equipment/loan and history subjects are 404, unknown route 404, DELETE equipment 405. The separate foundation verifier retrieves OpenAPI without any DB setting and validates the full sixteen-operation contract. Read requests do not alter saved rows. Result: Pass for this execution.

## T06 - histories and query semantics

Reset to S1-S4/E1-E7 before T06. Expected membership/display information is independently assembled from fresh PostgreSQL rows, not copied from API output. Observed S1/S4 and E1/E3 histories include the correct related IDs/names/numbers/codes, dates and Active/Returned statuses; missing subjects are 404. Partial mixed-case name/number/equipment-name searches, literal %, underscore and backslash searches, each status, both dates in both directions, combined filters, empty results and reset all pass. Ascending ID breaks tied dates even with descending dates. Invalid/repeated/unknown query values return safe 400 with no state change. Result: Pass for this execution.

## T05 - atomicity and limits

Reset the supplied dataset before every success, boundary, invalid and fault subcase; IDs are resolved via API each time. Observed S4 E2/E6 checkout returns 201 with two correct loans and unavailable flags; each new loan GET succeeds. S3 with two existing loans plus one single issue succeeds at three; S1 with one plus two-item checkout succeeds at three; S3 with two plus two rejects 409 unchanged. Individual duplicate, one/zero-item, unavailable, restricted, missing student/item, invalid date/ID, null/missing fields and protected state requests return the expected 400/404/409 with unchanged before/after rows.

Fault strategy: test setup installs an AFTER INSERT trigger on Borrowings in this newly created disposable database only. It raises a controlled exception when the first loan INSERT is attempted, after its equipment UPDATE inside the same serializable session. A valid two-item HTTP request receives safe 500; independent state is identical to before the request: no partial loans or availability changes. The trigger/function are removed in finally; a fresh list still has only the original four records. Recorded trigger definition:

```sql
CREATE TRIGGER lab5_after_insert_fault AFTER INSERT ON public."Borrowings" FOR EACH ROW EXECUTE FUNCTION lab5_fail_after_insert()
```

No public API failure switch or production observer was added. Source: reset/fault branch in tools/verify-student-api.py; production coordination remains MultiItemCheckoutService. This is a known PostgreSQL failure and rollback check, not a lost-commit-acknowledgment experiment. Result: T05 Pass for this execution.

## Diagnostics and limitations

No failing request/assertion in the above runs. OpenAPI metadata was expanded with exact query enums/defaults and Location headers, then the full build/foundation schema checks passed. Existing 76 backend and four UI tests pass. Error-path checks also scoped a DB access failure to each disposable DB, restored access and verified 500-to-200 recovery/restart; full T08 still needs stopped-API/no-response and HTTP Desktop evidence.

T01-T06 now have passing executions across Sessions 3-5. T07 remains Not run; T08 has partial supporting checks. Code/source records do not claim that Desktop has been migrated or all UI/manual acceptance is complete.

## Reproduce

Privately set PG variables and LAB5_STUDENT_VERIFY_CONNECTION to the corresponding NEW validated database as in [Session 2](session2-evidence.md). Build Release, then run:

```powershell
python tools/verify-student-api.py --resource reads --database equipment_borrowing_verify_lab5_unique_reads --output docs/lab5/session5-query-evidence.json
python tools/verify-student-api.py --resource checkout --database equipment_borrowing_verify_lab5_unique_checkout --output docs/lab5/session5-checkout-evidence.json
python tools/verify-api-foundation.py --openapi-output docs/lab5/openapi-current.json
```

Reset is confined to the newly created test DB; existing DB creation is refused. Evidence: [query capture](session5-query-evidence.json), [checkout capture](session5-checkout-evidence.json), [generated OpenAPI](openapi-current.json). Schema checks use the API-only process; no real credential is captured.
