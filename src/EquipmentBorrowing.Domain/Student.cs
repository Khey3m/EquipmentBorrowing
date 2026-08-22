namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; }
    public string FullName { get; private set; }
    public bool IsEligibleToBorrow { get; private set; }
    public int MaxAllowedBorrowings { get; private set; }

    public Student(int id, string fullName, bool isEligibleToBorrow = true, int maxAllowedBorrowings = 3)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Student name cannot be empty.", nameof(fullName));

        Id = id;
        FullName = fullName;
        IsEligibleToBorrow = isEligibleToBorrow;
        MaxAllowedBorrowings = maxAllowedBorrowings;
    }

    public bool CanBorrow(int currentActiveCount)
    {
        return IsEligibleToBorrow && currentActiveCount < MaxAllowedBorrowings;
    }
}
