# Lab 5 API-to-use-case traceability

The fourteen functional specifications remain the [Lab 4 suite](../lab4/use-case-suite.md). All implementation files below are under src/EquipmentBorrowing.Api; MapStudentEndpoints, MapEquipmentEndpoints, MapBorrowingEndpoints and MapQueryAndCheckoutEndpoints register the named API operations and call the existing Application services. Evidence below is actual API/PostgreSQL evidence, not a completed T07 Desktop walkthrough. T07 is partial: Session 6 adds actual HTTP management ViewModel/refresh evidence, while checkout/returns/retrieval and full Desktop dependency removal/walkthrough remain pending; full T08 is pending globally.

| UC | BR | API | Supplied tests | Actual implementation | API evidence |
|---|---|---|---|---|---|
| UC-01 | BR-05-07,10,18-19 | API-01 | T01,T07 | EquipmentEndpoints.cs | [session5-evidence.md](session5-evidence.md) |
| UC-02 | BR-10,18-19 | API-01 | T01,T07 | EquipmentEndpoints.cs | [session5-evidence.md](session5-evidence.md) |
| UC-03 | BR-08-11,14-16,18-19 | API-10,13 | T04,T07 | BorrowingEndpoints.cs | [session4-evidence.md](session4-evidence.md) |
| UC-04 | BR-14,18-19 | API-14 | T01,T06,T07 | QueryAndCheckoutEndpoints.cs | [session5-evidence.md](session5-evidence.md) |
| UC-05 | BR-10,13-16,18-19 | API-12,13 | T04,T07 | BorrowingEndpoints.cs | [session4-evidence.md](session4-evidence.md) |
| UC-06 | BR-08-12,14-16,18-19 | API-11 | T05,T07 | QueryAndCheckoutEndpoints.cs | [session5-evidence.md](session5-evidence.md) |
| UC-07 | BR-14,18-19 | API-02,03,15 | T01,T06,T07 | StudentEndpoints.cs / QueryAndCheckoutEndpoints.cs | [session5-evidence.md](session5-evidence.md) |
| UC-08 | BR-14,18-19 | API-01,07,16 | T01,T06,T07 | EquipmentEndpoints.cs / QueryAndCheckoutEndpoints.cs | [session5-evidence.md](session5-evidence.md) |
| UC-09 | BR-17-19 | API-14 | T06,T07 | QueryAndCheckoutEndpoints.cs | [session5-evidence.md](session5-evidence.md) |
| UC-12 | BR-01-02,16,18-19 | API-04 | T02,T07 | StudentEndpoints.cs | [session4-evidence.md](session4-evidence.md) |
| UC-13 | BR-01-03,14,16,18-19 | API-02,03,05 | T02,T07 | StudentEndpoints.cs | [session4-evidence.md](session4-evidence.md) |
| UC-14 | BR-04,08,13-14,18-19 | API-02,03,06 | T02,T07 | StudentEndpoints.cs | [session4-evidence.md](session4-evidence.md) |
| UC-15 | BR-05-06,10,16,18-19 | API-08 | T03,T07 | EquipmentEndpoints.cs | [session3-evidence.md](session3-evidence.md) |
| UC-16 | BR-05-07,10,14,16,18-19 | API-01,07,09 | T03,T07 | EquipmentEndpoints.cs | [session3-evidence.md](session3-evidence.md) |

Machine-readable contract: [OpenAPI](openapi-current.json). Original/final continuity: [baseline](baseline.md). Consolidated expected/observed, diagnosis and retests: [execution record](test-execution.md). Final revision and lab5-submitted tag remain pending actual completion.

Session 6 client implementation: EquipmentBorrowing.Http/ApiTransport.cs and HttpManagementWorkflows.cs, with IManagementWorkflows.cs and Desktop App/ViewModel DI. Actual HTTP management/refresh and stale-revision evidence: [Session 6](session6-evidence.md). These supplement the API evidence above without marking every T07 row complete.
