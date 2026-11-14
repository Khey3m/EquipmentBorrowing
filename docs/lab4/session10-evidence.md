# Session 10 — Integrated verification and recovery

Date: 2026-11-14. This record distinguishes local execution restrictions, Linux CI results, automated UI checks and remaining manual acceptance.

## Verification environment

Local Windows execution continued to encounter Application Control assembly-loading blocks; Code Integrity event 3077 confirmed them. Security settings were not changed. Rather than treating those failures as application test results, a GitHub Actions verification workflow was added using a Linux .NET 10 runner and isolated PostgreSQL 18 service.

The workflow restores repository tools/packages, builds the complete Release solution including Avalonia, runs all backend tests with PostgreSQL enabled, runs headless UI tests, executes separate-process persistence stages and retains TRX artifacts. Its database password is generated solely for that disposable CI service from the run identifiers; the user's local password is never sent to GitHub or committed.

Primary workflow references: [GitHub PostgreSQL service containers](https://docs.github.com/en/actions/tutorials/use-containerized-services/create-postgresql-service-containers), [setup-dotnet](https://github.com/actions/setup-dotnet), and [Avalonia headless XUnit testing](https://docs.avaloniaui.net/docs/testing/headless-xunit).

## Completed backend verification

The first CI run, [43557670588](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43557670588), built the complete Avalonia solution and passed all 73 then-existing backend tests, with no skips. This establishes current Session 9 search integration behavior, not just the historical Session 8 pass.

The next successful run, [43557792425](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43557792425), passed all 76 backend tests after adding three integrated PostgreSQL scenarios:

1. **Lifecycle and history preservation:** initialize twice; verify one real migration and stable seed counts; register student/equipment with generated identities; reject normalized duplicates; edit/clear optional fields; borrow; restrict new borrowing while allowing the existing return; record actual return; permit borrowing; issue two items together; reach the three-item limit; reject another issue; reject a stale equipment edit; edit a currently borrowed item without making it available; reinitialize without overwriting data; verify joined history/search and final counts through fresh contexts.
2. **Competing requests:** submit two coordinated requests for different item pairs against one student's quota, accept one request without partial changes from the other, then compete for the same remaining item and verify exactly one active borrowing. These tests submit overlapping tasks; they do not force every possible database interleaving or replace production load testing.
3. **Connection failure and recovery:** use a deliberately unavailable local port, verify startup/read/write failure without false success, restore a working factory and verify successful initialization/catalog access and unchanged borrowing rows.

Existing tests also cover clean EF migrations/model agreement, constraints, SQL search/filter/sort/reset, subject histories, controlled checkout rollback after writes, and application-level unknown-commit handling without replay. Actual transport loss precisely during commit is not simulated; that path has typed error mapping and injected-service outcome coverage.

## Defects and warnings fixed

- Qualified Avalonia.Application in the new headless test app after CI found a namespace collision with EquipmentBorrowing.Application.
- Matched the generated CurrentView change callback's nullable old-value signature.
- Replaced obsolete TextBox.Watermark with PlaceholderText in three management/search views.
- Aligned the headless test project with Avalonia 12's xUnit v3 dependency and executable test-host requirement.
- Corrected test assumptions exposed by real controls: target the named search input rather than template TextBoxes, and model a stale subject as a listed selection whose backend record is missing rather than an item absent from ItemsSource.

The warning fixes were prompted by the first successful full build, which had four warnings. A failed intermediate headless-project build and superseded/cancelled runs remain visible in Git history/Actions; they are not hidden or counted as passes.

## Headless UI verification added

EquipmentBorrowing.UiTests uses Avalonia's in-memory test platform and Fluent theme, without visible windows or a production database. Three tests exercise:

- Search textbox binding, responsive pending query, newer criteria replacing a deliberately delayed older result, and Clear resetting both the bound input and results.
- Student/equipment history selection replacing rows and distinguishing empty/missing subjects.
- Loading and laying out all seven workflow views.

These checks exercise actual Avalonia control trees/layout/bindings with fake read services. They do not establish screenshots/pixel appearance, accessibility, all button workflows with a live database, or manual desktop usability.

## Separate-process persistence verification added

The verification-only PersistenceProbe runs five separate dotnet invocations: init, borrow, verify-borrow, return and verify-return. It uses real Application services/EF persistence, restrictive verification-only database naming, generated identities and fresh-process reads. It checks the active/unavailable state after the borrower process exits, then the same retained returned borrowing/date and restored availability after the return process exits.

The probe never deletes/reuses a production database and omits credentials from output. This establishes application-service persistence across actual process boundaries when the workflow passes. It is distinct from closing and reopening the real Avalonia Desktop executable.

## Latest combined verification

The combined run for commit `8ad740b`, [43558140880](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43558140880), passed a zero-warning full build and all 76 backend tests but exposed the headless test-host setup problem. The next run, [43558371252](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43558371252), passed the zero-warning build, 76 backend tests and all-view layout test, but exposed the two test assumptions described above. The separate-process step was skipped after the headless failures; it is not claimed as passed by those runs. The final corrected run is recorded below after completion.

**Final successful combined run:** [43558553901](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43558553901), commit `1e5328dd14e0774ea789c002b3c80cabc1d40f2e`.

| Verification | Final observed result |
|---|---|
| Full Release solution, including Desktop/UI tests/probe | Build succeeded: zero warnings, zero errors |
| PostgreSQL-enabled backend suite | 76 passed, zero failed, zero skipped |
| Avalonia Headless suite | 3 passed, zero failed, zero skipped |
| Persistence probe: init | Passed in its own process |
| Persistence probe: borrow | Passed in its own process |
| Persistence probe: verify-borrow | Passed after borrower process exited |
| Persistence probe: return | Passed in its own process |
| Persistence probe: verify-return | Passed after return process exited; same history retained |
| Test evidence | Uploaded `lab4-test-results` TRX artifact on the run page |

Total automated tests: **79 passed** across two test projects, plus five successful process stages. Logs include each stage's explicit PASS line. This run supersedes earlier source-build/test setup failures for the verified commit without changing local Windows policy.

The process-restart evidence concerns the verification executable using real Application services. The complete real Desktop close/reopen walkthrough and pixel/usability review remain manual, as described in [the checklist](manual-verification-checklist.md). See [machine-readable summary](session10-ci-summary.json).

## Acceptance status

| Area | Evidence established / remaining |
|---|---|
| A03/A05/A12–A16 integrated behavior | Real PostgreSQL lifecycle, identity/defaults/edits/eligibility/returns/history readback automated; complete rendered staff walkthrough still needed |
| A06 | Real all-item success, controlled after-write rollback and competing-request integrity automated; UI demonstration still needed |
| A07–A09 | Real related histories and controlled SQL search/filter/sort/reset automated; headless interaction checks added; final visual review pending |
| A17 | Model comparison, real clean migrations/history, SQL constraints and generated identities automated |
| A18 | Known failure, reconnect/recovery, typed unknown outcome and no replay covered at service level; no real commit-acknowledgement loss demo |
| Startup | Repeat-safe bootstrap/seed behavior verified in disposable databases; local development database not overwritten/migrated by this session |
| Restart | Fresh service/context verification plus separate-process probe added; actual Desktop close/reopen remains a manual check |
| A19 | Reproducible CI and retained evidence added; final diagram/catalog/adapted suite/submission package remain Session 11 |

Local Windows Desktop execution remains restricted. A passing Linux/headless run does not mean local policy was repaired. Do not mark all acceptance checkboxes complete from CI alone.

## Git timeline

- `64d6f4a`: Add GitHub Actions verification for full Avalonia build and PostgreSQL tests.
- `d8bb810`: Add integrated PostgreSQL lifecycle, concurrency, and recovery verification.
- `2da327d`: Add headless Avalonia verification for workflow views and asynchronous bindings.
- `9109b66`: Fix Avalonia build warnings found by full CI verification.
- `9399d10`: Fix headless test application namespace resolution.
- `8ad740b`: Verify borrowing persistence across separate service processes in CI.
- `3de02f3`: Align headless test host with Avalonia 12 xUnit v3 requirements.
- `1e5328d`: Correct headless control targeting and stale-selection test setup.

All parts were committed/pushed separately with progress and validation bodies. Next: Session 11 documentation, diagram/catalog/fourteen-case suite, final traceability and demonstration handoff.
