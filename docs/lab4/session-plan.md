# Lab 4 — Session-by-session implementation plan

## Scope and starting point

Based on both supplied Lab Activity 4 PDFs, including BR-01–19 and Appendix C. Continue the existing Campus Equipment Borrowing System and preserve Git history.

Repository: https://github.com/Khey3m/EquipmentBorrowing

Local project: `C:/Users/kheye/Documents/3RD YEAR/DESKTOP DEVELOPMENT/FINAL LAB 1/EquipmentBorrowing-Final-Output-Activity-1`.

Required: UC-01–09 and UC-12–16 (14 cases). UC-10–11 and UC-17–27 are Lab 5; UC-28–33 are future scope. Do not implement those extras during Lab 4.

PostgreSQL becomes the operational database. Parallel SQLite support and importing historical SQLite rows are not required. Local PostgreSQL is sufficient; Docker/cloud/paid services are unnecessary.

## Working agreement for every part

Before editing, explain the part's purpose, user-visible behavior, affected layers and verification in a short message. Then implement and verify one coherent part, summarize actual results, commit only its files, and push. Continue under the user's authorization without repeatedly asking permission for routine changes or Git synchronization.

At the beginning of each part, inspect status, fetch origin and pull with `git pull --ff-only` when the worktree permits it. If unrelated changes prevent synchronization, preserve them and use an isolated worktree/branch; never reset or automatically stash someone else's work. Resolve upstream divergence deliberately; never force-push. At the end, stage explicit paths, inspect the staged diff for unrelated changes and secrets, commit, push, and confirm the remote branch contains the commit. Do not claim a push succeeded without checking its result.

Proposed implementation branch: `lab4/postgresql-workflows`, preserving main and earlier history. Push each completed part to this branch. Pull/merge upstream main changes into the feature branch as needed; do not blindly pull main into an unrelated branch. Final integration can use a reviewable pull request. The user can request direct-main development instead.

Current pre-existing changes: modified `docs/database-queries.sql` and untracked `.github/`. Inspect and preserve; do not sweep into feature commits. Remote HEAD matched local latest commit `2970cdb` during planning; remote access works, but push permission is not yet verified.

Use a small progress ledger recording completed session, commit, validation result, blockers and next step. Read the ledger at later sessions rather than repeating the full investigation. One meaningful commit per part is a useful default, not an assignment requirement. Fix failing parts before labeling them complete; if external execution is blocked, commit only with an explicit validation limitation.

## Week 1 — Foundations

### Session 1: Establish a reliable baseline and traceability

Explain: protect the earlier work and identify what actually runs before changing persistence.

- Inspect applicable repository instructions, current changes, package versions, SDK/tooling and Git state.
- Create the working branch or isolated worktree and retain earlier history.
- Investigate the previous Avalonia MSB4061 build failure and Windows Application Control test-loading block without disabling security policy. Fix project/tooling issues where appropriate; record any remaining environment requirement.
- Correct the unavailable-equipment test fixture, which currently selects an available item.
- Map all 14 cases and A01–A19 requirements to planned services, screens and evidence. Mark unverified items honestly.
- Inventory local PostgreSQL/client availability and establish secret-free connection configuration; obtain missing local connection details only if necessary.

Exit: reproducible baseline or clearly isolated external blocker; initial traceability and progress ledger. Commit: `chore: establish Lab 4 baseline and acceptance tracking`.

### Session 2: Extend the domain and define integrity rules

Explain: distinguish stable internal database IDs from staff-facing student/equipment identifiers, and preserve history while details change.

- Student: institutional number, normalized uniqueness key, full name, optional contact, eligibility; new students eligible.
- Equipment: staff identifier, normalized uniqueness key, name, optional description; new items available.
- Borrowing: checkout date, expected return date, nullable actual return date, active/returned state, stable generated ID.
- Replace random borrowing IDs with reliable database-generated identities and compatible in-memory behavior where retained.
- Enforce three active items, expected return strictly after checkout, and actual return no earlier than checkout. Use local calendar dates consistently; document Asia/Manila as the proposed campus timezone and keep the clock/timezone injectable/configurable.
- Normalize identifiers by trimming and case-insensitive canonical comparison; ensure application and database enforcement agree. Blank optional contact/description clears to absent.
- Define use-case request/result types and repository/query contracts. Keep EF/database types outside Views and ViewModels.

Exit: compiling model/contracts and focused rule checks, with no accidental Lab 5 scope. Commit: `feat: define Lab 4 identities dates and domain rules`.

### Session 3: PostgreSQL, schema and operation lifetimes

Explain: the database must independently reject invalid relationships and duplicate active loans, and each operation needs a fresh, correctly disposed context.

- Replace operational SQLite wiring with compatible EF Core/Npgsql packages, checking compatibility before selecting versions.
- Keep local credentials in environment/local secret configuration and commit only placeholders/instructions.
- Introduce Infrastructure-owned per-operation contexts and a transaction/unit-of-work abstraction so related repositories share one context during a write workflow. Do not use one root-resolved context for the window lifetime.
- Create PostgreSQL migrations with PK/FK constraints, required/nonblank fields, normalized unique student/equipment identifiers, and a partial unique index allowing only one active borrowing per equipment.
- Add applicable date/status consistency checks and history-preserving restrictive relationships.
- Keep old SQLite migration history clearly separated/archived as appropriate; ensure clean PostgreSQL migration discovery does not apply SQLite migrations.
- Seed safe example data only when appropriate without overwriting existing records.
- Move connection/migration work into Infrastructure/bootstrap services; Views/ViewModels must not configure connections or execute migrations.

Exit: migrations apply to a disposable clean PostgreSQL database; inspect schema/history and prove invalid FK, normalized duplicates and second active borrowing are rejected (A17). Commit: `feat: add PostgreSQL persistence and integrity constraints`.

### Session 4: Preserve the five existing workflows

Explain: catalog, availability filtering, single borrowing, active list and return must still work against PostgreSQL.

- Implement query/application services for catalog and active borrowings, with related names/identifiers and dates.
- Single checkout revalidates references, eligibility, count, dates and availability inside the transaction boundary; save borrowing and availability together.
- Return records the actual local return date on the same borrowing, restores availability atomically and rejects repeat returns.
- Establish conflict handling: serialize competing writes for the same student to protect the three-item limit and coordinate equipment changes consistently; choose deterministic locking/concurrency mechanics within Infrastructure. Database active-item uniqueness is the final guard.
- Add clear success, validation rejection, missing/stale selection, empty, loading and database-error states with retry/refresh.
- Distinguish known failed writes from unknown commit outcomes; an acknowledgement loss must not trigger automatic replay or a false rollback claim.

Exit: A01–A05 with fresh database reads and borrow/restart/return/restart evidence. Commit: `feat: preserve borrowing and return workflows on PostgreSQL`.

### Session 5: Student management

Explain: staff can register, locate, edit and restrict students without breaking existing loans.

- Implement UC-12 registration, UC-13 locate/select/edit and UC-14 permit/restrict through services and MVVM forms.
- Reject blank required data and normalized duplicate institutional numbers, including concurrent duplicate insertion.
- Permit contact clearing; preserve internal IDs and linked history across edits.
- Restriction blocks new checkout but permits returns and retains active loans; repeated-state changes give useful feedback.

Exit: A12–A14 including restart readback and effects on both new and existing borrowing. Commit: `feat: add persistent student management and eligibility`.

### Session 6: Equipment management

Explain: staff can register and edit descriptive inventory information while borrowing workflows control availability.

- Implement UC-15 registration and UC-16 locate/select/edit with identifier, name and optional description.
- Reject blank/duplicate normalized identifiers; allow description clearing.
- Protect availability, borrowing status and return dates from descriptive edit requests and UI controls.
- Preserve identity and linked history; newly registered items start available without active loans.

Exit: A15–A16, including protected-field checks and restart evidence. Commit: `feat: add persistent equipment registration and editing`.

## Week 2 — Coordinated checkout and retrieval

### Session 7: Atomic multi-item checkout

Explain: a request for several items succeeds entirely or leaves none of its changes behind.

- Implement UC-06 selection of one student, at least two distinct equipment items and one expected return date.
- Reject duplicate IDs instead of silently deduplicating; reject missing/unavailable items, invalid dates, ineligible students and requests exceeding three total active items.
- Reuse single-checkout rules and concurrency protections through a shared application workflow where practical.
- Commit one borrowing per selected item and every availability change in one transaction.
- Add a controlled failure seam restricted to verification, or arrange a safe constraint failure after a write is attempted, using a disposable database.
- Verify simultaneous/stale requests cannot bypass quotas or create duplicate active item loans; refresh after conflicts.

Exit: A06 success, exactly-at-limit, all rejection cases and proven rollback with fresh before/after reads. Commit: `feat: add atomic multi-item equipment checkout`.

### Session 8: Student and equipment histories

Explain: selecting a student or item shows its own active and returned records with related identities and dates.

- Implement UC-07 and UC-08 with asynchronous query services and master-detail views.
- Show checkout, expected return, actual return when present, status and related student/equipment data.
- Handle rapid selection changes with cancellation or request versioning so old results cannot overwrite the latest selection.
- Distinguish loading, empty history, missing/stale subject and connection failure; retain a responsive UI.

Exit: A07–A08 using two distinguishable subjects of each type plus empty histories and error cases. Commit: `feat: add asynchronous student and equipment histories`.

### Session 9: Borrowing search, filter and sort

Explain: staff can find records using combined criteria without changing stored data.

- Implement UC-09 trimmed case-insensitive partial search over student name, institutional number and equipment name.
- Filter Active/Returned/All; sort checkout or expected return date ascending/descending with borrowing ID as stable tie-breaker.
- Combine criteria and reset to the full list; execute filtering/sorting in database queries.
- Provide cancellation/request sequencing, loading/error/no-match feedback and retry while keeping UI responsive.

Exit: A09 covers each search field, every filter, four sort combinations, ties, combined criteria, reset and connection error. Commit: `feat: add borrowing search filters and stable sorting`.

## Week 3 — Evidence and submission

### Session 10: Integrated verification and recovery

Explain: independently working features must also agree when used together and after restart or failure.

- Execute Appendix C's nine-step integrated demonstration sequence using disposable PostgreSQL data.
- Recheck constraints on a clean migrated database; use fresh contexts/connections for verification reads.
- Prove known in-transaction failures leave no partial data; verify clear handling and refresh for unknown commit outcomes without asserting rollback or automatically replaying writes.
- Verify reconnect/retry, stale selections, conflicting changes, preserved history, date handling, UI responsiveness and persisted registration/edits/eligibility.
- Run the build and targeted meaningful checks. Automated tests are optional under the assignment; favor focused domain/query/transaction checks over tests that merely repeat implementation.
- Fix defects with scoped commits; record actual outcomes, not anticipated passes.

Exit: A01–A09 and A12–A19 evidence complete or explicitly recorded external limitations. Commit(s): `test: verify Lab 4 integration atomicity and recovery` plus specific fixes when needed.

### Session 11: Documentation, walkthrough and handoff

Explain: the instructor must be able to reproduce and assess the implementation from the submitted repository.

- Update README with prerequisites, local PostgreSQL setup, secret-free configuration, migration, seeding and launch commands.
- Explain context lifetime, transaction boundary, identifier normalization, local date policy and concurrency decisions.
- Supply updated use-case diagram (Laboratory Staff / Equipment Custodian as primary actor), catalog with deferred classifications, complete adapted 14-case suite and traceability matrix.
- For every acceptance scenario record setup, action, expected result, observed result and evidence location. Screenshots alone do not prove persistence or atomicity.
- Include migration/schema output, fresh readback/restart results, safe failure evidence and explicit Git history evidence.
- Preserve useful earlier documentation and check that claims match implementation.
- Prepare a short demonstration script and final reviewable PR; verify clean-checkout setup where practical.
- Remind each submitter to provide their own/both partners' names and IDs privately as specified; do not invent them or unnecessarily publish them to GitHub.

Exit: reproducible submission package and remote commits confirmed. Commit: `docs: complete Lab 4 setup traceability and verification evidence`.

## Token and session efficiency

- Start each part with a short explanation; finish with changed behavior, validation, commit/push result and next part.
- Read only the relevant source and ledger, reuse requirements mapping, and avoid repeating full PDFs or reports.
- Keep one coherent part in progress, with targeted checks and concise evidence.
- Preserve logs/artifacts locally or in appropriate repository docs instead of pasting long output into chat.
- Treat session numbers as work checkpoints; one conversation can cover several parts when useful, while still explaining and syncing each part.

## Assessment priorities

R1 existing workflows 15%; R2 PostgreSQL/integrity 15%; R3 multi-item transactions 18%; R4 student management 12%; R5 equipment management 10%; R6 histories/retrieval 12%; R7 architecture/responsiveness 10%; R8 verification/documentation 8%.

The order builds prerequisites early and reserves real time for the transaction and evidence requirements. A successful build alone does not establish completion.
