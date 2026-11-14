# Reproduce automated Lab 4 verification

The repository workflow is [.github/workflows/verify-lab4.yml](../../.github/workflows/verify-lab4.yml). It runs on pushes to main/development and pull requests. [Actions history](https://github.com/Khey3m/EquipmentBorrowing/actions/workflows/verify-lab4.yml) retains the commit-specific outcomes and uploaded TRX evidence.

## What runs

1. Restore .NET 10 tools and packages on a Linux runner with an isolated PostgreSQL 18 service.
2. Build the full Release solution, including Desktop, backend tests, headless UI tests and persistence probe.
3. Run all backend tests with the PostgreSQL integration variable enabled. Without that variable, opt-in integration tests skip and do not establish database acceptance.
4. Run Avalonia Headless UI tests against actual controls and bindings with fake read services.
5. Run init/borrow/readback/return/readback in five distinct verification processes against a dedicated generated database.
6. Upload TRX files even when a test fails; inspect failed/cancelled runs as well as passing ones.

The CI password is generated only for its disposable service. It is unrelated to the local PostgreSQL credentials. The workflow has read-only repository-content permissions and no deployment step.

## Run locally on an execution-permitted development environment

```powershell
dotnet tool restore
dotnet restore EquipmentBorrowing.slnx
dotnet build EquipmentBorrowing.slnx --configuration Release --no-restore
$env:POSTGRES_INTEGRATION_ADMIN_CONNECTION = [Environment]::GetEnvironmentVariable('EQUIPMENT_BORROWING_CONNECTION_STRING', 'User')
dotnet test tests/EquipmentBorrowing.Tests/EquipmentBorrowing.Tests.csproj --configuration Release --no-build
dotnet test tests/EquipmentBorrowing.UiTests/EquipmentBorrowing.UiTests.csproj --configuration Release --no-build
```

Configure the integration connection privately on other machines. Its role must be allowed to create uniquely named disposable databases. Integration data is separate from the operational development database and is retained locally for inspection; CI service databases disappear with the service container.

The separate-process probe requires EQUIPMENT_BORROWING_CONNECTION_STRING to target a new `equipment_borrowing_verify_process_<safe_suffix>` database and an authorized role. Init intentionally fails if the database already exists, rather than overwriting it. It rejects operational database names. Run the five stages as separate dotnet invocations as shown in the workflow; a new context within one process is not the same evidence.

## Limits

Local Windows policy may still block unsigned assemblies or Avalonia build tasks. A remote pass does not alter or repair that policy. Use approved development configuration for local execution; do not disable controls merely to match CI.

Headless tests use in-memory windows/control trees, not a real OS display or a pixel-quality review. The persistence probe uses real services/database calls across process boundaries, not the actual Desktop executable. Complete the [manual walkthrough](manual-verification-checklist.md) before claiming full rendered/restart acceptance.

See [Session 10 evidence](session10-evidence.md) for exact run links and observed counts.
