# Campus Equipment Borrowing System - Architectural Blueprint

Detailed current report: [Lab 4 development progress — Sessions 1–4](docs/lab4/development-report-sessions-1-4.md).

Recent progress: [Session 5 — student management](docs/lab4/session5-evidence.md) and [Session 6 — equipment registration and editing](docs/lab4/session6-evidence.md).

> **Lab 4 in progress:** PostgreSQL replaces operational SQLite. Follow [PostgreSQL setup](docs/lab4/postgres-setup.md), [progress](docs/lab4/progress.md), and the [acceptance matrix](docs/lab4/acceptance-matrix.md) for current instructions. Earlier sections below retain historical in-memory/SQLite documentation. Sessions 1–6 source work includes transactional borrowing/returns, catalog/active-loan screens, student management and equipment registration/editing. SQL constraints are verified; EF migration execution, Desktop and workflow runtime acceptance remain blocked locally by Windows Application Control.

## 1. Solution Structure
* **EquipmentBorrowing.Domain**: Contains the core concepts and business rules (Ex: `Student`, `Equipment`, `Borrowing`, and `BorrowingStatus`). This layer is independent and has zero external dependencies.
* **EquipmentBorrowing.Application**: Contains the use cases and operations (`BorrowEquipmentService`) and defines how data should be retrieved/saved using interfaces (`IStudentRepository`, etc.).
* **EquipmentBorrowing.Infrastructure**: Implements the technical details, uses C# collections (`InMemoryStudentRepository`, etc.) to store data in memory, fulfilling the application's interfaces.
* **EquipmentBorrowing.Tests**: Contains automated xUnit tests to verify that the application services and domain rules behave correctly.

## 2. Dependency Direction
Dependencies always point inward toward the Domain layer.

```text
 Executable / ConsoleDemo
          │
          ▼
     Application
       │      ▲
       ▼      │ (Implements Interfaces)
     Domain   │
              │
     Infrastructure
```




## 3. Use Case Mapping

* **Actor**: Student
* **Use Case**: Borrow Equipment
* **Application Service**: `BorrowEquipmentService`
* **Domain Objects Used**: `Student`, `Equipment`, `Borrowing`, `BorrowingStatus`
* **Repository Interfaces Used**: `IStudentRepository`, `IEquipmentRepository`, `IBorrowingRepository`
* **Infrastructure Implementations Used**: `InMemoryStudentRepository`, `InMemoryEquipmentRepository`, `InMemoryBorrowingRepository`



## 4. Reflection Answers

* **Why should the application service depend on a repository interface instead of directly depending on a database implementation?**
    Relying on interfaces keeps the application disconnected from specific technologies (Dependency Inversion Principle). It allows the system to switch from an in-memory database to SQL Server or SQLite without modifying the core business logic.

* **Which parts of your current solution could remain unchanged if SQLite were added later?**
    The `Domain` and `Application` projects would remain 100% unchanged. Only the `Infrastructure` project would get new repository implementations for SQLite.

* **Which project would eventually contain Avalonia Views?**
    A completely new Presentation/UI project (Ex: `EquipmentBorrowing.DesktopUI`) that references the Application and Infrastructure layers.

* **Should an Avalonia button directly execute database queries? Why or why not?**
    No, Direct database queries in the UI violate the Separation of Concerns. It tightly separates the visual interface to the database schema, making the application extremely hard to maintain, test, and secure.

* **What part of your implementation represents the actual business operation requested by the actor?**
    The `ExecuteAsync` method inside the `BorrowEquipmentService` represents the actual business operation, organizing the domain rules and data persistence to fulfill the "Borrow Equipment" request.




    ## 5. Desktop Project Architecture (Avalonia UI & MVVM)

    ### Reflection Answers (Activity 2)
    Avalonia View (XAML)
│
│ [Data Binding & Relay Commands]
▼
ViewModel (CommunityToolkit.Mvvm)
│
│ [Executes Operations]
▼
Application Services (Borrow & Return)
│
├──► Domain Models & Validation
│
▼
Repository Interfaces (Abstractions)
▲
│ [Implements]
Infrastructure (InMemory Repositories)


1. **Why should the View not call a repository directly?**
   Calling repositories directly from Views couples presentation logic to storage implementations, breaking separation of concerns and making automated UI testing impossible.
2. **Why should business rules not be implemented in the ViewModel?**
   ViewModels manage presentation state and user feedback. Placing domain rules in ViewModels duplicates logic across different UI targets and compromises system integrity.
3. **What is the responsibility of the ViewModel?**
   To adapt application data for UI consumption, handle selection/input presentation state, expose user commands, and trigger application services.
4. **Why can the existing Application layer work without knowing that Avalonia is being used?**
   Because the Application layer relies solely on standard C# abstractions and domain objects, rendering it completely agnostic to presentation frameworks.
5. **What advantage is gained from registering dependencies in one composition point?**
   Centralizing dependency registration in `App.axaml.cs` provides clear lifecycle control (e.g., Singleton vs. Transient) and allows swapping implementations without modifying consuming classes.
6. **If the in-memory repository were replaced by SQLite later, which parts of the current interface should remain largely unchanged?**
   All Views, ViewModels, Application Services, and Domain Models remain completely untouched; only the Infrastructure layer requires updates.

### Updated Architecture Diagram


## 6. Relational Database & Entity Framework Core (Laboratory Activity 3)

### Relational Database Schema Design
The application uses SQLite as its primary relational database. The schema maintains strict foreign key constraints to ensure referential integrity across borrowing workflows.

```text
STUDENTS
--------------------------------------------------
Id                   INTEGER    (Primary Key)
FullName             TEXT       (Required, Max 100)
IsEligibleToBorrow   BOOLEAN    (Required)
MaxAllowedBorrowings INTEGER    (Required)

EQUIPMENT
--------------------------------------------------
Id                   INTEGER    (Primary Key)
Name                 TEXT       (Required, Max 150)
IsAvailable          BOOLEAN    (Required)

BORROWINGS
--------------------------------------------------
Id                   INTEGER    (Primary Key)
StudentId            INTEGER    (Foreign Key -> STUDENTS.Id)
EquipmentId          INTEGER    (Foreign Key -> EQUIPMENT.Id)
BorrowedAt           DATETIME   (Required)
ExpectedReturnAt     DATETIME   (Required)
Status               TEXT       (Required, Enum: Active/Returned)



### Database ERD Diagram

```mermaid
erDiagram
    STUDENT ||--o{ BORROWING : places
    EQUIPMENT ||--o{ BORROWING : subject_of

    STUDENT {
        int Id PK
        string FullName
        bool IsEligibleToBorrow
        int MaxAllowedBorrowings
    }

    EQUIPMENT {
        int Id PK
        string Name
        bool IsAvailable
    }

    BORROWING {
        int Id PK
        int StudentId FK
        int EquipmentId FK
        datetime BorrowedAt
        datetime ExpectedReturnAt
        string Status
    }



## Architectural Reflection (Laboratory Activity 3)

1. **Why did the application not need to be completely rewritten when SQLite was introduced?**
   Because the Domain, Application, and Presentation layers depend on abstract repository interfaces (`IEquipmentRepository`, `IStudentRepository`, `IBorrowingRepository`) rather than concrete database classes[cite: 5]. Swapping the persistence layer only required creating EF Core implementations of those interfaces[cite: 5].

2. **Why should the ViewModel not use `DbContext` directly?**
   Directly using `DbContext` inside ViewModels leaks database details into the UI layer, violating Separation of Concerns and making the application harder to unit test without an actual database[cite: 5].

3. **What responsibility does the repository implementation now perform?**
   It translates domain-level repository requests into EF Core LINQ queries, handles entity state mapping, and executes `DbContext` persistence operations (`SaveChangesAsync`)[cite: 5].

4. **What is the purpose of an EF Core migration?**
   To generate version-controlled, reproducible database schema scripts that align the relational database tables with changes in C# model configurations over time[cite: 5].

5. **Why are foreign keys important in the borrowing database?**
   They enforce referential integrity in SQLite, ensuring borrowing records cannot reference non-existent students or equipment items[cite: 5].

6. **Why can a read-only query benefit from `AsNoTracking()`?**
   It tells EF Core not to track changes on retrieved entities, saving memory and improving query execution speed for display-only data[cite: 5].

7. **What would happen to the rest of the application if the SQLite implementation were replaced later by another database provider?**
   Only the `DbContext` provider registration in `App.axaml.cs` and Infrastructure package references would change[cite: 5]. All Application services, ViewModels, and Avalonia Views would remain completely untouched[cite: 5].
