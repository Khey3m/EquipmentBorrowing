# Session 2 — Domain and application contracts

## Identity and editing

StudentNumber and EquipmentIdentifier are staff-facing identifiers; integer Id remains the stable relational key. Required text is trimmed and rejected when blank. Normalized identifiers use Trim plus ToUpperInvariant in one shared helper. Session 3 must enforce uniqueness on these stored canonical values and prevent bypass via inconsistent database values. Database duplicate protection is not implemented by model validation alone.

Student contact and equipment description are optional: omitted/blank means null, including when clearing an existing value. Student edits preserve eligibility and ID; equipment edits preserve availability and ID. Registration defaults are eligible/available. No hard-delete API is added.

Every student has a fixed three-active-item limit, including existing plus requested items. MaxAllowedBorrowings is now a read-only compatibility property and excluded from EF mapping. Seed data no longer assigns a two-item limit.

## Calendar policy

BorrowedAt, ExpectedReturnAt and nullable ActualReturnAt use DateOnly. CampusClock converts UTC time to Asia/Manila before extracting the date; callers can inject a clock, time provider or timezone. Entity rules receive dates rather than reading the machine clock. Expected return must be strictly after checkout. Only active borrowings can return; actual return may equal checkout but cannot precede it. Early return is allowed.

BorrowRequest now takes an explicit expected return date. The existing duration UI temporarily converts its positive day count to a date using the campus clock; a staff date selector belongs to the later workflow/UI session. Borrowing/return services accept the clock for deterministic checks. This does not yet move validation inside a transaction.

## Reliable IDs

New Borrowing instances start with Id zero so EF can use database identity generation through the private setter. The in-memory adapter assigns increasing IDs, guarded against reassignment, instead of random IDs. Its retained list is a single-process demo/test adapter, not a concurrent production identity allocator. PostgreSQL identity constraints follow in Session 3.

## Application boundary

Lab4Contracts defines registration/edit/eligibility requests, multi-item checkout requests, borrowing search/result shapes, future management repository interfaces and history query contracts. Descriptive edit requests exclude protected transaction fields. OperationOutcome includes Unknown for commit acknowledgement loss. These contracts are foundations; management/history/multi-item implementations remain scheduled for later sessions.

## Transitional database limitation

The historical SQLite migrations do not describe the new fields, date types or removal of the mapped per-student limit. Do not run this intermediate model against the old SQLite database expecting migration compatibility. No existing database was modified. Session 3 must separate historical SQLite migrations and introduce a clean PostgreSQL schema before desktop persistence is considered runnable. Simultaneous SQLite support and historical data import are outside Lab 4 scope.

Borrow/return still perform separate repository saves and do not yet supply all Lab 4 concurrency/atomicity guarantees. Those are Session 3–4/7 work, not acceptance claims for this commit.

## Verification observed on 2026-11-14

- Tests project and its Domain/Application/Infrastructure references compiled with zero warnings/errors.
- ConsoleDemo compiled with zero warnings/errors after moving request callers to explicit dates.
- Initial test run: 10 domain cases passed; three existing service tests failed during loading of Infrastructure.dll due to Windows Application Control (0x800711C7), before exercising the service behavior.
- Added an eleventh domain case for the Manila/UTC calendar boundary and management repository contracts. The subsequent test project build succeeded, but Application Control blocked the rebuilt test DLL during discovery. Thus the final revision has 11 compiled domain cases; the added clock case is not runtime-verified, and the earlier 10 passes apply to the preceding revision.
- Desktop build was attempted and still fails in Avalonia resource task loading (MSB4061). Desktop compilation/launch is not verified.
- No security policy changes or bypasses were made. No PostgreSQL persistence acceptance scenario was claimed.
