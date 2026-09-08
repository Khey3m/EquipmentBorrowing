using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Student>> GetAllAsync(CancellationToken cancellationToken = default);
}

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default);
}

public interface IBorrowingRepository
{
    Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default);
    Task<int> GetActiveCountByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default);
    Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default);
}