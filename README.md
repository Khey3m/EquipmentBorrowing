# Campus Equipment Borrowing System - Lab 5

Lab 6 continues this implementation on `lab6/automated-verification`: [two-week session plan](docs/lab6/session-plan.md), [progress](docs/lab6/progress.md), [baseline and assertion audit](docs/lab6/baseline.md), [traceability](docs/lab6/traceability.md), and [execution/defect records](docs/lab6/test-execution.md). Sessions 1-8 are complete. The latest API run passed 29 tests, including all 22 Lab 6 objectives.

Continues the Lab 4 Avalonia application with an independently runnable ASP.NET Core API and PostgreSQL. All sixteen API contracts are implemented. As of Session 8, all seven Desktop screens use asynchronous HTTP; Desktop has no Infrastructure, EF Core, Npgsql or direct database dependency.

```text
Avalonia -> HTTP client/contracts -> ASP.NET Core API -> Application -> EF/Npgsql -> PostgreSQL
```

Current verification: full Release build with zero warnings/errors; 76 backend and 23 UI/client tests pass. Actual HTTP composition and native catalog startup work without database settings in Desktop. T01-T06 have passing API executions. T08 controlled recovery passed with native feedback and same-client Retry. Full rendered staff walkthrough remains pending; this is not the final submission tag.

## Lab 6 automated test setup

Use a local PostgreSQL role permitted to create/drop disposable databases and apply migrations. Enter its connection string privately in the test terminal; the database field is replaced with `postgres` for administration and a generated name for each scenario. The fixture never resets the database named in the supplied connection string.

```powershell
$lab6AdminSecret = Read-Host 'Private PostgreSQL test admin connection string' -AsSecureString
$env:POSTGRES_INTEGRATION_ADMIN_CONNECTION = [System.Net.NetworkCredential]::new('', $lab6AdminSecret).Password
dotnet restore EquipmentBorrowing.slnx
dotnet build EquipmentBorrowing.slnx --configuration Release --no-restore
dotnet test tests/EquipmentBorrowing.ApiTests/EquipmentBorrowing.ApiTests.csproj --configuration Release --no-build --logger 'trx;LogFileName=api-foundation.trx' --results-directory TestResults/lab6/local
```

Connection shape: `Host=localhost;Port=5432;Database=postgres;Username=<local-test-role>;Password=<private-password>`. If the existing private API connection uses a role with these permissions, the test terminal may instead set `$env:POSTGRES_INTEGRATION_ADMIN_CONNECTION = $env:EQUIPMENT_BORROWING_CONNECTION_STRING`. No private values belong in source files or screenshots.

`EquipmentBorrowing.ApiTests` starts the real ASP.NET Core middleware/routing/JSON pipeline through `WebApplicationFactory<Program>` and TestServer. It uses real PostgreSQL, not an HTTP response fake or an in-memory database. TestServer runs in the test process: this foundation does not prove TCP transport behavior or actual API process restart. No separately running API/Desktop is needed for these tests.

Each `ApiScenario` owns a generated `equipment_borrowing_lab6_<32-hex-GUID>` database, applies existing migrations without demo bootstrap data, and seeds S1-S4/E1-E7 at fixed local campus D = **2026-11-18**. Only the test host substitutes `ICampusClock` and its database factory; it does not change production settings or the machine clock. IDs are read through the API. Reset/reseed is explicit per scenario/subcase. Physical snapshot connections disable pooling and are independent of HTTP writes.

Disposal closes the scenario's host/client and drops only its own generated database after validating the name; it does not stop PostgreSQL, clear global pools or remove demo/legacy databases. Cleanup failures fail the test and identify the owned database for private inspection. An abruptly terminated test process may leave its generated database behind; inspect ownership before manual cleanup. Existing backend PostgreSQL tests still retain their own historical verification databases.

The private admin setting is mandatory for actual PostgreSQL execution. Without it, API/PostgreSQL cases explicitly skip; do not report skips as passes. Run existing regression projects with the same private setting:

```powershell
dotnet test tests/EquipmentBorrowing.Tests/EquipmentBorrowing.Tests.csproj --configuration Release --no-build --logger 'trx;LogFileName=backend.trx' --results-directory TestResults/lab6/local
dotnet test tests/EquipmentBorrowing.UiTests/EquipmentBorrowing.UiTests.csproj --configuration Release --no-build --logger 'trx;LogFileName=ui-client.trx' --results-directory TestResults/lab6/local
```

Observed results and compact raw TRX archives for Sessions 1-8 are linked from [the execution record](docs/lab6/test-execution.md). The API test command now executes 29 tests, including all 22 Lab 6 objectives. CI runs the Lab 6 suite on branch pushes and rejects skipped API tests. The E08 process harness uses the production campus clock for live writes and the fixed fixture date for seeded history.

## Application prerequisites

- .NET 10 SDK; dotnet-ef 10.0.12 is pinned in dotnet-tools.json.
- PostgreSQL server/client tools (18 is the verified baseline). API's local role needs permissions for migrations; isolated verification additionally needs CREATE DATABASE.
- Git. Optional Python 3 and psql for the supplied repeatable real-HTTP verification harness.
- No Docker, cloud deployment, authentication service or paid service is required.

## Restore and build

From the repository root:

```powershell
dotnet tool restore
dotnet restore EquipmentBorrowing.slnx
dotnet build EquipmentBorrowing.slnx --configuration Release --no-restore
```

## API - first terminal

Create a dedicated disposable Lab 5 database using your local PostgreSQL tools if needed. Keep existing databases intact. Supply a private connection string in the API terminal; the following prompt does not echo it:

```powershell
$apiConnectionSecret = Read-Host 'Local PostgreSQL connection string for the API' -AsSecureString
$env:EQUIPMENT_BORROWING_CONNECTION_STRING = [System.Net.NetworkCredential]::new('', $apiConnectionSecret).Password
```

Connection shape (replace placeholders privately):

```text
Host=localhost;Port=5432;Database=<lab5-database>;Username=<local-role>;Password=<local-password>
```

Apply the preserved migrations and start the server:

```powershell
dotnet ef database update --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Infrastructure --configuration Release --no-build
dotnet run --project src/EquipmentBorrowing.Api --configuration Release --no-build
```

Default API origin: http://127.0.0.1:5085/. Override with ASPNETCORE_URLS or --urls. Host liveness: /api/v1/status (not a database readiness claim). Machine-readable OpenAPI: /openapi/v1.json. Normal API startup does not migrate or seed; migrations and controlled dataset preparation are explicit. Empty lists in a fresh database are valid; register records through Desktop/API or use the controlled verification setup.

## Desktop - second terminal

From the same repository root:

```powershell
$env:EQUIPMENT_BORROWING_API_BASE_URL = 'http://127.0.0.1:5085/'
dotnet run --project src/EquipmentBorrowing.Desktop --configuration Release --no-build
```

Desktop needs only the API origin. Database credentials belong to the server process. Its origin must be HTTP/HTTPS with no user credentials, query or fragment. Requests are asynchronous with finite timeout, cancellation where applicable and latest-result protection. Known validation/missing/conflict responses are distinct from no response and received safe server 500. Unknown mutation outcomes require fresh state verification and staff review; mutations are not automatically replayed.

Navigation covers catalog/single issue, Active/return with confirmation, coordinated multi-checkout, student and equipment management, histories and search/filter/sort/reset. Server determines calendar dates in Asia/Manila, owns all validation and transactions, and preserves history and generated identities. Management clients use the documented optional revision header to reject stale writes.

## Controlled dataset and verification

The reference dataset is S1-S4 (ST-100/200/300/400) and E1-E7 with the prescribed active/returned links. Use a NEW verification database; do not reset the historical Lab 4 demo. The harness creates/migrates it and resets before each required subcase. It shifts reference dates by their offsets from recorded actual campus date and resolves IDs from API responses.

The real-HTTP harness requires private PGHOST, PGPORT, PGUSER, PGPASSWORD and LAB5_STUDENT_VERIFY_CONNECTION targeting the matching NEW name. It uses PostgreSQL 18 psql at the standard Windows path or PSQL_PATH override. See [reproduction details](docs/lab5/session2-evidence.md). Available scenarios: students, equipment, borrowings, reads, checkout, client, client-borrowing, client-all, recovery. The final scenario also checks the native Windows Desktop and captures its window.

```powershell
python tools/verify-api-foundation.py
python tools/verify-student-api.py --resource client-all --database equipment_borrowing_verify_lab5_unique_name --output docs/lab5/new-client-evidence.json
```

Set the private verification connection to that exact new name before invoking the second command. The script refuses an existing database on creation and never silently replaces one. Injected after-insert failure exists only as a temporary verification-DB trigger; no production API fault switch exists.

```powershell
# Load a private admin connection for disposable PostgreSQL integration tests.
$env:POSTGRES_INTEGRATION_ADMIN_CONNECTION = $env:EQUIPMENT_BORROWING_CONNECTION_STRING
dotnet test tests/EquipmentBorrowing.Tests/EquipmentBorrowing.Tests.csproj --configuration Release --no-build
dotnet test tests/EquipmentBorrowing.UiTests/EquipmentBorrowing.UiTests.csproj --configuration Release --no-build
```

Without the integration setting, backend PostgreSQL cases explicitly skip. Report the actual result rather than claiming those cases passed. Server-error/transport simulations must be limited to disposable environments; keep credentials and private identities out of captures.

## Contract, evidence and continuity

- [Required endpoint catalog](docs/lab5/endpoint-contract.md) and [captured generated OpenAPI](docs/lab5/openapi-current.json).
- [Session plan](docs/lab5/session-plan.md), [progress](docs/lab5/progress.md), [API/use-case traceability](docs/lab5/traceability.md).
- [Consolidated T01-T08 execution](docs/lab5/test-execution.md); [current HTTP-only boundary evidence](docs/lab5/session8-evidence.md).
- [Lab 4 continuity baseline](docs/lab5/baseline.md): authentic prepared package at 51f47ea5c127a09de16dbd95bd21c63197080823; Classroom turn-in not independently inspected.
- [Preserved Lab 4 use-case suite](docs/lab4/use-case-suite.md), [business rules](docs/lab4/business-rules.md), [Lab 4 architecture/schema](docs/lab4/architecture-and-schema.md) and [development report](docs/lab4/development-report-lab4.md).

The Lab 4 submitted checkout retains its original direct-database Desktop and launch scripts. Use that revision to reproduce Lab 4 alone; this branch runs API and Desktop separately. Existing migrations and development history are preserved. Final lab5-submitted tag, final handoff and private pair submission actions follow completed acceptance in Sessions 9-10.

Integrated CI and controlled no-response/500/recovery evidence: [Session 9](docs/lab5/session9-evidence.md). CI retains TRX, OpenAPI and HTTP/SQL captures under campuseq-test-results; Linux omits native Windows capture explicitly. Full rendered T07 staff walkthrough remains pending.

Verified integrated checkpoint: [CI run 43710719990](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43710719990), dc1e541: zero-warning Release build, 99 passing tests, five persistence stages and seven controlled HTTP scenarios. [Durable summary](docs/lab5/session9-ci-summary.json). Native recovery screenshots and exact T07/manual limits are recorded in Session 9 evidence.

## Lab 5 report and review handoff

[Development report](docs/lab5/development-report.md), [architecture](docs/lab5/architecture.md), [submission inventory](docs/lab5/submission-handoff.md), [remaining rendered T07 script](docs/lab5/rendered-walkthrough.md), and [retained raw CI artifact](docs/lab5/evidence/ci-43710719990.zip). The source review package is generated from a clean commit with `python tools/package-lab4.py --lab 5 --output-dir "$env:USERPROFILE/Downloads/Lab5-submission"`. Its manifest states the pending rendered T07 and final-tag status; it is not yet a completed lab5-submitted handoff.
