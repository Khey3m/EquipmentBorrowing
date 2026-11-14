using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Tests;

public class Lab4DomainTests
{
    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 14, 17, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public void CampusDateUsesManilaCalendarRatherThanUtcCalendar()
    {
        var clock = new EquipmentBorrowing.Application.Interfaces.CampusClock(new FixedTimeProvider());
        Assert.Equal(new DateOnly(2026, 11, 15), clock.Today);
    }

    [Fact]
    public void StudentEditsNormalizeIdentifiersAndPreserveIdentityAndEligibility()
    {
        var student = new Student(7, " stu-07 ", " Alice ", " contact ");
        Assert.Equal("STU-07", student.NormalizedStudentNumber);
        Assert.True(student.IsEligibleToBorrow);
        Assert.True(student.SetEligibility(false));
        Assert.False(student.SetEligibility(false));
        student.UpdateInformation(" new-07 ", " Updated ", " ");
        Assert.Equal(7, student.Id);
        Assert.Equal("NEW-07", student.NormalizedStudentNumber);
        Assert.Null(student.ContactInformation);
        Assert.False(student.IsEligibleToBorrow);
        Assert.Throws<ArgumentException>(() => student.UpdateInformation("changed", " ", null));
        Assert.Equal("new-07", student.StudentNumber);
    }

    [Theory]
    [InlineData(0, 3, true)]
    [InlineData(1, 2, true)]
    [InlineData(2, 2, false)]
    [InlineData(3, 1, false)]
    [InlineData(-1, 1, false)]
    [InlineData(0, 0, false)]
    [InlineData(int.MaxValue, 1, false)]
    public void ActiveLimitCountsEveryRequestedItem(int active, int requested, bool allowed)
    {
        var student = new Student(1, "S1", "Student");
        Assert.Equal(allowed, student.CanBorrow(active, requested));
        student.SetEligibility(false);
        Assert.False(student.CanBorrow(active, requested));
    }

    [Fact]
    public void EquipmentDescriptiveEditsDoNotChangeAvailability()
    {
        var item = new Equipment(10, " eq-10 ", " Item ");
        Assert.True(item.IsAvailable);
        Assert.Equal("EQ-10", item.NormalizedEquipmentIdentifier);
        item.MarkAsBorrowed();
        item.UpdateInformation("EQ-11", "Renamed", " ");
        Assert.False(item.IsAvailable);
        Assert.Equal(10, item.Id);
        Assert.Null(item.Description);
        Assert.Throws<ArgumentException>(() => new Equipment(0, " ", "Item"));
        Assert.Throws<ArgumentException>(() => new Student(0, "S1", " "));
    }

    [Fact]
    public void ReturnAllowsEarlyReturnButRejectsInvalidAndRepeatedTransitions()
    {
        var checkout = new DateOnly(2026, 11, 14);
        Assert.Throws<ArgumentException>(() => new Borrowing(1, 1, checkout, checkout));
        Assert.Throws<ArgumentException>(() => new Borrowing(1, 1, checkout, checkout.AddDays(-1)));
        var borrowing = new Borrowing(1, 10, checkout, checkout.AddDays(3));
        Assert.Equal(0, borrowing.Id);
        borrowing.AssignIdentity(1);
        Assert.Throws<InvalidOperationException>(() => borrowing.AssignIdentity(2));
        Assert.Throws<ArgumentException>(() => borrowing.MarkAsReturned(checkout.AddDays(-1)));
        Assert.Equal(BorrowingStatus.Active, borrowing.Status);
        Assert.Null(borrowing.ActualReturnAt);
        borrowing.MarkAsReturned(checkout);
        Assert.Equal(checkout, borrowing.ActualReturnAt);
        Assert.Equal(BorrowingStatus.Returned, borrowing.Status);
        Assert.Throws<InvalidOperationException>(() => borrowing.MarkAsReturned(checkout));
    }
}
