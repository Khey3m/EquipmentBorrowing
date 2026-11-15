# Lab 4 use-case catalog

Source: supplied activity handout Appendix A. Primary actor for the fourteen required cases: **Laboratory Staff / Equipment Custodian**. PostgreSQL supports all required cases. Students are stakeholders, not a student self-service actor in this scope.

| ID | Actor-level user goal | Classification | Delivered entry point |
|---|---|---|---|
| UC-01 | View Equipment Catalog | Required continuation | Equipment |
| UC-02 | Find Available Equipment | Required continuation | Equipment / Available items only |
| UC-03 | Borrow Equipment | Required continuation | Equipment / Borrow selected item |
| UC-04 | View Active Borrowings | Required continuation | Active Borrowings |
| UC-05 | Return Equipment | Required continuation | Active Borrowings / return confirmation |
| UC-06 | Check Out Multiple Equipment Items | Required new | Checkout multiple |
| UC-07 | View Student Borrowing History | Required new | Borrowing histories / Student |
| UC-08 | View Equipment Borrowing History | Required new | Borrowing histories / Equipment |
| UC-09 | Search, Filter, and Sort Borrowings | Required new | Search borrowings |
| UC-10 | View Overdue Borrowings | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-11 | View Current Equipment Holder | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-12 | Register Student | Required new | Students / Register |
| UC-13 | Update Student Information | Required new | Students / Save details |
| UC-14 | Change Student Borrowing Eligibility | Required new | Students / Permit or Restrict |
| UC-15 | Register Equipment | Required new | Manage Equipment / Register |
| UC-16 | Update Equipment Information | Required new | Manage Equipment / Save details |
| UC-17 | Retire Equipment from Circulation | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-18 | Mark Equipment as Under Maintenance | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-19 | Restore Equipment to Circulation | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-20 | Record Equipment Condition Upon Return | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-21 | Renew or Extend an Active Borrowing | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-22 | Return Multiple Equipment Items | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-23 | View Borrowings Due Soon | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-24 | View Student's Active Borrowing Count | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-25 | View Equipment Utilization Summary | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-26 | Manage Equipment Categories | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-27 | Void an Erroneous Borrowing Without Erasing Its History | Deferred to Lab 5 | Outside Lab 4 implementation |
| UC-28 | Request an Equipment Reservation | Future scope | Outside Lab 4 implementation |
| UC-29 | Approve, Reject, or Cancel Reservations | Future scope | Outside Lab 4 implementation |
| UC-30 | Review Staff Action Audit History | Future scope | Outside Lab 4 implementation |
| UC-31 | Manage Staff Accounts and Permissions | Future scope | Outside Lab 4 implementation |
| UC-32 | Export Borrowing Reports | Future scope | Outside Lab 4 implementation |
| UC-33 | Integrate External Services or Notifications | Future scope | Outside Lab 4 implementation |

The required suite contains exactly UC-01 through UC-09 and UC-12 through UC-16 (14 cases). No additional suite/diagram coverage is claimed for deferred or future cases. Validation and database operations are internal responsibilities, not extra user goals.

Source and automated verification are complete for the implemented cases; manual visual/real-Desktop restart checks remain in [handoff](submission-handoff.md). See [diagram](use-case-diagram.md), [suite](use-case-suite.md) and [traceability](final-traceability.md).
