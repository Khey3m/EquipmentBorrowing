# Lab 5 session plan - API integration and acceptance

## Scope and source

Read both supplied ITSD81 Laboratory Activity 5 PDFs: the two-page activity and the fifteen-page API and Acceptance Reference. Duration is exactly two weeks. Sessions below are work checkpoints, not extra weeks. Preserve Lab 4 UC-01-09/12-16 and BR-01-19; reuse its authoritative suite instead of duplicating fourteen tables.

Deliver sixteen independently runnable /api/v1 JSON contracts, machine-readable OpenAPI, an asynchronous Avalonia HTTP client, PostgreSQL integrity, and consolidated T01-T08 evidence. No Docker, deployment, authentication or additional web/mobile client is required. Do not expand the business scope to previously deferred use cases solely because Lab 5 adds transport.

## Boundary

Before: Desktop -> Application services -> Infrastructure/EF -> PostgreSQL.

After: Desktop -> asynchronous HTTP client -> ASP.NET Core API -> Application services/contracts -> Infrastructure/EF -> PostgreSQL. A small shared transport-contract project is proposed for JSON DTOs; it must not reference EF or Infrastructure. Desktop may retain presentation-oriented code, but cannot hold SQL, contexts, connection strings or a direct persistence dependency. Review whether remaining Application/Domain references are necessary rather than moving server services into the client.

The API owns validation, transactions and checkout/return dates. Reuse business coordination and migrations. Refactor structured result/error categories as needed: current services sometimes combine missing/stale outcomes and use broad rejection categories, while HTTP must distinguish 400/404/409/500 reliably without parsing human messages.

## Week 1 - independently functioning API

### Session 1 - baseline, contracts and host foundation

Record the Lab 4 package/commit and baseline limitations. Add API host and transport contracts to the solution, DI composition, configurable local URL, safe exception handling and machine-readable OpenAPI. Draft the sixteen-route catalog with exact fields/statuses. Add structured outcome categories where needed; retain original rules. Validate API startup with Desktop closed, OpenAPI retrieval, unknown-route 404 and wrong-method 405. A host-only checkpoint is not a pass for T01. Commits: baseline/contract foundation, host/OpenAPI, observed evidence.

### Session 2 - student endpoints (API-02-06)

Implement list/search, detail, registration with Location, complete editable update/null contact clearing, and explicit eligibility PATCH/no-op. Preserve identity and histories. Reject missing/blank/wrong-type inputs and duplicate normalized numbers; missing records return 404. Ensure missing isEligible is not silently false. Verify independent HTTP requests plus fresh DB reads for T02 branches. Commits: student endpoint behavior, focused checks/evidence.

### Session 3 - equipment endpoints (API-01/07-09)

Implement catalog/available boolean, detail, create/Location and update/null description clearing. Reject malformed boolean, invalid/duplicate names/codes and unknown/protected update fields, including isAvailable. Availability starts true and is governed only by borrowing/returns. Verify T03 plus relevant T01 reads against controlled data. Commits: equipment endpoints, observed contract/data evidence.

### Session 4 - single issue and return (API-10/12-13)

Implement issue, borrowing detail and return with required empty JSON body. Server owns dates/status; expectedReturnDate must be later than campus date. Map invalid input/date to 400, missing references to 404, eligibility/limit/availability/repeat-return conflicts to 409. Verify T04 success/rejections, retained row/identity, fresh DB reads and API restart persistence. Commits: borrowing endpoints, validation and T04 evidence.

### Session 5 - atomic checkout and complete query API (API-11/14-16)

Add coordinated checkout, search/status/sort/direction defaults, active list, student/equipment histories and related display fields. Verify exactly-at-limit single and multi paths, over-limit rejection, all invalid requests, stable ID ties, empty versus missing. Use the existing test-only after-write observer through an isolated verification host for rollback; no public production fault switch. Finish T01/T05/T06 independent HTTP execution and OpenAPI coverage of all sixteen contracts. Commits: checkout, queries, acceptance evidence.

## Week 2 - Desktop integration, diagnostics and handoff

### Session 6 - HTTP client and first Desktop screens

Add typed asynchronous HTTP transport, configurable API base URL, JSON/date serialization and shared error handling. Distinguish received safe error responses from no response; transport failure during mutations can mean unknown outcome, so never automatically replay writes. Migrate catalog, students and equipment management. Keep intermediate migration explicitly incomplete; remove all remaining direct DB access by Session 8. Verify real wire traffic and independent updates reflected after refresh. Commits: HTTP client, management-screen migration, evidence.

### Session 7 - checkout, return and synchronization

Migrate single/multi checkout and active loans/return to API. Refresh affected lists, details and availability; preserve confirmation, rejection explanations and stale-state verification. Keep network operations cancellable/asynchronous; disable duplicate submissions without freezing navigation. Verify server revalidation and expected UI behavior. Commits: checkout/return migration, synchronization/diagnostics.

### Session 8 - histories/search and completed boundary

Migrate histories/search/status/sort/reset with latest-result guards and cancellation. Move database initialization/migration responsibility out of Desktop; API unavailability should not require a Desktop DB connection. Remove Desktop Infrastructure/EF package references and replace any tests that assumed direct persistence composition. Review all fourteen workflows and complete T07 API-alone/UI/independent-update evidence. Commits: retrieval migration, direct-DB dependency removal, T07 evidence.

### Session 9 - controlled acceptance and recovery

Run T01-T08 against reset datasets; preserve all expected/observed outputs and failed attempts. Distinguish stopped API/no response from running API with invalid disposable DB/received safe 500. Restore and retry; restart API/Desktop and read committed data freshly. Confirm known-failure rollback after actual write attempt and document unknown commit limitations. Run appropriate regression and UI checks; update CI to cover Lab 5 branch and HTTP integration. Commit focused corrections separately from evidence. No pass claims based solely on compilation or HTTP status.

### Session 10 - final documentation and submission

Complete one consolidated T01-T08 execution record with diagnosis/correction/retest fields and actual evidence references. Finish fourteen-case API traceability, architecture/boundary and baseline records, endpoint/OpenAPI documentation and independent API/Desktop setup commands. Document credential-free configuration, migrations and repeatable disposable reset. Package source/history and mark unperformed manual checks honestly. After required work is actually complete, create and push lab5-submitted on the final revision; never tag an incomplete planning checkpoint. Both partners submit individually with identities in private submission fields.

## Acceptance dataset and evidence discipline

Build a repeatable isolated reset mechanism for S1?S4 (ST-100 Ada Cruz, ST-200 Ben Diaz, ST-300 Cara Lim, ST-400 Dani Ong) and E1?E7 (Camera, Laptop, Projector, Tablet, Microphone, Tripod, Speaker), with the exact active/returned relationships from reference Section 3. Lab 4's current demo database is not this dataset and must not be overwritten.

Reset before each T01-T08 test unless its steps specify otherwise, and separately for T05 subcases. Resolve opaque IDs from actual API responses. Prefer actual campus date with all historical/future dates shifted by the reference offsets, recorded consistently; fixed 2026-11-10 is only acceptable in a controlled test clock, never an arbitrary machine clock change.

For every mutation capture before state, actual HTTP method/path/body/status/response and fresh PostgreSQL after state. T05 requires a documented after-write failure point, safe 500 and no partial rows/flags. T08 requires no-response versus received 500, safe messages, recovery and restart readback. Screenshots supplement HTTP/DB evidence, not replace it.

## Git and efficient session workflow

Work on lab5/api-integration, preserving Lab 4 history. Start each session with status/fetch/fast-forward pull; resolve upstream changes normally without force-push or rewriting prior commits. Commit every authored source/doc change in coherent parts with action-oriented title and body describing progress, validation and limitations; push each completed part. Exclude credentials, bin/obj and private student identities.

Explain each session's purpose before implementation, then report outcomes and the next checkpoint briefly. Read only relevant files, batch independent inspection, reuse existing services/tests and run checks proportional to changed behavior. Do not create redundant reports or extensive tests mirroring code. Keep one progress ledger, one execution record and one traceability matrix; reuse the Lab 4 suite.

## Current state

Planning only. Sessions 1?10 implementation has not started; all T01-T08 are Not run for Lab 5. The existing Lab 4 tests are historical evidence for their revisions, not HTTP acceptance passes.
