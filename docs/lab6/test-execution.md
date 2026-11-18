# Lab 6 test execution and defect record

Session 1 foundation executed; full TC6 objectives/manual checks remain pending. Use Passed, Failed, Blocked or Not Run. Passed requires an actual execution with relevant assertions enabled; skipped tests remain explicitly identified and do not prove an objective. The table below tracks full objectives; infrastructure and supporting regression results are recorded separately.

Each execution entry must record source commit, run date/environment, PostgreSQL version and isolated database alias, D/clock arrangement, exact test command or manual method, expected and observed behavior, status, evidence and any defect/correction/retest. Keep credentials out of commands and output. Link focused TRX, HTTP exchanges, fresh PostgreSQL reads and short manual captures rather than redundant screenshots.

The table records initial status. Add dated execution entries below as work happens; replace Pending with exact file/class/method references only after implementation. E07a/b/c have separate rows to prevent a transport-only test from hiding missing server/database evidence.

| Objective | Planned session | Expected assertion/observation summary | Actual implementation / manual method | Observed | Status | Defect / correction / retest | Evidence |
|---|---|---|---|---|---|---|---|
| TC6-U01 | 2 | Exact seven catalog IDs/names/availability; empty [] | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U02 | 2 | Only E2/E3/E6/E7 available; none available [] | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U03 | 4 | S4/E2 201, Location/GET, D, active durable loan | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U04 | 2 | Active E1/E4/E5 with correct students; exclude returned E3 | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U05 | 4 | Return D; same ID/history; E1 available; repeat 409 unchanged | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U06 | 4 | S4/E2/E6 201; both IDs/relationships; both unavailable | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U07 | 2 | Correct S1 active/returned history; S4 []; missing 404 | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U08 | 2 | E1/E3 correct borrower/state; E2 []; missing 404 | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U09 | 5 | Partial search/all filters/sorts/combinations/reset/no match/stable ties | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U12 | 3 | Unique ST-500 201; ID/defaults/fields/Location/GET/fresh row | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U13 | 3 | API-02 match/no match; update fields; same ID/history foreign keys | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U14 | 3 | Restrict/409 issue/return/permit/reissue; retained history | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U15 | 3 | EQ-008 201/default/Location/GET; duplicate EQ-001 409; fresh row | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-U16 | 3 | Edit E1 description; same ID/unavailable/active history | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-E01 | 6 | Normalized create/update duplicates 409; direct PostgreSQL rejects; fresh counts | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-E02 | 4 | Single/multi exactly-three 201; over-limit 409; reset each subcase | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-E03 | 5 | Invalid 400/missing-route 404/method 405/conflict 409; safe/unchanged | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-E04 | 7 | Fault reached after PostgreSQL write attempt; safe 500; independent rollback state | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-E05 | 6 | Stale 409; one active E2; other item unchanged; direct DB rejects second active row | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-E06 | 3 | Null clearing 200; required/protected 400; duplicate 409; intact history | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-E07a | 7 | Controlled unreachable transport; no HTTP response/status | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-E07b | 7 | Reachable API safe 500; distinct client feedback; fresh known-failure state | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-E07c | 7 | Unknown acknowledgement outcome; no replay; independent PostgreSQL verification before action | Pending | Not executed | Not Run | None recorded | Pending |
| TC6-E08 | 8 | Automated fresh-connection create/issue/return state; actual API restart separately evidenced | Pending | Not executed | Not Run | None recorded | Pending |
| MA6-01 | 9 | Delayed HTTP/loading; UI interaction; stopped/restored API; no unintended writes | Pending | Not observed | Not Run | None recorded | Pending |
| MA6-02 | 9 | API independent; OpenAPI matches; external edit/refresh; HTTP-only dependencies | Pending | Not observed | Not Run | None recorded | Pending |

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

Scope limits: TestServer exercises middleware/routing/JSON and real database services but not TCP/process restart; seed setup is direct EF followed by HTTP ID discovery; this is not complete U01/U03/E08 acceptance. Empty/fixture/date checks are foundation evidence only. No manual checks, real-server fault injection, unknown-acknowledgement experiment or Lab 6 CI execution is claimed. All 22 full objectives and MA6-01/02 retain Not Run until their sessions implement and execute complete required assertions. No failed test or application correction occurred in this session.

## Defects, corrections and regression

No application defect has been established in Session 1; no failed run or runtime correction was concealed. The Lab 5 evidence reconciliation is recorded in baseline.md with its remaining qualifications.

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
