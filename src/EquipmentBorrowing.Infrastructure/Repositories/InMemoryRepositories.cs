using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryStudentRepository : IStudentRepository
{
    private readonly List<Student> _students = new()
    {
        new Student(1, "Alice Guo", isEligibleToBorrow: true, maxAllowedBorrowings: 2),
        new Student(2, "Bob Smith", isEligibleToBorrow: false, maxAllowedBorrowings: 2),
        new Student(3, "Charlie Brown", isEligibleToBorrow: true, maxAllowedBorrowings: 3)
    };

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_students.FirstOrDefault(s => s.Id == id));

    public Task<IEnumerable<Student>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Student>>(_students);
}

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly List<Equipment> _equipments = new()
    {
        new Equipment(101, "Oscilloscope Rigol DS1054Z", isAvailable: true),
        new Equipment(102, "Digital Multimeter Fluke 117", isAvailable: true),
        new Equipment(103, "Soldering Station Weller WE1010", isAvailable: true)
    };

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_equipments.FirstOrDefault(e => e.Id == id));

    public Task<IEnumerable<Equipment>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Equipment>>(_equipments);

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        var existing = _equipments.FirstOrDefault(e => e.Id == equipment.Id);
        if (existing != null)
        {
            _equipments.Remove(existing);
            _equipments.Add(equipment);
        }
        return Task.CompletedTask;
    }
}

public class InMemoryBorrowingRepository : IBorrowingRepository
{
    private readonly List<Borrowing> _borrowings = new();

    public Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_borrowings.FirstOrDefault(b => b.Id == id));

    public Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Borrowing>>(_borrowings.Where(b => b.Status == BorrowingStatus.Active));

    public Task<int> GetActiveCountByStudentIdAsync(int studentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_borrowings.Count(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active));

    public Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        _borrowings.Add(borrowing);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        var existing = _borrowings.FirstOrDefault(b => b.Id == borrowing.Id);
        if (existing != null)
        {
            _borrowings.Remove(existing);
            _borrowings.Add(borrowing);
        }
        return Task.CompletedTask;
    }
}