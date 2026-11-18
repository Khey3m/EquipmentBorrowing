# Lab 6 implementation baseline

| Field | Observed record |
|---|---|
| Verifiable Lab 5 snapshot | `d1be8c8bdabe0abb8c7d0220462f63efea5fd4db` |
| Source branch | `lab5/api-integration`, origin `https://github.com/Khey3m/EquipmentBorrowing.git` |
| Verification on 2026-11-18 | Clean checkout; fetched origin; fast-forward-only pull reported already up to date. Local tag list and remote `git ls-remote --tags origin` returned no tags. |
| Submission provenance | No `lab5-submitted` tag and no independently verified Classroom submission. Use the authentic snapshot above; do not create retrospective submission history. |
| Documentation reconciliation | Session 1 inspected all 60 caption entries, verified every referenced PNG exists and confirmed 60 embedded images in the Word screenshot report. Its final review explicitly qualifies T07; see the findings below. The older pending statements lack newer supporting references but do not establish an all-pass submission. |
| Existing test projects | `tests/EquipmentBorrowing.Tests` and `tests/EquipmentBorrowing.UiTests`; assertion audit below. Historical CI results remain scoped to their original revision; Session 1 results are recorded separately in test-execution.md. |
| Test environment | .NET SDK 10.0.404, .NET 10 API host via WebApplicationFactory/TestServer, real local PostgreSQL 18, generated per-scenario databases, fixed test-host D = 2026-11-18 local Asia/Manila. No production clock or process-wide database setting is changed by the fixture. |
| Final Lab 6 revision/tag | Pending; no `lab6-submitted` created during planning. |
| Significant refactoring / execution instructions | Added `tests/EquipmentBorrowing.ApiTests` to the solution. Production application unchanged. Private admin setup, isolation and commands are in the root README Lab 6 section. |

Source documents read locally from Downloads (not copied into the repository):

| Document | Pages | SHA-256 |
|---|---|---|
| ITSD81_Lab6_Handout.pdf | 3 | `06A3980BBC13040AA4602416031980FE502FE418F424D4C5646CA58A84767CEB` |
| ITSD81_Lab6_Test_Cases_and_Templates.pdf | 26 | `16BD90F200C35EB9B12C1F1F9CCA4781D40285102E2E08979FC1C8DD6F83C2B3` |

The published Lab 4 and Lab 5 specifications remain authoritative. Record genuine ambiguities and decisions instead of silently weakening expectations.

## Session 1 Lab 5 evidence reconciliation

Reviewed [caption inventory and final review](../lab5/manual-evidence/captions.json) and the embedded text/media inventory of `docs/lab5/DEMO_LAB_5_SCREENSHOT_REPORT.docx` at the authentic snapshot. The 60 figures support rendered management, issue/return, multi-checkout, history/search, independent update, failure/recovery, restart readback and architecture inspection.

The report itself records these remaining limits: initial API catalog is empty rather than the required seeded seven items; Desktop-closed API operation is not visibly established; checked return confirmation is unshown; some SQL cells are truncated; still images cannot establish full process continuity/termination; no failed-mutation or lost-acknowledgement experiment is demonstrated by the manual recovery pictures. Figure 60 shows the ledger still marking T07 Partial, and the final report review warns against treating the collection as unconditional acceptance. Earlier automated CI/HTTP/SQL captures remain separately scoped supporting evidence.

Decision: retain `d1be8c8` as the verifiable Lab 5 snapshot, retain qualified T07 status and missing submission provenance, and link the newer evidence here. No retrospective `lab5-submitted` tag, Classroom claim, or Lab 6 manual pass is inferred. Lab 6 MA6-01/02 and E07/E08 will receive their own executions.

## Existing assertion audit and remaining work

All test paths below are relative to `tests/`. This is source inspection of enabled assertions; execution counts are recorded separately. Reuse the assertion logic where applicable while adding the controlled Lab 6 fixture and real HTTP checks.

| Existing implementation inspected | What its assertions establish | Lab 6 gap / planned checkpoint |
|---|---|---|
| `EquipmentBorrowing.Tests/Lab4DomainTests.cs`: `CampusDateUsesManilaCalendarRatherThanUtcCalendar`, `ActiveLimitCountsEveryRequestedItem`, edit and return tests | Pure normalization, dates, limits, identity and transitions | No HTTP or PostgreSQL proof; reuse as supporting rule regression, Sessions 3-4 |
| `StudentManagementServiceTests.cs`, `EquipmentManagementServiceTests.cs`: registration, optional clearing, stale edits, duplicates, unknown commits | Service behavior using fake session/repository implementations; writes/commit/no-replay assertions | HTTP status/Location, genuine uniqueness and retained PostgreSQL relationships still need Lab 6 tests, Sessions 3/6 |
| `MultiItemCheckoutTests.cs`: `ValidRequestCommitsEveryItemExactlyAtLimit`, `InvalidRequestMakesNoWrites`, `UnknownCommitDoesNotReplayWrites`; `WorkflowSessionTests.cs` | Pure coordination and simulated failures/unknown commits, including no replay | Fake session is not proof of actual transaction/commit state; Sessions 4/7 |
| `MultiItemPostgresTests.cs`: `SuccessfulCheckoutPersistsBothLoansAndAvailability`, `ControlledFailureAfterWritesRollsBackAllChanges` | Real migrations/transactions; observer reached after first actual save; new context sees no loans and available items after rollback | Uses different fixture/date, service calls rather than HTTP, retained DBs; adapt to TC6-E04 including safe 500 and fresh physical read, Session 7 |
| `BorrowingHistoryPostgresTests.cs`: `RelatedHistoriesFilterSubjectsAndRetainBothStatusesAndEmptySubjects` | Real query relationships, both statuses and empty subjects | Different fixture; no route/HTTP status assertions; Session 2 |
| `BorrowingSearchPostgresTests.cs`: `SearchFiltersSortDirectionsTiesAndResetMatchControlledRecords` | PostgreSQL search normalization, literal wildcard escaping, filters/sorts/ties and no-match/default behavior | Different fixture; no HTTP protocol or OpenAPI assertions; Session 5 |
| `IntegratedLifecyclePostgresTests.cs`: `RegisterEditBorrowRestrictReturnCheckoutAndReloadPreserveHistory`, `CompetingRequestsCannotExceedStudentQuotaOrDuplicateActiveItem` | Real lifecycle/fresh contexts and competing service operations | Not actual API restart, direct database constraint bypass or full TC6 fixture; Sessions 4/6/8 |
| `EquipmentBorrowing.UiTests/HttpClientTests.cs`: `ReadTransportFailureAndReceived500AreDistinctAndSafe`, `LostMutationResponseBlocksWritesUntilFreshReadWithoutReplay`, `SlowReadReturnsPendingTaskAndExposesBusyState` | Controlled HTTP handler responses, safe client feedback, no replay and busy-state logic | No reachable server fault/transactional state or human responsiveness observation; Sessions 7/9 |
| `BorrowingHttpClientTests.cs`: lost single/multi responses and confirmed-return request; `RetrievalHttpClientTests.cs`: latest response/history validation and full Desktop composition | Client request/response handling and dependency composition with HTTP doubles | No real routing/database evidence or completed manual acceptance; Sessions 7/9 |
| `tools/verify-student-api.py` and `verify-api-foundation.py` | Existing real-process HTTP/SQL/restart harness with Lab 5 expected/observed captures | Python is not the required xUnit mapping; current-date shifts differ from fixed D. Reuse actual API restart mechanics in Session 8 |

Legacy PostgreSQL helpers intentionally retain generated verification databases and skip when the private admin setting is missing. They remain unchanged as historical regression support. The new Lab 6 fixture owns and removes only its own generated databases; it never cleans up legacy/demo databases.
