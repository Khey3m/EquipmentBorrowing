namespace EquipmentBorrowing.Domain;

public class Borrowing
{
    public int Id { get; }
    public int StudentId { get; }
    public int EquipmentId { get; }
    public DateTime BorrowedAt { get; }
    public DateTime ExpectedReturnAt { get; }
    public BorrowingStatus Status { get; private set; }

    public Borrowing(int id, int studentId, int equipmentId, TimeSpan duration)
    {
        Id = id;
        StudentId = studentId;
        EquipmentId = equipmentId;
        BorrowedAt = DateTime.UtcNow;
        ExpectedReturnAt = BorrowedAt.Add(duration);
        Status = BorrowingStatus.Active;
    }

    public void MarkAsReturned()
    {
        Status = BorrowingStatus.Returned;
    }
}
