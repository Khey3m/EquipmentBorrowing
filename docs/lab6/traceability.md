# Lab 6 requirements traceability

Matrix transcribed from the companion suite. U/E abbreviate TC6-U/TC6-E. Session 2 completed U01/U02/U04/U07/U08 in `tests/EquipmentBorrowing.ApiTests/ReadApiTests.cs`; method names below are relative to that class. Other scenario results remain Not Run even when sharing a UC/API row. Session 1 foundation checks are separate supporting infrastructure evidence. See [execution record](test-execution.md), [baseline assertion audit](baseline.md#existing-assertion-audit-and-remaining-work) and [session plan](session-plan.md).

| UC | BR | API | TC6 | Actual test implementation | Execution result | Evidence |
|---|---|---|---|---|---|---|
| UC-01 | BR-05-07,10,19 | API-01 | U01,E07 | `U01_CatalogReturnsExactFixtureAndEmptyArray`; E07 pending | U01 Passed; E07 Not Run | [Session 2](evidence/session2-summary.json) |
| UC-02 | BR-10,19 | API-01 | U02 | `U02_AvailableFilterIncludesOnlyAvailableItemsAndHandlesNone` | Passed | [Session 2](evidence/session2-summary.json) |
| UC-03 | BR-08-11,14-16,19 | API-10,13 | U03,E02,E03,E05,E07,E08 | Pending | Not Run | Pending |
| UC-04 | BR-14,19 | API-14 | U04,U09 | `U04_ActiveLoansHaveExactIdsAndRelatedRecordsWithoutReturnedLoan`; U09 pending | U04 Passed; U09 Not Run | [Session 2](evidence/session2-summary.json) |
| UC-05 | BR-10,13-14,19 | API-12,13 | U05,U14,E08 | Pending | Not Run | Pending |
| UC-06 | BR-08-12,14-16,19 | API-11 | U06,E02-E05,E07 | Pending | Not Run | Pending |
| UC-07 | BR-14,19 | API-03,15 | U07 | `U07_StudentHistoryDistinguishesMixedEmptyAndMissingSubjects` | Passed | [Session 2](evidence/session2-summary.json) |
| UC-08 | BR-14,19 | API-07,16 | U08,U16 | `U08_EquipmentHistoryDistinguishesActiveReturnedEmptyAndMissingSubjects`; U16 pending | U08 Passed; U16 Not Run | [Session 2](evidence/session2-summary.json) |
| UC-09 | BR-17,19 | API-14 | U09,E03 | Pending | Not Run | Pending |
| UC-12 | BR-01-02,16,19 | API-03,04 | U12,E01,E08 | Pending | Not Run | Pending |
| UC-13 | BR-01-03,14,16,19 | API-02,03,05,15 | U13,E01,E06 | Pending | Not Run | Pending |
| UC-14 | BR-04,08,13-14,19 | API-06,10,12 | U14 | Pending | Not Run | Pending |
| UC-15 | BR-05-06,10,16,19 | API-07,08 | U15,E01,E08 | Pending | Not Run | Pending |
| UC-16 | BR-05-07,10,14,16,19 | API-07,09,16 | U16,E01,E06 | Pending | Not Run | Pending |

Each API needs identifiable assertions through the real HTTP pipeline. Direct endpoint/service calls and mocked HTTP responses cannot populate the actual HTTP-test column as route verification.

| API contract | Required automated scenario coverage | Actual HTTP test | Result | Evidence |
|---|---|---|---|---|
| API-01 | U01,U02 | `ReadApiTests.U01_CatalogReturnsExactFixtureAndEmptyArray`, `U02_AvailableFilterIncludesOnlyAvailableItemsAndHandlesNone` | Passed for U01/U02 | [Session 2](evidence/session2-summary.json) |
| API-02 | U13 | Pending | Not Run | Pending |
| API-03 | U07,U12,U13 | `ReadApiTests.U07_StudentHistoryDistinguishesMixedEmptyAndMissingSubjects` | U07 Passed; U12/U13 pending | [Session 2](evidence/session2-summary.json) |
| API-04 | U12,E01 | Pending | Not Run | Pending |
| API-05 | U13,E01,E06 | Pending | Not Run | Pending |
| API-06 | U14 | Pending | Not Run | Pending |
| API-07 | U08,U15,U16 | `ReadApiTests.U08_EquipmentHistoryDistinguishesActiveReturnedEmptyAndMissingSubjects` | U08 Passed; U15/U16 pending | [Session 2](evidence/session2-summary.json) |
| API-08 | U15,E01 | Pending | Not Run | Pending |
| API-09 | U16,E01,E06 | Pending | Not Run | Pending |
| API-10 | U03,U14,E02,E03,E05,E07,E08 | Pending | Not Run | Pending |
| API-11 | U06,E02-E05,E07 | Pending | Not Run | Pending |
| API-12 | U05,U14,E08 | Pending | Not Run | Pending |
| API-13 | U03,U05,E08 | Pending | Not Run | Pending |
| API-14 | U04,U09,E03 | `ReadApiTests.U04_ActiveLoansHaveExactIdsAndRelatedRecordsWithoutReturnedLoan` | U04 Passed; U09/E03 pending | [Session 2](evidence/session2-summary.json) |
| API-15 | U07,U13 | `ReadApiTests.U07_StudentHistoryDistinguishesMixedEmptyAndMissingSubjects` | U07 Passed; U13 pending | [Session 2](evidence/session2-summary.json) |
| API-16 | U08,U16 | `ReadApiTests.U08_EquipmentHistoryDistinguishesActiveReturnedEmptyAndMissingSubjects` | U08 Passed; U16 pending | [Session 2](evidence/session2-summary.json) |

Supplementary live OpenAPI consistency assertions: pending Session 5. MA6-01/02 observations: pending Session 9. TC6-E08 automated fresh-connection results and actual API-restart method/evidence must be identified separately in the execution record.
