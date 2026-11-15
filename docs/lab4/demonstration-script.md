# Lab 4 demonstration script

Use a disposable database and record the tested commit, environment, setup, expected and observed results, and evidence location. This is a script, not a record that its manual steps have already passed. Keep credentials and private submitter information out of screenshots and logs.

## Suggested preparation

1. Follow the README to configure a dedicated PostgreSQL database and migrate/start the application. Show the real __EFMigrationsHistory row and schema constraints through a fresh connection.
2. Prepare at least two students with distinguishable histories, an ineligible student, several available/unavailable items, and a student/item with no history. Startup examples can be supplemented through the management forms.
3. Keep [final traceability](final-traceability.md) and the verified CI run open. Explain which evidence comes from PostgreSQL integration, headless controls, separate service processes and the current real Desktop walkthrough.

## Walkthrough sequence

| Step | Staff action | Expected result and evidence |
|---|---|---|
| 1 — Student foundation (A12–A13) | Students -> New student -> register; locate/edit number/name/contact and clear optional contact | One durable identity, eligible default, revised descriptive fields. Reject blank/duplicate number variants. Fresh readback; later edit a student after loans exist too |
| 2 — Equipment foundation (A15–A16) | Manage Equipment -> register and locate/edit an item; clear description | Available default, same internal ID after edits, no editable availability/status/date controls. Reject blank/duplicate identifiers; retain history once loans exist |
| 3 — Catalog (A01–A02) | Open Equipment, compare fields, apply Available items only | Accurate current names/identifiers/descriptions/states, unavailable/active items excluded from available filter; demonstrate empty versus failed load separately |
| 4 — Single issue (A03) | Select new student/item and future date, then Borrow selected item | One Active loan and unavailable equipment. Show fresh readback; separately reject ineligible/missing/unavailable/over-limit/invalid-date requests |
| 5 — Restart/readback (A03) | Close the entire Desktop process and reopen | Same Active loan/links/date and unavailable equipment survive. Contrast this with CI's separate-service-process probe, which is already automated evidence |
| 6 — Eligibility and return (A14/A05) | Restrict student; attempt a new issue; return the existing loan with confirmation; permit again | New issue rejected without cancelling history; return succeeds with actual local date, retained row and available item. Repeat/stale return rejected; repeated eligibility gives no-change feedback |
| 7 — Coordinated issue (A06) | Checkout multiple -> select two distinct available items and one date | Both Active rows and unavailability saved together. Show exactly-at-limit and each rejection, including duplicate IDs via a controlled test request if the checkbox UI cannot represent duplicates |
| 8 — Safe rollback (A06/A18) | Show MultiItemPostgresTests controlled observer and CI TRX/logs; rerun in a disposable database if needed | Observer runs after an equipment update and borrowing insert inside the transaction; fresh context sees no request rows or unavailable flags. Do not manufacture a failure in shared data |
| 9 — Histories (A07–A08) | Borrow/return an item; open Student and Equipment modes; select distinguishable and empty subjects; change selections rapidly | Correct related names/identifiers/statuses/checkout/expected/actual dates, no record mixing, responsive loading and clear empty/missing/error/retry feedback |
| 10 — Retrieval (A09) | Search each required field, all statuses, both dates/directions, ties, combined criteria and Clear | SQL-backed membership/order matches controlled records. Borrowing ID ascending breaks date ties. Empty results are not a connection error; old slow requests cannot overwrite new criteria |
| 11 — Recovery (A18) | Safely deny access to the disposable database or use an invalid verification connection; restore and refresh/retry | Safe messages, no false success or partial known-failure writes; restored access works. Explain Unknown commit handling without claiming an actual lost-ack experiment was performed |
| 12 — Final restart | Close/reopen again and inspect edited students/equipment/eligibility and returned/active history | Changed fields and relationships persist; no startup overwrite. Record fresh database reads |

## Short architecture explanation

Show View -> ViewModel -> Application service -> repository/query/session contract -> EF/Npgsql -> PostgreSQL. Explain short-lived contexts, a shared serializable write transaction, full checkout prevalidation, database uniqueness/FKs and active-item protection, date policy, cancellation/versioning and commit uncertainty. The Domain does not depend on repositories or EF. No unnecessary validation use cases or include/extend relationships are modeled.

## Close the demonstration

Show the fourteen-case suite/catalog/diagram, final traceability, setup commands, meaningful Git history and commit-specific CI results. Record what was actually observed and leave unperformed branches pending. Submitter/partner names and IDs belong in the private submission location required by the handout.
