# Lab 4 acceptance and evidence map

The consolidated, current matrix is [final-traceability.md](final-traceability.md). It maps all fourteen required use cases plus A17-A19 to source, exact automated evidence and remaining manual demonstrations.

Verified code baseline: `1e5328dd14e0774ea789c002b3c80cabc1d40f2e`; [successful CI run](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43558553901). Complete Release build: zero warnings/errors; 76 backend tests plus 3 headless UI tests passed, zero failed/skipped; all five separate-process persistence stages passed.

Manual visual/usability and actual Avalonia Desktop close/reopen checks remain pending. The process probe uses real services/database calls across separate processes, but is not the Desktop executable. Do not mark every requirement complete from a passing CI badge.

## Current requirement artifacts

- [Fourteen-case suite](use-case-suite.md): all eighteen supplied fields, original branches/rules/outcomes and implementation notes.
- [Catalog](use-case-catalog.md): all 33 supplied IDs classified as required, deferred or future scope.
- [Diagram](use-case-diagram.md) and [PlantUML](use-case-diagram.puml): fourteen actor goals and system boundary.
- [Business rules](business-rules.md): BR-01 through BR-19.
- [Architecture and schema](architecture-and-schema.md).
- [Final traceability](final-traceability.md), [Session 10 evidence](session10-evidence.md), [manual checklist](manual-verification-checklist.md) and [demonstration script](demonstration-script.md).
- [Submission handoff](submission-handoff.md) and [final development report](development-report-lab4.md).

The earlier incremental planning/status matrix is retained in [archive](archive/acceptance-matrix-through-session10.md), with session-specific blocked runs preserved as historical evidence.
