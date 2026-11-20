# Lab 6 review handoff

All 22 automated objectives have passing recorded executions. MA6-01 and MA6-02 passed in the supervised native Windows walkthrough. The annotated lab6-submitted tag identifies the final package revision.

## Delivered inventory

| Requirement | Artifact |
|---|---|
| Continued solution and working xUnit tests | EquipmentBorrowing.slnx and tests/EquipmentBorrowing.ApiTests |
| Isolated PostgreSQL fixture, fixed date, cleanup | ApiScenario.cs and README Lab 6 setup |
| Fourteen use cases and sixteen API operations | traceability.md |
| Actual executions, diagnosis, corrections and retests | test-execution.md and evidence/session1–8-trx.zip |
| Real API restart and fresh connections | PersistenceApiTests.cs, Session 8 raw TRX |
| Final full local regression | 29 API, 76 backend and 23 UI/client passes in evidence/final-regression-trx.zip |
| Successful hosted verification | GitHub Actions run 44644519235 at 43d0fc7 |
| Manual acceptance preparation | tools/verify-lab6-desktop.py and Session 9 instructions |
| Authentic baseline and history qualifications | baseline.md |
| Source ZIP with revision, statuses, checksum and history | tools/package-lab4.py --lab 6 |

The HTTP test host uses fixed local D = 2026-11-18. The actual API subprocess uses the production Manila calendar, with its dates recorded in the E08 output. Missing PostgreSQL configuration causes explicit skips locally. CI requires every API test to pass and verifies that the restart test executed.

The tests use real PostgreSQL constraints and fresh connections for database claims. Client handlers simulate unavailable transport and lost acknowledgements. The Session 7 archive retains the failed test-handler attempt and successful retest. The Session 9 rehearsal records API, SQL and dependency checks with blank UI observation fields.

## Create the final package

Run from a clean committed checkout after restoring tools and following the root README:

```powershell
python tools/package-lab4.py --lab 6 --output-dir "$env:USERPROFILE/Downloads/Lab6-final"
```

The ZIP exports the committed source, includes execution evidence, and embeds submission/revision.json and submission/git-history.txt. The adjacent manifest records the exact commit, all objective statuses, ZIP checksum, file count and submission-tag status. Select a new output folder if that revision already has a package.

## Submission procedure

1. Review the retained supervised observations and API/SQL findings in evidence/session9-manual-observations.json. The outage screenshot was provided in conversation.
2. Verify the final regression results linked from test-execution.md.
3. Resolve lab6-submitted and confirm the package manifest records that commit.
4. Verify the downloaded ZIP checksum against the generated manifest.
5. Each student submits individually, with required identities in private submission fields.

The final local regression is retained with the handoff. The hosted Session 8 result remains linked at its tested revision. Final acceptance uses the supervised observations and fresh local regression output.
