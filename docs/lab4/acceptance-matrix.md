# Lab 4 acceptance map

Status at Session 1: planned unless stated otherwise. Existing source is not proof of PostgreSQL acceptance. Based on the supplied Lab 4 handout and Use Case Suite, BR-01–19.

Primary actor: Laboratory Staff / Equipment Custodian. All database access belongs behind Application abstractions in Infrastructure; Views/ViewModels contain no SQL, connection setup or migrations.

| Case / evidence | Behavior and rules | Planned implementation | Session | Verification |
|---|---|---|---|---|
| UC-01 / A01 | Catalog identifiers, names, descriptions, availability; BR-05–07,10,18–19 | Catalog query service, EF projection, EquipmentViewModel/view | 4 | Compare fresh DB read; empty and connection-error states |
| UC-02 / A02 | Available items only; BR-10,18–19 | Catalog availability query/filter | 4 | Available/unavailable membership, no matches, load failure |
| UC-03 / A03 | Single checkout; BR-08–11,14–16,18–19 | BorrowEquipmentService, transactional repositories | 4 | Valid issue; missing/ineligible/limit/unavailable/date rejection; restart |
| UC-04 / A04 | Active records with student/equipment and dates; BR-14,18–19 | Borrowing query service, BorrowingsViewModel/view | 4 | Related identities correct; exclude returned; empty/error |
| UC-05 / A05 | Return same record with actual date; BR-10,13–16,18–19 | ReturnEquipmentService, atomic persistence | 4 | Repeat/missing/stale rejected; history retained; restart readback |
| UC-06 / A06 | At least two distinct items, all-or-none; BR-08–12,14–16,18–19 | Coordinated checkout service, shared transaction/context, multi-select UI | 7 | Success, exact limit, each rejection, concurrent conflicts, failure after attempted write and fresh rollback reads |
| UC-07 / A07 | Selected student's full history; BR-14,18–19 | Async student history query and master-detail ViewModel/view | 8 | Two distinguishable students plus no history; missing/error; rapid selection; responsive loading |
| UC-08 / A08 | Selected equipment's full history; BR-14,18–19 | Async equipment history query and master-detail ViewModel/view | 8 | Two distinguishable items plus no history; missing/error; rapid selection; responsive loading |
| UC-09 / A09 | Search, status, stable date sorting; BR-17–19 | Borrowing search request/query service and retrieval controls | 9 | Each search field/status/date/direction, ID tie-break, combined/reset/no matches/error |
| UC-12 / A12 | Student registration/default eligibility; BR-01–02,16,18–19 | Registration service, student repository, management form | 5 | Valid/restart; blank name/number; case/whitespace duplicate; DB uniqueness |
| UC-13 / A13 | Locate/edit student, preserve identity/history; BR-01–03,14,16,18–19 | Student update service and selection/edit UI | 5 | Edit/restart; clear contact; invalid/duplicate edit; linked rows unchanged |
| UC-14 / A14 | Permit/restrict eligibility; BR-04,08,13–14,18–19 | Eligibility service and commands | 5 | Block new loan while restricted; allow return; repeat-state feedback; permit/reborrow |
| UC-15 / A15 | Equipment registration/default availability; BR-05–06,10,16,18–19 | Equipment registration service/repository/form | 6 | Valid/restart; blanks/normalized duplicate; no active loan initially |
| UC-16 / A16 | Locate/edit descriptive fields only; BR-05–07,10,14,16,18–19 | Equipment update service and edit UI | 6 | Clear description; invalid/duplicate edit; protected availability/status/dates; history/restart |
| Cross-cutting / A17 | PostgreSQL constraints; BR-01,05,10,14,16 | Npgsql mappings, migrations, normalized unique keys, partial unique active-item index | 3,10 | Clean DB migration/history; reject invalid FK, duplicate identifiers, duplicate active item |
| Cross-cutting / A18 | Failure/recovery, context lifetime, atomicity; BR-15,18–19 | Per-operation scope, transaction orchestration, typed outcomes | 3–4,7,10 | Known failure no partial change; unknown commit no auto-retry/false rollback; reconnect/refresh; source walkthrough |
| Cross-cutting / A19 | Reproducible submission and traceability | README, diagram/catalog/14-case suite, matrix and evidence | 1,11 | Follow clean setup, compare docs/source/evidence, inspect Git history |

## Evidence template

For each scenario record: ID, setup, action, expected result, observed result, date/toolchain, and evidence path. Use fresh database contexts/connections for readback. Never mark a scenario passed from source inspection alone. Screenshots alone do not prove persistence/atomicity. Deliberate failures use disposable databases.

## Scope exclusions

UC-10–11 and UC-17–27: deferred to Lab 5. UC-28–33: future scope. No authentication, hard deletes, maintenance/retirement, exports, cloud deployment, parallel SQLite support or historical SQLite import required in Lab 4.
