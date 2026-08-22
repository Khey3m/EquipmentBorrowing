using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default);
}

public interface IBorrowingRepository
{
    Task<int> GetActiveCountByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default);
}