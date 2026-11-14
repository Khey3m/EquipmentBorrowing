# Lab 4 submission handoff

## Delivered package

The repository contains the continued Labs 1–3 application, PostgreSQL configuration/migrations, fourteen required workflows, automated integration/headless/process verification and the Lab 4 documentation set. All authored source/documentation is tracked in Git; local credentials, database files and generated build output are excluded.

Verified executable code: `1e5328dd14e0774ea789c002b3c80cabc1d40f2e`. [CI run 43558553901](https://github.com/Khey3m/EquipmentBorrowing/actions/runs/43558553901) passed a zero-warning full Release build, 76 backend tests, 3 headless UI tests and five separate-process persistence stages. Session 11 adds documentation without changing the application behavior. Later documentation commits are discoverable in Git; use the submitted checkout's actual HEAD when recording a new demonstration.

## Submission inventory

| Handout requirement | Artifact |
|---|---|
| Updated source and PostgreSQL migrations | src/, tests/, tools/, EquipmentBorrowing.slnx; InitialPostgres and frozen snapshot in Infrastructure/Persistence/Migrations |
| Meaningful Git development history | Full repository history; [concise export](git-history.txt) and [progress ledger](progress.md) |
| Reproducible prerequisites/configuration/migration/seeding/execution | [README](../../README.md), [PostgreSQL setup](postgres-setup.md), [continuous verification](continuous-verification.md) |
| Architecture/context lifetime/transaction explanation | [Architecture and schema](architecture-and-schema.md) |
| Updated use-case diagram/catalog/suite | [Diagram](use-case-diagram.md), [PlantUML](use-case-diagram.puml), [catalog](use-case-catalog.md), [fourteen-case suite](use-case-suite.md) |
| Business rules and traceability | [BR-01–19](business-rules.md), [final traceability](final-traceability.md) |
| Schema, verification and integrated evidence | [Schema SQL](postgres-schema.sql), [Session 10 evidence](session10-evidence.md), [CI summary JSON](session10-ci-summary.json), CI logs/TRX artifact |
| Concise project report | [Lab 4 development report](development-report-lab4.md); earlier session reports remain historical |
| Demonstration and remaining observations | [Demonstration script](demonstration-script.md), [manual checklist](manual-verification-checklist.md) |

## Remaining items before declaring full submission acceptance

- [ ] Perform and record the actual Desktop staff walkthrough, including visible success/rejection/empty/error states and reasonable resizing/keyboard use.
- [ ] Close the entire Avalonia Desktop process, reopen it against the same disposable PostgreSQL database, and record changed student/equipment/eligibility plus active/returned state and fresh readback. CI's five-process probe is separate supporting evidence.
- [ ] Demonstrate any branches listed as remaining in final traceability, including direct catalog/available/active-list comparisons and editing a student that already has borrowing history.
- [ ] Retain or download the test evidence needed for submission. GitHub artifacts follow retention limits; a run URL is not permanent local evidence storage.
- [ ] Add any instructor-required live/recorded demonstration and screenshots. Screenshots alone do not establish persistence, architecture or atomicity.
- [ ] Include each submitter's full name and student ID privately as instructed. For a confirmed pair, both submit individually and identify both partners; a solo submitter identifies themself. These details were not supplied and have not been invented or published in Git.
- [ ] Check the actual submission deadline and course platform instructions. No deadline was assumed from the project files.

These boxes are deliberately unchecked. Source/documentation completion and automated verification do not mean these personal/manual actions were performed.

## Repository or archive submission

Submit the appropriate repository URL/branch/commit, preserving earlier history. If an archive is required, generate it from the committed final checkout so it includes the docs and history export:

```powershell
git status --short
git rev-parse HEAD
git archive --format=zip --output=../EquipmentBorrowing-Lab4.zip HEAD
```

The archive contains tracked files only; .git history is not embedded by git archive, so retain git-history.txt and/or provide repository access. Do not include credential files, local database contents, bin/obj or unrelated personal files. This document provides packaging commands; no submission upload, deployment, merge or final grading claim was made.

## Handoff state

Sessions 1–11 source/documentation work is delivered with explicit automated evidence and a manual checklist. Session 11 document validation checks links, required use-case fields, IDs/classifications and diagram consistency. Full acceptance remains conditional on the unchecked items above.
