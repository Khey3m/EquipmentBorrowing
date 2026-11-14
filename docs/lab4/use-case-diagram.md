# Lab 4 use-case diagram

The diagram shows the fourteen required user goals inside the Campus Equipment Borrowing System boundary. Associations are not workflow ordering. PostgreSQL is the supporting system for every case. Internal validation is intentionally not a separate use case; no include/extend relationships are required.

```mermaid
flowchart LR
    Staff["Laboratory Staff / Equipment Custodian"]
    PG["PostgreSQL (supporting system)"]
    subgraph System["Campus Equipment Borrowing System - Lab 4"]
        U01(["UC-01: View Equipment Catalog"])
        U02(["UC-02: Find Available Equipment"])
        U03(["UC-03: Borrow Equipment"])
        U04(["UC-04: View Active Borrowings"])
        U05(["UC-05: Return Equipment"])
        U06(["UC-06: Check Out Multiple Equipment Items"])
        U07(["UC-07: View Student Borrowing History"])
        U08(["UC-08: View Equipment Borrowing History"])
        U09(["UC-09: Search, Filter, and Sort Borrowings"])
        U12(["UC-12: Register Student"])
        U13(["UC-13: Update Student Information"])
        U14(["UC-14: Change Student Borrowing Eligibility"])
        U15(["UC-15: Register Equipment"])
        U16(["UC-16: Update Equipment Information"])
    end
    Staff --- U01
    Staff --- U02
    Staff --- U03
    Staff --- U04
    Staff --- U05
    Staff --- U06
    Staff --- U07
    Staff --- U08
    Staff --- U09
    Staff --- U12
    Staff --- U13
    Staff --- U14
    Staff --- U15
    Staff --- U16
    U01 --- PG
    U02 --- PG
    U03 --- PG
    U04 --- PG
    U05 --- PG
    U06 --- PG
    U07 --- PG
    U08 --- PG
    U09 --- PG
    U12 --- PG
    U13 --- PG
    U14 --- PG
    U15 --- PG
    U16 --- PG
```

Mermaid supplies a GitHub-renderable use-case-style view. The companion [PlantUML source](use-case-diagram.puml) uses native actor/usecase notation for export in a PlantUML renderer. Both describe the same boundary, fourteen goals and associations. See the [catalog](use-case-catalog.md) for deferred/future classifications and the [suite](use-case-suite.md) for behavior.
