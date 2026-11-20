# Lab 6 session plan - automated verification and regression

Sessions 1-8 are complete. All 22 automated objectives passed locally. Both manual checks are pending. See the progress and execution records on `lab6/automated-verification`.

## Requirements and scope

Read the supplied three-page `ITSD81_Lab6_Handout.pdf` and 26-page `ITSD81_Lab6_Test_Cases_and_Templates.pdf`. Exactly two weeks; the ten sessions below are checkpoints within those weeks, not ten weeks or extra deadlines. Schedule dates around the actual instructor deadline.

Deliver meaningful enabled xUnit assertions for all 22 objectives (TC6-U01-U09, TC6-U12-U16, TC6-E01-E08), real HTTP coverage of API-01-16, actual PostgreSQL integrity/transaction/persistence checks, and observed MA6-01/02 manual acceptance. Continue the [Lab 4 suite](../lab4/use-case-suite.md), [BR-01-19](../lab4/business-rules.md) and [Lab 5 API contracts](../lab5/endpoint-contract.md); do not reproduce their full specifications or add business modules.

No prescribed test-method count, coverage percentage, mocking library, full UI automation, Docker installation or new Windows release is required. Existing CI uses a PostgreSQL service container; local PostgreSQL remains sufficient. Rubric priorities: use cases 30%, HTTP/OpenAPI 15%, PostgreSQL integrity/transactions 20%, edges/recovery 15%, manual Desktop integration 10%, test quality/traceability/evidence 10%.

## Current implementation and reuse

- Baseline candidate: `d1be8c8bdabe0abb8c7d0220462f63efea5fd4db` on `lab5/api-integration`. Local and remote have no `lab5-submitted` tag. This is a verifiable code snapshot, not a verified Classroom submission; see [baseline](baseline.md).
- Existing `EquipmentBorrowing.Tests` and `EquipmentBorrowing.UiTests` use xUnit. Reuse pure-rule, PostgreSQL, HTTP-adapter and ViewModel assertions after checking their actual scope. Historical 76 backend + 23 UI/client passes belong to Lab 5 executions; they are not Lab 6 results.
- Existing backend test project does not reference the API host. The Python HTTP harness verifies real routes but does not itself satisfy the required xUnit mapping. Plan a focused `EquipmentBorrowing.ApiTests` project using the existing API `public partial Program` and `WebApplicationFactory<Program>`; use compatible .NET 10 test-host packages at implementation time. An equivalent real-host xUnit harness is acceptable if inspection reveals a practical reason.
- Existing `ICampusClock` supports test substitution. The Python Lab 5 harness shifts data around the current date; Lab 6 needs fixed D shared by the API and fixture. Existing PostgreSQL helpers create disposable databases but retain them; review safe cleanup and test independence before reuse.
- Reuse the actual API process/restart harness where suitable. Application-service process restarts alone cannot prove an API process restart.

## Fixture and assertion design

Proposed D: `2026-11-18`, interpreted as an Asia/Manila local calendar date. Confirm this constant in Session 1 and record it with executions. Inject a fixed `ICampusClock` only into the test host. Do not change the Windows clock or add a public production clock/fault switch. Keep the real-process restart check separate if its production clock uses the actual campus date; record both date arrangements explicitly.

| Alias | Required starting state relative to D |
|---|---|
| S1 | ST-100 Ada, eligible; E1 active; E3 returned |
| S2 | ST-200 Ben, ineligible; no active borrowing |
| S3 | ST-300 Cara, eligible; E4 and E5 active |
| S4 | ST-400 Dani, eligible; no borrowings |
| E1 | EQ-001 Camera, unavailable; S1; borrowed D-9, expected D-1 |
| E2 | EQ-002 Laptop, available |
| E3 | EQ-003 Projector, available; S1 returned D-2, borrowed D-8, expected D+2 |
| E4 / E5 | EQ-004 Tablet / EQ-005 Microphone, unavailable; S3; borrowed D-7, expected D+2 |
| E6 / E7 | EQ-006 Tripod / EQ-007 Speaker, available |

Apply the existing migrations to a newly generated, disposable PostgreSQL database. Resolve fixture identifiers from API responses, not assumed sequence values. Reset or isolate independently before every scenario and parameterized subcase, including each limit test. Use unique databases or coordinate a shared fixture so parallel execution cannot interfere. Keep historical/demo databases intact; clean up only database names owned by the current run, after disposing hosts/connections and retaining needed evidence.

Assert response fields, generated IDs, Location where required, relationships, dates, exact membership/order, and unchanged unrelated rows. For mutations compare independent before/after snapshots. Persistence claims require disposed write contexts and fresh no-tracking contexts/connections; use an independently opened PostgreSQL connection for explicit fresh-connection claims. Direct PostgreSQL bypass attempts prove constraints that an API-only rejection cannot. Fakes prove the rule/client behavior under their simulated conditions, not routing, database constraints or rollback.

## Week 1 - infrastructure and use-case/API verification

### Session 1 - baseline, coverage audit and deterministic test infrastructure

Explain: establish a safe test laboratory and identify which existing assertions genuinely cover the assignment.

Reconcile the newer twelve-step screenshot report against older Lab 5 pending statements. Record the authentic snapshot and any unavailable submission provenance without inventing a submitted tag. Inspect existing assertions and list reusable coverage versus gaps. Add the real-pipeline xUnit host, isolated PostgreSQL fixture/reset, fixed D and fresh-read helpers. Keep test substitutions confined to test composition. Verify a known route, fixture contents, empty reset and an independently repeated fixture setup; document database cleanup and credential-free setup.

Exit: the test infrastructure runs against PostgreSQL and the real ASP.NET pipeline; baseline and gap audit are recorded. Commit/push: `test(lab6): add deterministic API and PostgreSQL fixtures` plus concise observed evidence. A smoke check does not mark entire TC6 scenarios Passed.

### Session 2 - catalog, active lists and histories

Explain: test the read side with exact records, including the difference between empty history and a missing subject.

Implement TC6-U01/U02/U04/U07/U08 through HTTP. Assert all seven catalog items, availability, E2/E3/E6/E7 available-only, empty catalog/none-available, active E1/E4/E5 with correct students, returned E3 excluded, correct student/item dates and relationships, empty S4/E2 history and missing-subject 404. Exercise subject detail API-03/07 explicitly. Confirm read-only operations leave database state unchanged.

Exit: these five objectives have enabled assertions and recorded outcomes. Commit/push: `test(lab6): verify catalog active loans and histories`.

### Session 3 - student/equipment management and protected edits

Explain: verify that editing a person or item preserves its identity and borrowing history.

Implement TC6-U12/U13/U14/U15/U16 and TC6-E06. Cover create defaults/generated ID/Location and GET, student search matching ST-100 and no match via API-02, complete updates, null optional clearing, and unchanged active/returned foreign keys. Restrict S1, reject new issue, allow return, re-enable and issue. Reject duplicate equipment, blank required fields and protected `isAvailable`; compare fresh rows and history after each rejection. Audit API-02-09 coverage.

Exit: management, eligibility sequence and optional/protected-field objectives are recorded. Commit/push: `test(lab6): verify management eligibility and protected fields`.

### Session 4 - issue, return, coordinated checkout and limits

Explain: prove the whole borrowing lifecycle and the exact three-active-loan boundary.

Implement TC6-U03/U05/U06 and TC6-E02. Issue S4/E2 due D+2; follow Location and assert date D, identity and item state. Return S1/E1 dated D, preserve row/history, repeat return 409 unchanged. Checkout S4/E2/E6 and assert both IDs/references/states. Reset separately for S3+E2 single (201, exactly three), S1+E2/E6 multi (201, exactly three), S3+E2/E6 multi (409, no partial writes).

Exit: success, repeat-return and all three limit subcases have fresh-state evidence. Commit/push: `test(lab6): verify borrowing lifecycle checkout and limit boundaries`.

### Session 5 - search, protocol responses and OpenAPI consistency

Explain: test useful response contents and predictable failures across all routes.

Implement TC6-U09 and TC6-E03. Cover each case-insensitive partial search field, every status filter, both sort fields/directions, combined criteria, Clear/defaults, no matches and stable ID ties. Exercise malformed JSON, invalid/missing/wrong-type fields, invalid dates, missing resources, unknown route 404, known route wrong method 405 and business conflict 409; assert safe bodies and unchanged state. Compare live OpenAPI paths/methods, request/response schemas and documented statuses against actual API-01-16 behavior and authoritative contracts. Keep supplementary contract assertions linked in traceability.

Exit: all fourteen U objectives implemented; all sixteen API contracts have identified real-HTTP assertions. Investigate first failures, record defects and corrections, then run affected tests and the relevant broader regression suite. Commit/push: test work and any corrections in coherent separate commits.

## Week 2 - database failures, recovery and handoff

### Session 6 - PostgreSQL uniqueness, constraints and stale state

Explain: prove the database protects data even if a caller bypasses API validation.

Implement TC6-E01/E05. Exercise case/whitespace duplicate creates and updates, assert 409 and fresh counts/unchanged rows, then directly attempt duplicates in PostgreSQL. Observe E2 available, borrow it from another client/context, submit stale single and multi requests, assert conflict/one active row and no partial other-item changes. Directly attempt a second active row and inspect the actual PostgreSQL rejection. Review foreign keys and applicable existing constraints; retain failing assertions if any protection is missing.

Exit: actual constraint failures and independent final state captured. Commit/push: `test(lab6): verify PostgreSQL constraints and stale conflicts`; corrective schema/service work separately if needed.

### Session 7 - after-write rollback and uncertain outcomes

Explain: a confirmed rollback and a lost response are different situations and require different evidence.

Implement TC6-E04/E07a/E07b/E07c. Induce an observed fault after a genuine PostgreSQL write attempt inside multi-checkout; assert safe HTTP 500, fault reached, disposed transaction/context and identical independent before/after state for E2/E6 and all relevant rows. Prefer existing test-only write observer or isolated trigger with reliable evidence that the failure point ran; never infer rollback solely from a 500.

Use a controlled HTTP double for E07a unreachable/no status. Use reachable real API + PostgreSQL fault for E07b received safe 500/client feedback. For E07c send a real mutation and deliberately suppress its acknowledgement at the client boundary, assert unknown-outcome feedback and exactly one submission/no automatic replay, then independently read PostgreSQL before further action. Record that acknowledgement suppression models response loss, not a physical interruption during database commit; do not claim a transport fake proves rollback. Assert the observed all-or-none state without assuming which outcome a lost acknowledgement implies.

Exit: each E07 mechanism/limitation is explicit and independent state supports every transaction claim. Commit/push: `test(lab6): verify rollback transport failures and unknown outcomes`.

### Session 8 - fresh connections, actual API restart and integrated CI

Explain: data must survive beyond the context and process that wrote it.

Implement TC6-E08 xUnit creates/issue/fresh read/return/another fresh read. Assert IDs, values, relationships, availability and retained returned history. Extend the existing integration harness to stop/restart the actual API executable without resetting its database and compare HTTP with fresh SQL; document the method, process lifecycle and date configuration. If harness reuse is unsuitable, supervised manual actual API restart is allowed, while fresh-connection assertions remain automated.

Add the Lab 6 branch/new test project to CI and preserve TRX, concise HTTP/SQL evidence and live OpenAPI. Make missing PostgreSQL configuration visible: skips are not passes; CI must actually execute required database cases. Run the broader relevant regression suite, diagnose and correct failures, then retest. Record tested source revision, environment and D.

Exit: all 22 automated objectives have actual execution outcomes and restart evidence has a declared method. Commit/push: `test(lab6): verify fresh persistence API restart and CI`.

### Session 9 - manual Desktop acceptance and final corrections

Explain: automated tests support the app, but someone must observe its responsiveness and cross-client behavior.

Perform MA6-01 with controlled delayed HTTP: show loading, interact with the window while pending, stop/restore the isolated API, observe understandable failure/recovery and confirm no unintended writes. Perform MA6-02: API works with Desktop closed, live OpenAPI agrees, make one independent-client mutation, refresh Desktop, verify the same persisted change, and inspect HTTP-only project/runtime dependencies. Keep database settings out of Desktop. Use short recordings or a few targeted screenshots and actual observations; prior Lab 5 images are supporting historical evidence only.

Record defects with expected/observed, likely layer, UC/BR/API/TC6, correction revision, targeted retest and broader regression. Retain unresolved failing tests and disclose limitations. No full UI automation is required.

Exit: both manual checks have observed outcomes and evidence; final code fixes have completed regression. Commit/push: fixes separately from acceptance records.

### Session 10 - traceability, reproducibility and submission revision

Explain: make every requirement easy to find and every result reproducible.

Complete [traceability](traceability.md), [execution/defect/retest records](test-execution.md), [baseline](baseline.md), [progress](progress.md), README test setup and a compact evidence inventory. Replace pending implementation entries with exact test names only after inspection; link actual outputs. Ensure all 22 automated objectives, two manual checks and sixteen APIs are accounted for, with unresolved failures/skips honestly disclosed. Verify documented setup/commands and final relevant regression at the final source revision. Keep final source revision distinct from later evidence-only commits.

Commit/push the completed handoff. Once the work and disclosures are complete, create an annotated `lab6-submitted` tag on the final committed revision and push it explicitly; record its peeled commit without moving existing tags. Do not tag this planning checkpoint. Verify remote branch/tag. Each confirmed partner submits individually with identities privately; do not infer partner names or claim Classroom submission.

## Every session: explain, synchronize, implement, verify, save

Before work, explain the purpose, intended change, evidence needed and completion condition in plain language. Read [progress](progress.md), inspect `git status --short --branch`, then `git fetch origin` and `git pull --ff-only` once the branch has its upstream. If there are local edits or divergence, preserve them and reconcile normally; never reset them away or force-push.

Implement only the current session's scope and necessary corrections. Run checks appropriate to the change; use existing assertions/fixtures where meaningful. Update the one progress ledger, one execution/defect record and one traceability matrix. Stage explicit intended paths, inspect the staged diff for credentials/unrelated files, commit coherent completed parts, then `git push`. The initial branch publish uses `git push -u origin lab6/automated-verification`.

After work, explain what changed and why, actual verification outcomes (including skipped/failed/not run), relevant limitations, commit/push status and the next checkpoint. A pushed commit is a session checkpoint, not proof of acceptance. Pause between implementation sessions for the user's next-session instruction; the initial request authorizes planning and Git synchronization, not silently completing all ten sessions.
