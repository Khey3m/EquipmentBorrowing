# Lab 6 test execution and defect record

Sessions 1-8 executed. All 22 automated objectives passed. Both manual checks remain pending. Use Passed, Failed, Blocked or Not Run. Passed requires an actual execution with relevant assertions enabled; skipped tests remain explicitly identified and do not prove an objective. Infrastructure and supporting regression results are recorded separately.

Each execution entry must record source commit, run date/environment, PostgreSQL version and isolated database alias, D/clock arrangement, exact test command or manual method, expected and observed behavior, status, evidence and any defect/correction/retest. Keep credentials out of commands and output. Link focused TRX, HTTP exchanges, fresh PostgreSQL reads and short manual captures rather than redundant screenshots.

The table records current status. Add dated execution entries below as work happens; replace Pending with exact file/class/method references only after implementation. E07a/b/c have separate rows to prevent a transport-only test from hiding missing server/database evidence. Session 2 methods are in `tests/EquipmentBorrowing.ApiTests/ReadApiTests.cs`.

| Objective | Planned session | Expected assertion/observation summary | Actual implementation / manual method | Observed | Status | Defect / correction / retest | Evidence |
|---|---|---|---|---|---|---|---|
| TC6-U01 | 2 | Exact seven catalog IDs/names/availability; empty [] | `ReadApiTests.U01_CatalogReturnsExactFixtureAndEmptyArray` | Exact seven/empty, 200; unchanged SQL | Passed | No defect; no correction/retest needed | [Session 2](evidence/session2-summary.json) |
| TC6-U02 | 2 | Only E2/E3/E6/E7 available; none available [] | `ReadApiTests.U02_AvailableFilterIncludesOnlyAvailableItemsAndHandlesNone` | Exact available four; none available [] with seven items retained, 200; unchanged SQL | Passed | No defect; no correction/retest needed | [Session 2](evidence/session2-summary.json) |
| TC6-U03 | 4 | S4/E2 201, Location/GET, D, active durable loan | `LifecycleApiTests.U03_IssueAndFollowLocation` | 201 and Location GET 200 matched the stored active loan and unavailable item. | Passed | None observed | [Session 4](evidence/session4-trx.zip) |
| TC6-U04 | 2 | Active E1/E4/E5 with correct students; exclude returned E3 | `ReadApiTests.U04_ActiveLoansHaveExactIdsAndRelatedRecordsWithoutReturnedLoan` | Exact three borrowing IDs/relationships/dates; returned E3 excluded; unchanged SQL | Passed | No defect; no correction/retest needed | [Session 2](evidence/session2-summary.json) |
| TC6-U05 | 4 | Return D; same ID/history; E1 available; repeat 409 unchanged | `LifecycleApiTests.U05_ReturnRetainsHistoryAndRepeatLeavesStateUntouched` | Return retained the loan ID and history. Repeat returned 409 with unchanged PostgreSQL state. | Passed | None observed | [Session 4](evidence/session4-trx.zip) |
| TC6-U06 | 4 | S4/E2/E6 201; both IDs/relationships; both unavailable | `LifecycleApiTests.U06_CheckoutPersistsBothLoansAndAvailability` | 201 produced two distinct durable loans and both items became unavailable. | Passed | None observed | [Session 4](evidence/session4-trx.zip) |
| TC6-U07 | 2 | Correct S1 active/returned history; S4 []; missing 404 | `ReadApiTests.U07_StudentHistoryDistinguishesMixedEmptyAndMissingSubjects` | S1 active E1/returned E3; S4 []; missing detail/history 404; unchanged SQL | Passed | No defect; no correction/retest needed | [Session 2](evidence/session2-summary.json) |
| TC6-U08 | 2 | E1/E3 correct borrower/state; E2 []; missing 404 | `ReadApiTests.U08_EquipmentHistoryDistinguishesActiveReturnedEmptyAndMissingSubjects` | E1 active/E3 returned for S1; E2 []; missing detail/history 404; unchanged SQL | Passed | No defect; no correction/retest needed | [Session 2](evidence/session2-summary.json) |
| TC6-U09 | 5 | Partial search/all filters/sorts/combinations/reset/no match/stable ties | `ProtocolApiTests.U09_SearchFiltersSortsAndResets` | 14 GETs matched expected IDs, filters, date orders, ascending ID ties, empty results and reset. | Passed | None observed | [Session 5 TRX](evidence/session5-trx.zip) |
| TC6-U12 | 3 | Unique ST-500 201; ID/defaults/fields/Location/GET/fresh row | `ManagementApiTests.U12_RegisterStudent` | 201 with generated ID and Location. GET and fresh SQL matched. | Passed | None observed | [Session 3](evidence/session3-trx.zip) |
| TC6-U13 | 3 | API-02 match/no match; update fields; same ID/history foreign keys | `ManagementApiTests.U13_EditStudentAndPreserveHistory` | Search matched ST-100 and returned an empty no-match result. Edits persisted with the same ID and unchanged loan rows. | Passed | None observed | [Session 3](evidence/session3-trx.zip) |
| TC6-U14 | 3 | Restrict/409 issue/return/permit/reissue; retained history | `ManagementApiTests.U14_RestrictionBlocksIssueButAllowsReturnAndReissue` | Restriction rejected checkout with 409. Return and re-enabled checkout succeeded with retained history. | Passed | None observed | [Session 3](evidence/session3-trx.zip) |
| TC6-U15 | 3 | EQ-008 201/default/Location/GET; duplicate EQ-001 409; fresh row | `ManagementApiTests.U15_RegisterEquipmentAndRejectDuplicate` | 201 with generated ID and Location. Duplicate returned 409 with unchanged SQL. | Passed | None observed | [Session 3](evidence/session3-trx.zip) |
| TC6-U16 | 3 | Edit E1 description; same ID/unavailable/active history | `ManagementApiTests.U16_EditEquipmentAndPreserveAvailabilityAndHistory` | Edits persisted with the same ID, unavailable state and unchanged loan rows. | Passed | None observed | [Session 3](evidence/session3-trx.zip) |
| TC6-E01 | 6 | Normalized create/update duplicates 409; direct PostgreSQL rejects; fresh counts | `ConstraintApiTests.E01_NormalizedIdentifiersAreUniqueThroughApiAndPostgres` | Four duplicate requests returned 409. Both normalized indexes rejected direct inserts with 23505. Rows and counts matched the baseline. | Passed | None observed | [Session 6 TRX](evidence/session6-trx.zip) |
| TC6-E02 | 4 | Single/multi exactly-three 201; over-limit 409; reset each subcase | `LifecycleApiTests.E02_MultipleAtLimitSucceeds`, `LifecycleApiTests.E02_MultipleOverLimitLeavesBothItemsAvailable`, `LifecycleApiTests.E02_SingleAtLimitSucceeds` | Single and multi-item checkout reached exactly three active loans. Over-limit checkout returned 409 with no writes. | Passed | None observed | [Session 4](evidence/session4-trx.zip) |
| TC6-E03 | 5 | Invalid 400/missing-route 404/method 405/conflict 409; safe/unchanged | `ProtocolApiTests.E03_ProtocolErrorsHaveSafeJsonAndNoWrites` | 10 requests returned the expected 400/404/405/409 JSON errors and preserved PostgreSQL state. | Passed | None observed | [Session 5 TRX](evidence/session5-trx.zip) |
| TC6-E04 | 7 | Fault reached after PostgreSQL write attempt; safe 500; independent rollback state | `RecoveryApiTests.AfterWriteFailureRollsBackAndClientReceivesSafe500` | Observer reached after first item write. HTTP 500 and independently identical PostgreSQL snapshots confirmed rollback. | Passed | Response capture buffered before retest | [Session 7 TRX](evidence/session7-trx.zip) |
| TC6-E05 | 6 | Stale 409; one active E2; other item unchanged; direct DB rejects second active row | `ConstraintApiTests.E05_StaleSelectionsAndSecondActiveLoanAreRejected` | Both stale requests returned 409. PostgreSQL rejected a second active loan with 23505. The original loan and other item remained intact. | Passed | None observed | [Session 6 TRX](evidence/session6-trx.zip) |
| TC6-E06 | 3 | Null clearing 200; required/protected 400; duplicate 409; intact history | `ManagementApiTests.E06_ClearOptionalFieldsAndRejectInvalidEdits` | Null clears persisted. Blank/protected edits returned 400 and duplicates returned 409 with unchanged SQL. | Passed | None observed | [Session 3](evidence/session3-trx.zip) |
| TC6-E07a | 7 | Controlled unreachable transport; no HTTP response/status | `RecoveryApiTests.UnreachableReadHasNoHttpStatus` | Controlled connection failure returned no HTTP status and one attempt. | Passed | None observed | [Session 7 TRX](evidence/session7-trx.zip) |
| TC6-E07b | 7 | Reachable API safe 500; distinct client feedback; fresh known-failure state | `RecoveryApiTests.AfterWriteFailureRollsBackAndClientReceivesSafe500` | Client received safe HTTP 500 feedback and the fresh PostgreSQL state matched the baseline. | Passed | Response capture buffered before retest | [Session 7 TRX](evidence/session7-trx.zip) |
| TC6-E07c | 7 | Unknown acknowledgement outcome; no replay; independent PostgreSQL verification before action | `RecoveryApiTests.LostCheckoutResponseIsUnknownWithoutReplayAndFreshReadFindsBothLoans` | Server returned 201 before the handler dropped the response. Client reported Unknown after one POST. Fresh PostgreSQL contained both loans. | Passed | None observed | [Session 7 TRX](evidence/session7-trx.zip) |
| TC6-E08 | 8 | Automated fresh-connection create/issue/return state; actual API restart separately evidenced | `PersistenceApiTests.E08_RecordsAndReturnSurviveApiProcessRestarts` | Three real API processes used the same database. HTTP and fresh SQL agreed before and after return. | Passed | None observed | [Session 8 TRX](evidence/session8-trx.zip) |
| MA6-01 | 9 | Delayed HTTP/loading; UI interaction; stopped/restored API; no unintended writes | Supervised Windows Desktop run below | Delay, API interruption, recovery and unchanged SQL verified in setup rehearsal. Window observations pending. | Not Run | Setup command corrected and rehearsed | [Setup rehearsal](evidence/session9-setup-rehearsal.json) |
| MA6-02 | 9 | API independent; OpenAPI matches; external edit/refresh; HTTP-only dependencies | Supervised Windows Desktop run below | Standalone API, 16 OpenAPI operations, HTTP-only dependency scan and persisted edit verified. Desktop refresh observation pending. | Not Run | None observed in rehearsal | [Setup rehearsal](evidence/session9-setup-rehearsal.json) |

## Execution entries

For each run include expected versus observed values, enabled assertions, passed/failed/skipped counts, source revision and evidence. Record all E02 subcases independently. For E07 identify each simulation/fault mechanism and its limits. For E08 include both automated fresh-connection results and harness-based or supervised manual restart steps/outcome.

### Session 1 - foundation and baseline regression

Tested source: `cbd1373` (the implementation was built/tested before committing its unchanged contents). Actual run timestamps and full revision are retained in [the execution summary](evidence/session1-summary.json). Environment: Windows, .NET SDK 10.0.404/net10.0, local PostgreSQL 18, real ASP.NET TestServer pipeline in Production environment. D is fixed at 2026-11-18 local Asia/Manila in the test host. Each foundation scenario creates its own database with the prescribed S1-S4/E1-E7 relationships; physical snapshot connections have pooling disabled. Existing regression tests retain their original fixtures/dates and scope.

Commands: root README Lab 6 setup, full Release solution build, then all three test projects with `--configuration Release --no-build` and TRX output. Private admin setting was supplied from the existing private API connection without printing it. No separate API/Desktop process was needed for foundation tests.

| Actual enabled test / check | Expected | Observed | Status |
|---|---|---|---|
| Full Release solution build | Successful build, no warnings/errors | Successful; 0 warnings, 0 errors | Passed |
| `FoundationTests.SeededHostUsesRealRoutesAndExactRelativeFixture` | Real status 200 JSON and unknown-route 404/not_found; migration; exact students/items/eligibility/availability and four dated borrowing relationships | All assertions satisfied; independent physical SQL snapshot retained in TRX stdout | Passed |
| `FoundationTests.FixedClockAndResetRestoreFixtureAfterMutationAndEmptyState` | HTTP issue uses D; mutation changes persisted state; empty reset clears three tables; reseed restores exact baseline | 201 with borrowed date 2026-11-18; fresh-state change and exact empty/reseed comparisons satisfied | Passed |
| `FoundationTests.SeparateScenariosRemainIndependentAndOwnedDatabasesAreRemoved` | Two simultaneous independent hosts/DBs; clearing first leaves second unchanged; first DB absent after disposal; repeat disposal safe | Independent state and admin existence assertions satisfied; owned cleanup logged | Passed |
| Existing backend regression | Enabled pure-rule/PostgreSQL assertions pass with private admin setting | 76 passed, 0 failed, 0 skipped | Passed |
| Existing UI/client regression | Enabled client-double/headless assertions pass | 23 passed, 0 failed, 0 skipped | Passed |

Total: **102 passed, 0 failed, 0 skipped**, including three infrastructure tests. Four owned foundation databases were created/dropped across those tests. Compact raw output: [three TRX files in ZIP](evidence/session1-trx.zip); checksums/counters/method names/fixture database lifecycle are in [summary](evidence/session1-summary.json). Build output was observed directly; the TRX archive contains test output rather than a build log.

Scope limits: TestServer exercises middleware/routing/JSON and real database services but not TCP/process restart; seed setup is direct EF followed by HTTP ID discovery; this is not complete U01/U03/E08 acceptance. Empty/fixture/date checks are foundation evidence only. No manual checks, real-server fault injection, unknown-acknowledgement experiment or Lab 6 CI execution is claimed. At the Session 1 checkpoint all 22 full objectives and MA6-01/02 remained Not Run. No failed test or application correction occurred in that session.

### Session 2 - catalog, available items, active loans and histories

Executed on 2026-11-20 at source `95a882c` (unchanged tested contents committed after the run). Full revision, actual timestamps, all eight test names, HTTP bodies and checksums: [summary](evidence/session2-summary.json). Raw test output with fresh physical PostgreSQL before/after snapshots: [TRX archive](evidence/session2-trx.zip). Environment: Windows/.NET SDK 10.0.404/net10.0, real local PostgreSQL 18, actual ASP.NET middleware/routing/JSON via TestServer in Production. D stays fixed at **2026-11-18**, independently of the execution date.

Pulled upstream with `--ff-only` before work; branch was current. Ran:

```powershell
dotnet build EquipmentBorrowing.slnx --configuration Release --no-restore
$env:POSTGRES_INTEGRATION_ADMIN_CONNECTION = $env:EQUIPMENT_BORROWING_CONNECTION_STRING
dotnet test tests/EquipmentBorrowing.ApiTests/EquipmentBorrowing.ApiTests.csproj --configuration Release --no-build --logger 'trx;LogFileName=api-reads.trx' --results-directory TestResults/lab6/session2
```

The private existing connection used a role with disposable-database permissions; it was not printed or committed. Result: **build passed with 0 warnings/errors; 8 tests passed, 0 failed, 0 skipped** (five new read objectives plus three foundation regressions). The unchanged 76 backend and 23 UI/client cases were not rerun; Session 1 remains their latest recorded execution. No application correction or failed run occurred.

| Objective / method suffix in `ReadApiTests` | Expected | Observed |
|---|---|---|
| U01 `CatalogReturnsExactFixtureAndEmptyArray` | Seven unique IDs/codes, exact names/availability and null descriptions, then empty catalog | 200 with exact seven; independently reset empty subcase returned 200 [] |
| U02 `AvailableFilterIncludesOnlyAvailableItemsAndHandlesNone` | Only E2/E3/E6/E7, excluding E1/E4/E5; none-available [] | 200 with exact available four; reset/prepared none-available subcase returned 200 []; unfiltered catalog retained all seven unavailable items |
| U04 `ActiveLoansHaveExactIdsAndRelatedRecordsWithoutReturnedLoan` | Exactly E1/E4/E5; generated loan IDs, correct students/display data/dates; no returned E3 | 200 with exact three loans, related values and dates, matching fresh persisted rows; E3 excluded |
| U07 `StudentHistoryDistinguishesMixedEmptyAndMissingSubjects` | API-03 detail plus S1 active E1/returned E3, S4 empty, missing subject 404 | Both real subjects/detail/history 200; exact S1 relationships/dates/status; S4 []; missing detail/history 404/not_found |
| U08 `EquipmentHistoryDistinguishesActiveReturnedEmptyAndMissingSubjects` | API-07 detail plus E1 active S1, E3 returned S1, E2 empty, missing subject 404 | Details/history 200 with exact relationships/dates/status or []; missing detail/history 404/not_found |

Every tested GET asserts JSON content type and expected status and compares **every row/field of Students, Equipment and Borrowings** using independent physical PostgreSQL connections before/after. All **20 captured GETs** satisfied these unchanged-state assertions (16 returned 200; four returned 404). Generated loan IDs are discovered from actual HTTP history/list responses and matched to independent no-tracking persisted rows. Each scenario owns a fresh database; resets occur before each separate subcase. The none-available setup adds genuine active loans after reset, respects student limits, and retains the existing returned history rather than merely deleting equipment. Nine owned databases were created/dropped across the eight tests; lifecycle records are retained in the summary.

Coverage: API-01, API-03, API-07, API-14 Active query, API-15 and API-16. U09 search/sort and other later scenarios remain pending even where they share these routes. This session establishes five complete automated objectives, not manual acceptance, full protocol-error coverage, TCP transport, API process restart or transaction-failure recovery. CI branch wiring remains planned for Session 8.

### Session 3 - management, eligibility and protected edits

The Release API test project ran on Windows against local PostgreSQL 18 via the ASP.NET Core TestServer with fixed campus date 2026-11-18. Each objective used its own disposable database. Tested source revision `b85b40c488dea6e9378f22073152843728900069`.

```powershell
dotnet test tests/EquipmentBorrowing.ApiTests/EquipmentBorrowing.ApiTests.csproj -c Release --logger 'trx;LogFileName=api-management.trx' --results-directory TestResults/lab6/session3
```

The private PostgreSQL test admin setting was supplied as documented in the README. All 14 API tests passed, including six new objectives and eight prior tests. There were zero failures or skips. Seven rejected requests were checked against unchanged fresh PostgreSQL snapshots. The test output includes the disposable database names and observed HTTP responses. [Raw TRX archive](evidence/session3-trx.zip).

Archive SHA-256 is `6d56b4d00c3c32df080688d2bab25bf6ff0875f63a3a108ae724bdb50549d21c`.

### Session 4 - borrowing lifecycle and active loan limits

Tested source revision `7a1dc99440ef906cc681561c2c1da48a813dd720` on Windows against local PostgreSQL 18 via ASP.NET Core TestServer. The fixed campus date was 2026-11-18. Each test used a separate disposable database.

```powershell
dotnet test tests/EquipmentBorrowing.ApiTests/EquipmentBorrowing.ApiTests.csproj -c Release --logger 'trx;LogFileName=api-lifecycle.trx' --results-directory TestResults/lab6/session4
```

All 20 API tests passed with zero failures or skips. Six new tests fulfilled four objectives. Fresh PostgreSQL reads confirmed the persisted loan rows, item availability and active counts. Repeat return and over-limit checkout both returned 409 with identical before and after snapshots.

| E02 subcase | HTTP | Active loans | PostgreSQL result |
|---|---|---|---|
| S3 borrows E2 | 201 | 2 to 3 | New loan persisted and E2 unavailable |
| S1 checks out E2 and E6 | 201 | 1 to 3 | Both loans persisted and both items unavailable |
| S3 attempts checkout of E2 and E6 | 409 | 2 to 2 | No writes and both items remained available |

[Raw TRX archive](evidence/session4-trx.zip). Archive SHA-256 is `5f985d1b95f9141e3a548ab8aafce5b5adc80d85b55889988aa2fe37e4f6890f`.

### Session 5 - search, protocol responses and OpenAPI

Tested source `c7fdf61d6da75b843355e688daa6742fc5536a1c` on Windows against local PostgreSQL 18 through ASP.NET Core TestServer. The fixed campus date was 2026-11-18. Each test used a disposable database.

```powershell
dotnet test tests/EquipmentBorrowing.ApiTests/EquipmentBorrowing.ApiTests.csproj -c Release --logger 'trx;LogFileName=api-protocol.trx' --results-directory TestResults/lab6/session5
```

All 23 API tests passed with zero failures or skips. U09 exercised student name, student number and equipment name searches, all status filters, both date sorts in both directions, ties, combined filters, empty results and reset. E03 checked malformed JSON, missing or wrong-type fields, invalid dates and query values, duplicate checkout items, a missing resource, an unknown route, a wrong method and an ineligible borrower. Each rejected request had a safe JSON error and an independently verified unchanged PostgreSQL snapshot.

`ProtocolApiTests.OpenApiMatchesTheReviewedContract` matched the live paths, methods, operation IDs, parameters, request bodies, response statuses and component schemas to the retained Lab 5 OpenAPI document. Sessions 2?5 have real HTTP assertions for all 16 business routes. Later edge and manual objectives remain pending.

The initial test build reported CS1061 because the new test file lacked the JSON HTTP extensions import. Adding the import resolved the build error before the successful full API test run.

[Session 5 TRX](evidence/session5-trx.zip). Archive SHA-256 is `7806396fd6d8a3f68e6797d9b5a9357d7e83067990c016ed234ce996cb3e41bb`.

### Session 6 - PostgreSQL constraints and stale checkout

Tested source `3112f89389856568edce549b3a9e82d24e8283ec` on Windows against local PostgreSQL 18 through ASP.NET Core TestServer. Each test used a disposable database and fixed campus date 2026-11-18.

```powershell
dotnet test tests/EquipmentBorrowing.ApiTests/EquipmentBorrowing.ApiTests.csproj -c Release --logger 'trx;LogFileName=api-constraints.trx' --results-directory TestResults/lab6/session6
```

All 25 API tests passed with zero failures or skips. E01 confirmed four duplicate create/edit requests returned HTTP 409 and both normalized identifier indexes rejected direct inserts with SQLSTATE 23505. E05 confirmed that after another client issued the selected laptop, stale single and multi-item requests returned HTTP 409. A direct second active loan was rejected by the filtered unique index with SQLSTATE 23505. The foreign keys rejected missing student and equipment references with 23503, and the due-date check rejected a same-day due date with 23514. Fresh snapshots matched after each rejection. The original loan remained active and the other item remained available.

[Session 6 TRX](evidence/session6-trx.zip). Archive SHA-256 is `0d7f9b6a6baf77abf87c94d19f367949d3734919be79255c4d14f735c5ebefaf`.

### Session 7 - rollback and uncertain outcomes

Tested source `bbdc29812eaee06fab483847fa2a7eb596ed8bd2` on Windows via ASP.NET Core TestServer with disposable PostgreSQL 18 databases and fixed campus date 2026-11-18. Ran the Release API test project using the private PostgreSQL test setting and the TRX logger, with output in `TestResults/lab6/session7`.

E04 and E07b used a test-only checkout observer. It threw after the first item's equipment update and loan insert completed their database saves and before the transaction committed. After the request returned HTTP 500, an independent physical connection confirmed identical before and after snapshots. The HTTP client received safe server failure feedback.

E07a used a handler that threw a controlled connection error and verified that the client reported no HTTP response. E07c passed the checkout to the actual API and then discarded its successful HTTP 201 at the client boundary. The client reported Unknown and sent exactly one POST. Independent PostgreSQL reads found both active loans and both items unavailable. The handler simulated loss of the response after a successful commit.

The first run passed 27 tests and failed one because the test observer consumed the response stream before the HTTP client read it. The observer now buffers the response before inspecting the error. The full retest passed all 28 tests with zero failures or skips. Both runs are retained in the archive. [Session 7 TRX](evidence/session7-trx.zip).

### Session 8 - persistence across API process restarts

Tested source `c19b67beb491c219ed6e1a017918f92512101add` on Windows against local PostgreSQL 18. E08 started three separate `dotnet EquipmentBorrowing.Api.dll` processes on OS-assigned loopback ports. The test waited for readiness and stopped each owned process before starting the next. All three used the same disposable database without resetting it between phases.

The first process created the student, equipment, and active loan. The second read the same records and returned the item. The third confirmed the same returned loan, restored availability, and unchanged PostgreSQL snapshot. Fresh connections with pooling disabled independently checked saved IDs, values, dates, availability, and relationships. The seed uses fixed D = 2026-11-18. The subprocess uses the production Manila clock. Initial campus date 2026-11-20. Borrowed 2026-11-20. Returned 2026-11-20.

Ran all three test projects in Release with TRX logging to `TestResults/lab6/session8`. The local result was 29 API, 76 backend, and 23 Desktop tests passed, totaling **128 passed with zero failures or skips**. [Session 8 TRX](evidence/session8-trx.zip).

The GitHub Actions workflow includes the Lab 6 branch and enforces a passing restart case and zero skipped API results. It retains TRX and the existing live OpenAPI export. The local results above are from Windows.

### Session 9 - supervised Desktop acceptance

The setup rehearsal passed on Windows and local PostgreSQL 18. It created a disposable database, applied the migrations, and started the actual API. An independent client verified the equipment response and matched all 16 live OpenAPI operations and their schemas against the reviewed Lab 5 document. The Desktop project references the HTTP library and its Release dependency manifest contains no Infrastructure, EF Core, or Npgsql dependency. An independent student edit changed only the expected name field in a fresh SQL snapshot. The controlled delay, stopped API, restored API and unchanged database state were also verified. [Setup rehearsal evidence](evidence/session9-setup-rehearsal.json).

The first setup attempt omitted the PostgreSQL client credentials due to PowerShell treating a property assignment as a connection-string key. The setup below uses the explicit setter. The second attempt passed the EF configuration through a shorthand reserved for context selection. Using `--configuration Release` fixed the migration command, and the subsequent setup rehearsal passed.

Run from the repository root in a Windows terminal. Use the existing private API connection if its role can create and drop disposable databases. Otherwise set `POSTGRES_INTEGRATION_ADMIN_CONNECTION` using the private entry example in the README and omit the first line. If PostgreSQL is installed outside the standard PostgreSQL 18 path, set `PSQL_PATH` to its `psql.exe`.

```powershell
$env:POSTGRES_INTEGRATION_ADMIN_CONNECTION = $env:EQUIPMENT_BORROWING_CONNECTION_STRING
$lab6Connection = New-Object System.Data.Common.DbConnectionStringBuilder
$lab6Connection.set_ConnectionString($env:POSTGRES_INTEGRATION_ADMIN_CONNECTION)
$env:PGHOST = $lab6Connection['Host']
$env:PGPORT = if ($lab6Connection.ContainsKey('Port')) { $lab6Connection['Port'] } else { '5432' }
$env:PGUSER = $lab6Connection['Username']
$env:PGPASSWORD = $lab6Connection['Password']
dotnet tool restore
dotnet build EquipmentBorrowing.slnx -c Release
python tools/verify-lab6-desktop.py
```

Follow the prompts in order. For MA6-01, watch the loading indicator, interact with the window during the eight-second student request, then observe the message and recovery after the API stops and restarts. For MA6-02, observe the same student before and after refreshing the independent edit. Capture a short recording or a few screenshots of those moments. Type what you observed at each prompt, including any discrepancy, and save the evidence before the owned Desktop, API, and database are cleaned up. The script saves the notes alongside the HTTP and SQL findings in `TestResults/lab6/session9/manual-observations.json`. Use `--prepare-only` for a rehearsal that leaves the UI observation fields blank.

The current setup rehearsal contains no native window observations. MA6-01 and MA6-02 stay Not Run until a person performs and records those steps.

### Session 10 - source review package and evidence audit

Created the review package from source revision e69bb7dbea7a983651108ec79de50d31fe57561f using the Lab 6 packaging command in submission-handoff.md. ZIP integrity, required source/evidence files, the embedded revision/history and all 26 execution rows were verified. The package was written to Downloads/Lab6-review. [Package manifest and checksum](evidence/session10-package.json).

The audit verified every automated method reference in this execution table against the Session 8 archive containing 128 passing tests. Local documentation links resolved. The consolidated Lab 5 snapshot tree matches its original recorded snapshot. Stale pending annotations in traceability were corrected to match the completed executions.

The package tool passed Python compilation and an actual package-generation run. The packaged application and automated tests retain the tested Session 8 behavior. MA6-01 and MA6-02 still require supervised native observations. The final lab6-submitted revision remains pending those results.

## Defects, corrections and regression

No application defect has been established in Sessions 1-6. The Session 5 test compilation correction is recorded above. The Lab 5 evidence reconciliation is recorded in baseline.md with its remaining qualifications.

Use one record per discovered defect:

| Field | Entry |
|---|---|
| Defect ID and affected TC6 / UC / BR / API | Pending actual discovery |
| Environment, deterministic fixture / D and reproduction | Pending |
| Expected versus observed | Pending |
| Likely layer and diagnostic evidence | Pending |
| Correction or reason unresolved; revision | Pending |
| Targeted retest and broader regression; revisions/results | Pending |
| Fresh PostgreSQL / HTTP / UI / source evidence | Pending |

Retain failed attempts and unresolved failing tests. Do not disable assertions to obtain a passing run. Record specification ambiguities explicitly; no fabricated defects are required.
