using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Repositories;

// 1. Instantiate the in-memory repositories (our fake database)
var studentRepo = new InMemoryStudentRepository();
var equipmentRepo = new InMemoryEquipmentRepository();
var borrowingRepo = new InMemoryBorrowingRepository();

// 2. Inject dependencies manually into the application service
var borrowingService = new BorrowEquipmentService(studentRepo, equipmentRepo, borrowingRepo);

Console.WriteLine("=== CAMPUS EQUIPMENT BORROWING DEMO ===");
Console.WriteLine();

// TEST CASE 1: Successful Borrowing (Eligible student, Available equipment)
Console.WriteLine("[TEST 1] Attempting valid borrowing (Student ID: 1, Equipment ID: 101)...");
var request1 = new BorrowRequest(StudentId: 1, EquipmentId: 101, Duration: TimeSpan.FromDays(3));
var result1 = await borrowingService.ExecuteAsync(request1);
Console.WriteLine($"Result: {(result1.Success ? "SUCCESS" : "FAILED")} - {result1.Message}");
if (result1.Success)
{
    Console.WriteLine($"Borrowing Record Created! ID: {result1.Borrowing!.Id}, Due: {result1.Borrowing.ExpectedReturnAt}");
}
Console.WriteLine();

// TEST CASE 2: Equipment Unavailable Failure (Digital Multimeter is already borrowed)
Console.WriteLine("[TEST 2] Attempting to borrow unavailable equipment (Student ID: 1, Equipment ID: 102)...");
var request2 = new BorrowRequest(StudentId: 1, EquipmentId: 102, Duration: TimeSpan.FromDays(1));
var result2 = await borrowingService.ExecuteAsync(request2);
Console.WriteLine($"Result: {(result2.Success ? "SUCCESS" : "FAILED")} - {result2.Message}");
Console.WriteLine();

// TEST CASE 3: Student Ineligible Failure (Bob Smith is suspended)
Console.WriteLine("[TEST 3] Attempting borrow with suspended student (Student ID: 2, Equipment ID: 101)...");
var request3 = new BorrowRequest(StudentId: 2, EquipmentId: 101, Duration: TimeSpan.FromDays(2));
var result3 = await borrowingService.ExecuteAsync(request3);
Console.WriteLine($"Result: {(result3.Success ? "SUCCESS" : "FAILED")} - {result3.Message}");

Console.ReadLine();