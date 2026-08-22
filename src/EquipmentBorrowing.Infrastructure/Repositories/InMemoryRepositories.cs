using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryStudentRepository : IStudentRepository
{
    // Simulating a database table with pre-existing students
    private readonly List<Student> _students = new()
    {
        new Student(1, "Alice Guo", isEligibleToBorrow: true, maxAllowedBorrowings: 2),
        new Student(2, "Bob Smith", isEligibleToBorrow: false, maxAllowedBorrowings: 2)
    };

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var student = _students.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(student);
    }
}

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    // Simulating a database table with pre-existing equipment
    private readonly List<Equipment> _equipments = new()
    {
        new Equipment(101, "Oscilloscope Rigol DS1054Z", isAvailable: true),
        new Equipment(102, "Digital Multimeter Fluke 117", isAvailable: false)
    };

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var equipment = _equipments.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(equipment);
    }

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        var existing = _equipments.FirstOrDefault(e => e.Id == equipment.Id);
        if (existing != null)
        {
            _equipments.Remove(existing);
            _equipments.Add(equipment); // "Update" by swapping out the old record
        }
        return Task.CompletedTask;
    }
}

public class InMemoryBorrowingRepository : IBorrowingRepository
{
    // Simulating an initially empty borrowings table
    private readonly List<Borrowing> _borrowings = new();

    public Task<int> GetActiveCountByStudentIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        int count = _borrowings.Count(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active);
        return Task.FromResult(count);
    }

    public Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        _borrowings.Add(borrowing);
        return Task.CompletedTask;
    }
}
