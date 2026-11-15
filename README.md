# Campus Equipment Borrowing System - Lab 4

Continues the Labs 1-3 application with PostgreSQL, student/equipment management, atomic multi-item checkout, related histories and borrowing search/filter/sort. Primary actor: Laboratory Staff / Equipment Custodian. All fourteen required Lab 4 use cases are implemented; deferred Lab 5/future cases are listed separately.

**Verified code:** `1e5328dd14e0774ea789c002b3c80cabc1d40f2e`. [Successful CI](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43558553901): full Release solution builds with zero warnings/errors, **76 backend + 3 headless UI tests pass**, and all five separate-process persistence stages pass. Session 11 changes are documentation-only. [Evidence details](docs/lab4/session10-evidence.md).

**Remaining acceptance:** manual visual/usability review, the actual Avalonia Desktop close/reopen walkthrough, and any instructor-specific recording/private submitter details. Headless windows and the service-process probe are useful evidence with different scopes; they do not establish every manual branch. Local Windows Application Control remains unchanged and may block local build/test assembly loading. [Handoff checklist](docs/lab4/submission-handoff.md).

## Prerequisites

- .NET 10 SDK. The repository uses net10.0 and pins dotnet-ef 10.0.12 in dotnet-tools.json.
- PostgreSQL server and client tools; PostgreSQL 18 is the verified CI baseline (18.6 was used locally). A local database is sufficient; cloud, Docker and paid services are not required for normal use.
- Git and an execution-permitted environment for the Desktop application.
- Optional Python 3 plus psql only for the standalone historical SQL verification script.

Package versions are committed in the project files: Avalonia 12.1.1, CommunityToolkit.Mvvm 8.4.2, EF Core 10.0.12 and Npgsql EF provider 10.0.3. The headless UI project uses xUnit v3; backend tests retain their existing xUnit runner.

## Get the submitted source

```powershell
git clone https://github.com/Khey3m/EquipmentBorrowing.git
cd EquipmentBorrowing
git switch lab4/postgresql-workflows
```

Use the branch/commit supplied with the submission. If development is later merged and its branch deleted, check out the submitted commit or main containing it. Preserve prior history. To inspect the local version:

```powershell
git status --short
git rev-parse HEAD
```

## Configure PostgreSQL locally

Create a dedicated empty database on a fresh installation; do not overwrite existing data. Example with an authorized local role (psql/createdb may require adding PostgreSQL's bin directory to PATH):

```powershell
createdb -h localhost -U <local-role> equipment_borrowing_lab4
```

PostgreSQL tools prompt for the local password when needed. Current local setup already has this database and a stored Windows user setting; skip creation if reusing it. Operational connection-string shape:

```text
Host=localhost;Port=5432;Database=equipment_borrowing_lab4;Username=<local-role>;Password=<local-secret>;Include Error Detail=false
```

Set `EQUIPMENT_BORROWING_CONNECTION_STRING` privately in the launching process or OS user environment. Do not put real values in source, screenshots, submitted logs or committed files. In PowerShell, prompt without echoing the value:

```powershell
$connectionSecret = Read-Host 'Enter the local PostgreSQL connection string' -AsSecureString
$env:EQUIPMENT_BORROWING_CONNECTION_STRING = [System.Net.NetworkCredential]::new('', $connectionSecret).Password
```

For the Windows user configured during development, load the already saved value instead:

```powershell
$env:EQUIPMENT_BORROWING_CONNECTION_STRING = [Environment]::GetEnvironmentVariable('EQUIPMENT_BORROWING_CONNECTION_STRING', 'User')
```

Restart Visual Studio after changing a user environment setting so its processes inherit it. Special connection-string characters must use valid Npgsql connection-string quoting. Database setup/migrations require schema permissions; integration tests additionally require CREATE DATABASE in a disposable verification environment.

## Restore, migrate and launch

Run from the repository root:

```powershell
dotnet tool restore
dotnet restore EquipmentBorrowing.slnx
dotnet ef migrations has-pending-model-changes --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Infrastructure
dotnet ef database update --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Infrastructure
dotnet build EquipmentBorrowing.slnx --configuration Release --no-restore
dotnet run --project src/EquipmentBorrowing.Desktop --configuration Release --no-build
```

Infrastructure is the EF startup project for migration commands, so migration tooling does not need to initialize Avalonia. InitialPostgres is the operational migration; historical SQLite files are archived outside compiled migration discovery. There is no operational SQLite fallback or historical SQLite row-import requirement.

Desktop startup also delegates migrations/seeding to Infrastructure and provides safe Retry startup feedback on failure. Students/equipment are seeded only when each corresponding table is empty; existing rows are not overwritten. Example student identifiers: STU-001 (eligible), STU-002 (restricted), STU-003 (eligible). Example equipment identifiers: EQ-101..103 (initially available). Internal IDs are database-generated; staff should use visible identifiers rather than assume internal IDs.

The local development database was not migrated/seeded during CI verification; CI creates separate disposable databases. Running the commands above locally is a real setup action, not a previously claimed local Desktop pass.

## Staff workflow

| Navigation | Actions |
|---|---|
| Equipment | Catalog, available-only filter, single borrowing with expected return date |
| Active Borrowings | Related active records and confirmed return |
| Students | Register, find/edit descriptive details, permit/restrict new borrowing |
| Manage Equipment | Register/find/edit identifier, name and optional description; availability read-only |
| Checkout multiple | Select at least two distinct items, one student and one expected date |
| Borrowing histories | Student or Equipment mode, active/returned records and all dates |
| Search borrowings | Partial text, Active/Returned/All, either date/direction, Apply/Retry and Clear |

The limit is three active items per student. Expected return is later than checkout; actual return uses the campus calendar and may be early/same-day. Default campus timezone is Asia/Manila. Restricting a student blocks new borrowing but permits existing returns. Descriptive edits preserve internal identities and history. Multi-item writes are coordinated in one transaction. Unknown commit outcomes are not automatically replayed; reconnect/refresh and review before another staff decision.

## Automated verification

```powershell
dotnet test tests/EquipmentBorrowing.Tests/EquipmentBorrowing.Tests.csproj --configuration Release --no-build
dotnet test tests/EquipmentBorrowing.UiTests/EquipmentBorrowing.UiTests.csproj --configuration Release --no-build
```

Without `POSTGRES_INTEGRATION_ADMIN_CONNECTION`, PostgreSQL tests explicitly skip. On an authorized local disposable setup, load a connection privately before running them:

```powershell
$env:POSTGRES_INTEGRATION_ADMIN_CONNECTION = [Environment]::GetEnvironmentVariable('EQUIPMENT_BORROWING_CONNECTION_STRING', 'User')
dotnet test tests/EquipmentBorrowing.Tests/EquipmentBorrowing.Tests.csproj --configuration Release --no-build
```

Integration tests create unique equipment_borrowing_verify_* databases and retain them locally for inspection; they do not delete/reuse the operational database. The service-process probe requires its own verification-only database naming scope. [Exact verification instructions](docs/lab4/continuous-verification.md).

[GitHub Actions](https://github.com/Khey3m/EquipmentBorrowing/actions/workflows/verify-lab4.yml) builds/tests on a Linux runner with isolated PostgreSQL 18 and disposable CI credentials. It has no deployment step. Logs/TRX are available from each run. The successful baseline includes 79 tests with zero skips and five process stages; a skipped local integration run is not equivalent.

Windows Code Integrity may block unsigned assemblies or Avalonia tasks with 0x800711C7/MSB4061. This was observed locally and not addressed by disabling policy. Use an approved development allowance/environment or inspect CI evidence; a remote pass does not repair local policy.

## Architecture and project layout

- Domain: Student, Equipment, Borrowing and validation/state transitions; no Avalonia/EF dependency.
- Application: actor-goal services and repository/query/session contracts.
- Infrastructure: Npgsql/EF mappings, migrations, queries, repositories, bootstrap and serializable transactions.
- Desktop: AXAML views, CommunityToolkit MVVM state/commands and DI composition root.
- Tests: backend/domain/service/real PostgreSQL tests.
- UiTests: Avalonia Headless controls, bindings and asynchronous selection checks.
- tools/EquipmentBorrowing.PersistenceProbe: verification-only service-process persistence harness.
- ConsoleDemo: retained in-memory learning/demo path; it does not establish PostgreSQL atomicity.

IDbContextFactory creates short-lived contexts. A write session shares one context/serializable transaction across validation and repository saves, then commits once. Queries are asynchronous/no-tracking. Database-generated normalized unique keys, foreign keys, date/state checks and a partial active-item index supplement Application rules. [Architecture, ERD and rationale](docs/lab4/architecture-and-schema.md).

## Requirements, evidence and submission

- [Use-case diagram](docs/lab4/use-case-diagram.md) / [PlantUML](docs/lab4/use-case-diagram.puml)
- [33-case scope catalog](docs/lab4/use-case-catalog.md) / [adapted fourteen-case suite](docs/lab4/use-case-suite.md)
- [Business rules BR-01-19](docs/lab4/business-rules.md) / [final traceability](docs/lab4/final-traceability.md)
- [Final development report](docs/lab4/development-report-lab4.md) / [progress ledger](docs/lab4/progress.md) / [Git history export](docs/lab4/git-history.txt)
- [Session 10 evidence](docs/lab4/session10-evidence.md) / [CI summary](docs/lab4/session10-ci-summary.json)
- [Demonstration script](docs/lab4/demonstration-script.md) / [manual checklist](docs/lab4/manual-verification-checklist.md) / [submission handoff](docs/lab4/submission-handoff.md)

UC-10-11 and UC-17-27 are deferred to Lab 5; UC-28-33 are future scope. No authentication, hard deletion, maintenance/retirement, reporting/export or deployment is required in this Lab 4 implementation.

Earlier laboratory reflections and incremental status notes remain in [the archived README](docs/lab4/archive/readme-through-session10.md), [Labs 1-3 report/review](docs/lab4/labs1-3-review.md), and session evidence files. Historical blocked/pass results apply to their own revisions. Submitter/partner names and IDs were not supplied; add them privately in the required submission location rather than inventing or publishing them.

## Lab 5 in progress

Lab 4 reproduction above remains valid for its direct-database Desktop. On branch lab5/api-integration, Sessions 1-2 add an independent ASP.NET Core host, transport contracts and student endpoints API-02-06; Desktop HTTP migration and other business endpoints are pending.

```powershell
dotnet run --project src/EquipmentBorrowing.Api --configuration Release
```

Host status: http://127.0.0.1:5085/api/v1/status. Generated OpenAPI: http://127.0.0.1:5085/openapi/v1.json (currently student endpoints plus operational status). Override the address with --urls. This foundation does not connect, migrate or seed on startup. See [Lab 5 plan](docs/lab5/session-plan.md), [endpoint target](docs/lab5/endpoint-contract.md), [session evidence](docs/lab5/session1-evidence.md), and [acceptance execution](docs/lab5/test-execution.md).

Student endpoint verification and limits: [Session 2 evidence](docs/lab5/session2-evidence.md). Current generated contract snapshot: [OpenAPI](docs/lab5/openapi-current.json). Full T02 includes future issue/return branches and remains partial.
