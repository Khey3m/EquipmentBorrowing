# Lab 5 submission handoff - review draft

## Delivered inventory

| Handout requirement | Actual artifact |
|---|---|
| Continued source, API and Avalonia client | src/, EquipmentBorrowing.slnx; all sixteen endpoints and HTTP-only Desktop |
| Preserved reproducible migrations | Infrastructure/Persistence/Migrations; no Lab 5 schema change needed |
| Independent API/Desktop setup, private configuration and controlled dataset | README.md and tools/verify-student-api.py scenarios |
| Endpoint catalog and machine-readable contract | endpoint-contract.md, openapi-current.json, live /openapi/v1.json |
| Authentic Lab 4 continuity record | baseline.md and preserved Git history |
| Boundary, DI, context/transaction explanation | architecture.md |
| Fourteen-use-case API traceability | traceability.md, linked preserved Lab 4 suite |
| Consolidated execution/diagnostics/retests | test-execution.md, session evidence and raw captures |
| Durable verified CI evidence | evidence/ci-43710719990.zip and checksum/inventory JSON; session9-ci-summary.json |
| Development report | development-report.md |
| Current revision/history | Generated package manifest and embedded submission/revision.json / submission/git-history.txt |
| Full rendered T07 evidence | Pending; rendered-walkthrough.md is instructions only |
| Final lab5-submitted tag | Pending until required acceptance is actually complete |

Verified code/tools checkpoint dc1e54186f35600bc8cad15ed8ec6897ccdbb167: CI 43710719990 passed 99 tests, five persistence stages and seven HTTP scenarios. Later documentation/package commits preserve that runtime behavior. Package metadata always records the actual checkout HEAD.

## Before turning in

- [ ] Complete the actual rendered T07 sequence and add observed evidence to the consolidated execution record.
- [ ] Record the completed source revision and create/push lab5-submitted on that revision; do not tag an incomplete draft.
- [ ] Regenerate the final ZIP and attach any teacher-required screen recording/evidence.
- [ ] Include both confirmed partners' full names and student IDs privately; each submits individually to Google Classroom.
- [ ] Check instructor-specific format/deadline and teacher access to the repository/recording.

These boxes are pending, not a claim that implementation failed. T01-T06/T08 have passing recorded executions; T07 automated boundary/workflows passed but full rendered staff sequence remains unrecorded. No submission or grade is claimed.

## Review packaging

From a clean committed checkout:

```powershell
python tools/package-lab4.py --lab 5 --output-dir "$env:USERPROFILE/Downloads/Lab5-submission"
```

The shared packaging tool retains its Lab 4 default for older commands; --lab 5 selects Lab 5 inventory/naming/status. It exports committed files, embeds current commit-title history and revision metadata, checks ZIP integrity/required files and OpenAPI operations, and produces SHA-256/size/commit metadata beside the archive. Generated output stays outside the repository. The source ZIP does not contain installed dependencies, local DB contents, credentials or the .git object database; repository access provides complete historical diffs.

While T07/tag remain pending, the manifest explicitly labels the archive Review draft. Keep earlier packages intact. The completed final revision/tag will require a new package.

## Final revision/tag procedure after acceptance

After recording actual T07 results and committing its evidence, check a clean tree and synchronize the branch. Create the required annotated tag only on the completed committed revision, then push the branch and tag and package again. If lab5-submitted already exists, inspect it and resolve provenance rather than moving it silently. No tag was created during this draft handoff.

For Classroom, attach the final source ZIP, repository/commit/tag link and required demonstration evidence. Individual partner submissions may share the same project. Personal identities belong in private submission comments rather than public source docs.
