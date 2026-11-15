# Session 6 - asynchronous HTTP management and catalog

Date: 2026-11-15. Management/client foundation cfb379a; verification tool c347792; stale-write race guard 96fed32. This is the first stage of Desktop HTTP migration, not a completed HTTP-only Desktop.

## Delivered boundary

StudentsViewModel uses IStudentManagementWorkflow -> HttpStudentManagement; EquipmentManagementViewModel uses IEquipmentManagementWorkflow -> HttpEquipmentManagement; EquipmentViewModel catalog/student reads use ICatalogReader -> HttpCatalogReader. Shared UI binding models remain Domain presentation snapshots; persistence/business coordination remains server-side for these operations. Equipment management locally filters the catalog because API-01 has no search parameter; student search uses API-02.

The Http project references Contracts and Application/Domain only, not Infrastructure/EF/Npgsql. Long-lived HttpClient has a configurable origin, finite timeout and disabled automatic redirects; no automatic mutation retry policy is installed. Complete updates send explicit null to clear optional fields. Server response fields are required/validated before presenting success.

Startup no longer migrates/seeds through the Desktop initializer; it loads the API catalog and shows actual safe load feedback. However, the Desktop composition still registers direct persistence for unmigrated workflows and constructing those dependencies still requires its local DB setting. Single issue in the catalog, active loans/return, multi-checkout, histories and search remain direct until Sessions 7-8. Keep the API and these remaining Desktop paths on the SAME disposable database; do not combine the old Lab 4 demo launcher with an API pointed elsewhere. T07 is therefore partial, not passed.

## Observed checks

- Full solution Release build: zero warnings/errors after correcting five new test cancellation-token analyzer warnings.
- 76 backend tests pass with PostgreSQL enabled, zero failures/skips.
- Eleven UI/client cases pass, zero failures/skips: four existing headless cases plus seven focused transport/ViewModel cases. Slow reads return an unfinished Task with busy state; unknown writes are blocked until fresh read; no replay occurs; no-response differs from received 500; malformed successes never report success; known stale selections stop before PUT.
- Real HTTP management probe: 27 exchanges, including registration/edit/contact clearing, eligibility, equipment registration/edit/description clearing, catalog filtering, independent HTTP changes and ViewModel refresh.
- 3 deliberately stale revision requests returned 409 for student edit, eligibility and equipment edit; independently changed names/eligibility remained intact.
- Probe process had no PostgreSQL connection setting or PG credentials. It constructed the actual management ViewModels with HTTP adapters, not the full Desktop composition. This is real ViewModel/network evidence, not a manually observed rendered UI walkthrough.
- Fresh psql snapshots before/after the full probe confirm one added student/item, final edited values and unchanged existing borrowing history; these are grouped state checks, not per-probe-mutation SQL snapshots. Earlier API acceptance captures retain per-request snapshots.
- 4 additional outer HTTP checks verify fresh read, API restart, safe database 500 and recovery. Total real HTTP exchanges in this capture: 31.
- Disposable database: `equipment_borrowing_verify_lab5_1115_181348`; actual campus date 2026-11-15. No existing Lab 4/development data or saved user setting changed.
- OpenAPI remains retrievable with all sixteen business operations and now documents the optional revision extension and required response identity/state fields.

Actual evidence: [client wire trace and grouped saved state](session6-client-evidence.json), [current OpenAPI](openapi-current.json). The harness stops its API and probe processes afterward; no API was left running by this verification.

## Failure behavior and concurrency correction

No HTTP response during a read -> safe connection failure. A received safe 500 -> distinct server/database message. Lost/malformed mutation response or explicit outcome_unknown -> unknown, verification required, no automatic replay. Received validation/missing/conflict statuses are handled separately. Error bodies/exception detail are not echoed for server failures.

Design review found that a pre-update GET alone leaves a gap before the API fetches its own snapshot. Added optional X-Expected-Revision to API-05/06/09: Desktop hashes selected response fields, server compares against its fetched state, then existing serializable services revalidate that state before writing. The required editable JSON contract and callers omitting the header remain unchanged. Real stale-header requests passed rejection/no-overwrite checks. This is an optional concurrency extension, not authentication or an entity ID edit.

Initial real probe passed before this guard; after correction it was rerun on another fresh database with the three stale cases and passed. Earlier five cancellation warnings were fixed by using the xUnit test cancellation token; final build is clean. No lost-commit-acknowledgment experiment is claimed.

## Local run configuration at this intermediate checkpoint

Start API and Desktop in separate terminals at the repository root. Set EQUIPMENT_BORROWING_CONNECTION_STRING privately in BOTH terminals to the same existing, disposable, migrated Lab 5 database. Do not paste real passwords into source/recordings. Use the existing saved user connection only if its target is the intended shared database; to use a demo/verification DB, change Database in a local DbConnectionStringBuilder without modifying the saved user setting.

API terminal:

```powershell
dotnet ef database update --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Infrastructure --configuration Release
dotnet run --project src/EquipmentBorrowing.Api --configuration Release --no-build
```

Desktop terminal (same DB setting during this transitional stage):

```powershell
$env:EQUIPMENT_BORROWING_API_BASE_URL = 'http://127.0.0.1:5085/'
dotnet run --project src/EquipmentBorrowing.Desktop --configuration Release --no-build
```

The base URL must be an HTTP/HTTPS origin without user credentials/query/fragment. Default is http://127.0.0.1:5085/. Invalid configuration produces safe feedback from the HTTP client. API migrations/dataset preparation are explicit; merely starting API does not seed. For repeatable acceptance setup, use the private PG variables/matching new connection and tools/verify-student-api.py as in earlier evidence; --resource client runs this session's fresh isolated probe.

## Remaining

Session 7 migrates single/multi checkout and active returns; Session 8 migrates retrieval and removes remaining direct persistence references. Full T07/T08 and the visible staff click-through remain pending. Existing headless passes do not establish full HTTP integration or visual acceptance.
