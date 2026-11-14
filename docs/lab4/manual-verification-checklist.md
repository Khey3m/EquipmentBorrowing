# Remaining staff walkthrough and visual acceptance

Use a disposable PostgreSQL database. Keep connection values/passwords out of recordings, screenshots and submitted evidence. CI covers automated behavior; this walkthrough supplies actual Desktop interaction and visual acceptance.

Record the tested commit, OS, setup data, action, expected result, observed result and evidence path for each scenario. Use a fresh database connection/context for verification reads. Do not mark a row passed until it has been performed.

## Normal integrated sequence

1. Launch the actual Desktop application. Confirm startup finishes, navigation works and the window remains responsive. Check there are no clipped labels, unreadable dates or unexpected blank sections at the default size and when resized.
2. Open Students. Register a student, search for it, edit its name/number, and clear optional contact. Confirm the same internal identity remains and duplicate/blank inputs produce useful reasons.
3. Open Manage Equipment. Register three distinguishable items, find/edit one and clear its description. Confirm availability is read-only and no transaction fields can be edited in this form.
4. Open Equipment. Compare catalog identifiers/names/descriptions/availability against PostgreSQL. Apply available-only filtering. Borrow one new item to the new student with a future expected return date; inspect the Active borrowing and unavailable item through a fresh connection.
5. Close the entire Desktop process and reopen it using the same disposable database. Confirm the borrowing and unavailability remain. This is the actual Desktop restart check, beyond CI's service-process probe.
6. Restrict the student. Confirm a new issue is rejected while the existing loan remains. Return the existing item after explicit confirmation; verify actual local return date, retained history and available state. Reject a repeated/stale return.
7. Permit the student again. Open Checkout multiple and issue two distinct items with one date. Verify two Active records and both unavailable flags. Test exactly at the three-item limit and reject an over-limit request without changed state.
8. Open Borrowing histories. Switch between two students and two items with distinguishable Active/Returned records, then subjects with no history. Confirm related names/dates/statuses, no record mixing and clear empty states. Change selection/mode rapidly during a slow read; an older result must not overwrite the newest selection.
9. Open Search borrowings. Search each required field; try every status, both dates and both sort directions, tied dates, combined criteria, no matches and Clear. Compare membership/order with controlled PostgreSQL data.
10. Close/reopen the Desktop process after edits and returns. Recheck student/equipment details, eligibility, returned history and current active loans.

## Failure/recovery and integrity

- Use invalid/missing/stale references, unavailable equipment, duplicate multi-item selections and an expected date no later than checkout. Expect an explanation and no borrowing/availability changes.
- Demonstrate empty catalog/list/history results separately from a connection failure.
- Stop access only to the disposable database or use an invalid local verification connection. Expect safe startup/read/write failure feedback, no credentials and no false success. Restore access, refresh/retry and verify state before staff resubmission.
- Use the automated controlled-after-write test for rollback evidence. Do not damage a shared database to manufacture failure.
- Explain Unknown commit handling: uncertainty is not reported as rollback or success; the UI requires a fresh read before a staff decision and does not automatically replay writes. An actual commit-acknowledgement-loss experiment has not been performed.
- Inspect normalized identifier uniqueness, foreign keys, required/nonblank checks, date/state checks, partial active-item uniqueness and real EF migration history. Keep generated database names with the corresponding evidence.

## Evidence still requiring a person

- Pixel appearance, accessibility/readability, keyboard usability and comfortable resizing.
- The complete staff click-through for all fourteen use cases and visual feedback branches.
- Real Avalonia Desktop close/reopen with PostgreSQL readback.
- Any demonstration-specific recording/screenshots requested by the instructor, plus required submitter/partner identity details supplied privately.

These are pending until performed; the checklist is not a pass record. Final documentation assembly and traceability are Session 11.
