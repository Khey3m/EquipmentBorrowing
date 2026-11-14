namespace EquipmentBorrowing.Domain;

public class Student
{
    public const int ActiveBorrowingLimit = 3;
    public int Id { get; private set; }
    public string StudentNumber { get; private set; } = string.Empty;
    public string NormalizedStudentNumber { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? ContactInformation { get; private set; }
    public bool IsEligibleToBorrow { get; private set; }
    public int MaxAllowedBorrowings => ActiveBorrowingLimit;

    private Student() { } // EF materialization

    public Student(int id, string studentNumber, string fullName, string? contactInformation = null,
        bool isEligibleToBorrow = true)
    {
        Id = id;
        UpdateInformation(studentNumber, fullName, contactInformation);
        IsEligibleToBorrow = isEligibleToBorrow;
    }

    public void UpdateInformation(string studentNumber, string fullName, string? contactInformation)
    {
        // Validate every value before mutating the entity.
        var number = DomainText.Required(studentNumber, nameof(StudentNumber));
        var name = DomainText.Required(fullName, nameof(FullName));
        StudentNumber = number;
        NormalizedStudentNumber = DomainText.NormalizeIdentifier(number);
        FullName = name;
        ContactInformation = DomainText.Optional(contactInformation);
    }

    public bool SetEligibility(bool eligible)
    {
        if (IsEligibleToBorrow == eligible) return false;
        IsEligibleToBorrow = eligible;
        return true;
    }

    public bool CanBorrow(int currentActiveCount, int requestedCount = 1) =>
        IsEligibleToBorrow && currentActiveCount >= 0 && requestedCount > 0 &&
        currentActiveCount <= ActiveBorrowingLimit &&
        requestedCount <= ActiveBorrowingLimit - currentActiveCount;
}
