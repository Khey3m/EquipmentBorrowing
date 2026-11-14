namespace EquipmentBorrowing.Domain;

public class Borrowing
{
    public int Id { get; private set; }
    public int StudentId { get; private set; }
    public int EquipmentId { get; private set; }
    public DateOnly BorrowedAt { get; private set; }
    public DateOnly ExpectedReturnAt { get; private set; }
    public DateOnly? ActualReturnAt { get; private set; }
    public BorrowingStatus Status { get; private set; }

    private Borrowing() { } // EF materialization

    public Borrowing(int studentId, int equipmentId, DateOnly borrowedAt, DateOnly expectedReturnAt)
    {
        if (studentId <= 0 || equipmentId <= 0)
            throw new ArgumentException("An existing student and equipment item are required.");
        if (expectedReturnAt <= borrowedAt)
            throw new ArgumentException("Expected return must be later than checkout.", nameof(expectedReturnAt));
        StudentId = studentId;
        EquipmentId = equipmentId;
        BorrowedAt = borrowedAt;
        ExpectedReturnAt = expectedReturnAt;
        Status = BorrowingStatus.Active;
    }

    // The in-memory adapter assigns an identity; EF uses the private setter/database identity.
    public void AssignIdentity(int id)
    {
        if (Id != 0 || id <= 0) throw new InvalidOperationException("Identity can only be assigned once.");
        Id = id;
    }

    public void MarkAsReturned(DateOnly returnDate)
    {
        if (Status != BorrowingStatus.Active) throw new InvalidOperationException("Only active borrowings can be returned.");
        if (returnDate < BorrowedAt) throw new ArgumentException("Return cannot precede checkout.", nameof(returnDate));
        ActualReturnAt = returnDate;
        Status = BorrowingStatus.Returned;
    }
}
