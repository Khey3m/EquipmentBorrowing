# Lab 4 progress ledger

## Session 1 — baseline established with an external execution blocker

Date: 2026-11-14. Branch: `lab4/postgresql-workflows`. Prior history retained; origin/main was already current at `2970cdb`. Fetch and fast-forward pull succeeded. Feature branch pushes succeeded.

### Git timeline

- `5532159`: preserved pre-existing SQL examples and agent template without changing their content. SQL remains historical SQLite examples, not PostgreSQL acceptance evidence.
- `a5fd661`: corrected unavailable-equipment test arrangement by first successfully borrowing the item, then attempting it again.
- Documentation checkpoint: this ledger, acceptance matrix, session plan and historical report/review copies. Find the exact commit with `git log --oneline -- docs/lab4`.

Every authored source/documentation change is committed. Generated build output, local credentials and database files do not belong in Git. Use explicit staging and separate coherent commits; push each completed part and verify remote synchronization.

### Baseline checks and observed results

| Check | Observed result |
|---|---|
| `dotnet --info` | SDK 10.0.404 / MSBuild 18.9.15, Windows x64; SDK 10.0.400 also installed; no global.json |
| `dotnet build EquipmentBorrowing.slnx --nologo` | Non-Desktop projects compiled; Desktop failed MSB4061 loading Avalonia GenerateAvaloniaResourcesTask |
| SDK 10.0.400 MSBuild comparison | Same Desktop MSB4061 failure; switching SDK did not resolve it |
| `dotnet test tests/EquipmentBorrowing.Tests/EquipmentBorrowing.Tests.csproj --nologo --no-restore` after fixture fix | Test assembly compiled; discovery/execution blocked by Application Control 0x800711C7; no passing test count established |
| Code Integrity Operational log, event 3077 | Explicitly reports Avalonia.Build.Tasks.dll blocked by signing/code-integrity policy; also reports test DLL blocked. This establishes an environment cause, not a proven project-code defect |
| PostgreSQL Windows service | `postgresql-x64-18` Running |
| PostgreSQL client | `C:/Program Files/PostgreSQL/18/bin/psql.exe`, version 18.6; bin directory is not currently on PATH |
| `pg_isready -h localhost -p 5432` | Accepting connections; readiness does not establish authenticated database access |
| Applicable AGENTS.md search | No applicable files found in project or checked ancestor paths |

### External requirements and limits

The managed Windows policy needs an administrator-approved development allowance for the blocked build/test assemblies, or verification must run on an approved development machine/runner. Security settings were not modified. Do not substitute a successful compile for runtime or persistence evidence.

PostgreSQL database name, role and authenticated access remain unverified. Session 3 will use a dedicated local development database and secret-free configuration. Proposed environment variable: `EQUIPMENT_BORROWING_CONNECTION_STRING`; it is a configuration contract to implement in Session 3, not a currently supported setting. Example shape only: `Host=localhost;Port=5432;Database=equipment_borrowing_lab4;Username=<local-role>;Password=<local-secret>`. Set credentials locally, never in tracked files or chat logs. No database was created or modified in Session 1.

Historical report/review files under this directory describe the earlier implementation and review state; later session results belong in this ledger and evidence, rather than silently rewriting historical claims.

### Next session

Session 2: model identifiers/contact/description, date policy and actual return date, fixed three-active-item rule, reliable identity strategy and application contracts. Explain the part before editing; fetch/pull, implement, check, commit and push. Full execution validation remains pending until the policy blocker is resolved.

## Session status

1. Baseline/traceability: done with documented external execution blocker.
2. Domain/contracts: pending.
3. PostgreSQL/schema/lifetimes: pending.
4. Existing workflows: pending.
5. Student management: pending.
6. Equipment management: pending.
7. Atomic multi-item checkout: pending.
8. Histories: pending.
9. Search/filter/sort: pending.
10. Integrated verification: pending.
11. Submission documentation: pending.
