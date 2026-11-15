# Disposable demonstration database

Prepared on 2026-11-14: `equipment_borrowing_demo_20261114_160829` on the existing local PostgreSQL server.

A new database was created without replacing the existing development database. EF applied `20261114000100_InitialPostgres` (10.0.12); Desktop startup supplied the standard seed records. A fresh psql connection verified three students, three equipment records and zero borrowings. The Desktop main window opened and responded.

The saved Windows user connection was not modified. Only the launched process receives the demo database connection. Alex/Jordan/Sam and the demonstration equipment have not been entered; create them in the management screens during preparation/Steps 1-2. Explicitly restrict Jordan after registration. Keep a student and EQ-TAB-01 unused to demonstrate zero history.

## Reopen the same demo database

From the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\start-lab4-demo.ps1 -Database equipment_borrowing_demo_20261114_160829
```

This command uses a process-only script execution setting; no persistent execution policy or saved connection is changed. It applies pending migrations and launches the existing Release build. Close the previous Desktop window first so the demonstration uses one clear application instance. The ordinary launch command continues to use the original saved database.

For a future fresh demo, supply a new name beginning with equipment_borrowing_demo_ and add -CreateDatabase. The script refuses to replace an existing database. It expects PostgreSQL 18 psql at the standard Windows installation path and a previously built Release application. Credentials are read privately from the existing Windows user setting and restored in the parent process after launch.

## Walkthrough reminders

Use [final traceability](final-traceability.md) and [the demonstration script](demonstration-script.md). Expected return dates must be after the actual campus date. Test exactly three loans separately from over-limit rejection. Search covers student number/name and equipment name. Do not stop the shared PostgreSQL service for the recovery demonstration. Database preparation and window launch do not mark the full manual walkthrough passed.
