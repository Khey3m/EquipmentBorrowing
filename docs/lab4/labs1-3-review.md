# Review of the Labs 1–3 Project Completion Report

Reviewed on 2026-11-14.

## Verdict and scope

The report broadly matches the implementation, but it is not fully correct or fully verified. Revise the technical statements below and qualify the completion/testing claims before submission.

Report reviewed: `C:/Users/kheye/Downloads/project_completion_report.md`.

Matching source inspected: `C:/Users/kheye/Documents/3RD YEAR/DESKTOP DEVELOPMENT/FINAL LAB 1/EquipmentBorrowing-Final-Output-Activity-1`.

Several local project copies exist. This review applies to that matching copy. Lab instructions/rubrics and historical Lab 1/2 snapshots were not supplied, so exact rubric compliance and the timing of individual changes cannot be certified.

## Confirmed from source

- Domain entities, borrowing eligibility/quota logic, repository interfaces, borrowing and return services exist.
- Avalonia views, CommunityToolkit.Mvvm ViewModels, and dependency registrations exist.
- EF Core SQLite repositories, DbContext, design-time factory, Fluent API configurations, and an InitialCreate migration exist.
- Borrowing has the mapped-column constructor described in the report.
- Startup calls MigrateAsync and seeds Students and Equipment only when each corresponding table is empty.
- Repository tracking choices match the report's general description.
- The ERD correctly describes the logical one-to-many relationships and entity properties. It is a logical diagram: the migration stores dates/status as TEXT and booleans as INTEGER.

## Report corrections

1. **Section 1 — dependency diagram:** Add Infrastructure → Application because EF repositories implement interfaces declared in Application. The Desktop composition root also references Infrastructure. Domain does not depend on repository interfaces; revise reflection answer 1 to say Application services and relevant ViewModels consume interfaces while Domain remains independent.

2. **Section 1 — value objects:** BorrowingStatus is an enum. No separate value-object type was found in the inspected Domain files. Replace “Value Objects & Enums” with “Enums.” Describe the project as using layered architecture and selected DDD principles unless more DDD patterns are demonstrated.

3. **Section 4.B — length constraints:** HasMaxLength configures model metadata, but the SQLite TEXT columns do not enforce those lengths. Replace “column length constraints” with “configured maximum-length metadata (100 for student names, 150 for equipment names); application validation or CHECK constraints are needed to enforce these limits in SQLite.” See [SQLite FAQ](https://sqlite.org/faq.html).

4. **Section 4.D — equipment tracking:** FirstOrDefaultAsync is in GetByIdAsync. UpdateAsync calls Update followed by SaveChangesAsync. Use: “GetAllAsync uses AsNoTracking for catalog display. GetByIdAsync uses a tracked query; UpdateAsync marks the entity for update and saves changes.”

5. **Section 4.E.1 — SQL analysis:** The displayed WHERE clause uses the literal 'Active', not a parameter. Replace the analysis with: “The enum-to-string conversion allows the Active comparison to translate into a SQL WHERE clause using the stored string value. AsNoTracking disables tracking of returned entities.”

6. **Section 4.E — generated SQL evidence:** Query logic is consistent with source, but actual SQL was not captured during this review. Label SQL as representative unless logs or ToQueryString output substantiate it. Parameter names and exact SQL formatting can vary.

7. **Section 5 — build and testing:** The current full solution build failed in the Desktop project with Avalonia resource-generation error MSB4061. Domain, Application, Infrastructure, ConsoleDemo, and Tests compiled. Test execution was blocked by Windows Application Control (0x800711C7), so no automated pass/fail count is established. A past successful build may have occurred, but its date, toolchain, and output are needed to support the historical claim.

8. **Section 5 — restart persistence:** The described GUI borrow/restart/return/restart sequence was not reproduced in this review. Preserve it as a historical manual test only if it was actually performed and supporting evidence is available. Source code alone cannot prove that sequence succeeded.

9. **Section 6.2 — unit testing:** Direct DbContext use couples ViewModels to persistence and makes isolated testing harder; it does not make isolated testing categorically impossible. Replace “preventing isolated unit testing” with “making isolated ViewModel testing more difficult.”

10. **Section 6.7 — changing providers:** Replacing SQLite may also require changes to the design-time factory, connection configuration, provider-specific migrations, mappings, and queries. Use: “The repository abstractions should allow Application services and UI code to remain largely unchanged. Infrastructure configuration, packages, the design-time factory, migrations, and provider-specific mappings or queries must be reviewed.” See [Microsoft: migrations with multiple providers](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers).

## Implementation limitations the report should acknowledge

- **Borrow/return operations are not atomic across repository saves.** Borrow saves equipment unavailability before inserting the borrowing. Return saves equipment availability before marking the borrowing returned. A failure between saves can leave inconsistent data. Coordinate each workflow using one transaction or one unit-of-work save. See [Microsoft: transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions).
- **Borrowing IDs can collide.** BorrowEquipmentService uses Random.Shared.Next(1000, 9999). A collision can cause a primary-key failure after availability was already saved. Use a reliable identity strategy compatible with both persistence implementations.
- **Scoped registrations are resolved from the root provider.** Only seeding creates an explicit scope; MainWindowViewModel is resolved from Services directly. Runtime scoped repositories/DbContext therefore live in the root scope. Registration alone does not provide a short-lived context per operation. See [Microsoft: DbContext lifetime](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/).
- **An existing test has an incorrect setup.** ExecuteAsync_ShouldFail_WhenEquipmentIsUnavailable requests equipment 102, but the fresh InMemoryEquipmentRepository marks equipment 102 available. From source inspection, that test is expected to fail when execution is permitted. Mark it unavailable or borrow it before checking rejection. This is a source finding, not an observed test result.
- **Test coverage is narrow.** The inspected test file contains three borrowing tests using in-memory repositories. It does not establish return behavior, quotas, SQLite integration, restart persistence, or failure recovery.
- **Concurrency is not protected by the shown schema/workflows.** Separate availability and quota checks can race between app instances. Foreign keys protect references, but do not enforce one active borrowing per equipment or per-student quotas.

## Recommended replacement verification paragraph

“Source inspection confirms the domain/application layers, Avalonia UI, EF Core SQLite persistence, migrations, and startup seeding are implemented. During review on 2026-11-14, the non-Desktop projects compiled, but the full solution build failed during Avalonia resource generation (MSB4061). Automated test execution was blocked by Windows Application Control (0x800711C7), so passing tests could not be confirmed. The previously described GUI restart-persistence test requires retained manual evidence or a fresh successful run. Transaction handling, ID generation, DbContext lifetime, and one test fixture require follow-up.”

The original report and project source were left unchanged. This review does not certify all lab requirements without the assignment sheets.
