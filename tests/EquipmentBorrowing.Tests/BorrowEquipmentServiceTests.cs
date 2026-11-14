using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Infrastructure.Repositories;

namespace EquipmentBorrowing.Tests;

public class BorrowEquipmentServiceTests
{
    private sealed class FixedClock : ICampusClock
    {
        public DateOnly Today => new(2026, 11, 14);
    }

    private readonly BorrowEquipmentService _service;

    public BorrowEquipmentServiceTests()
    {
        // Set up fresh repositories for the tests
        _service = new BorrowEquipmentService(
            new InMemoryStudentRepository(),
            new InMemoryEquipmentRepository(),
            new InMemoryBorrowingRepository(), new FixedClock());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new BorrowRequest(1, 101, new DateOnly(2026, 11, 15));
        var result = await _service.ExecuteAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Borrowing);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenEquipmentIsUnavailable()
    {
        // Issue the item first: the fresh repository starts with every item available.
        var initialBorrow = await _service.ExecuteAsync(
            new BorrowRequest(1, 102, new DateOnly(2026, 11, 15)));
        Assert.True(initialBorrow.Success);

        var request = new BorrowRequest(1, 102, new DateOnly(2026, 11, 15));
        var result = await _service.ExecuteAsync(request);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenStudentIsIneligible()
    {
        var request = new BorrowRequest(2, 101, new DateOnly(2026, 11, 15));
        var result = await _service.ExecuteAsync(request);

        Assert.False(result.Success);
    }
}
