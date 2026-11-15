using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Api;

public static class BorrowingEndpoints
{
    public static void MapBorrowingEndpoints(this WebApplication app)
    {
        var borrowings = app.MapGroup("/api/v1/borrowings").WithTags("Borrowings");
        borrowings.MapPost("", async (BorrowInput input, BorrowEquipmentService service, CancellationToken ct) =>
        {
            if (input.StudentId is not > 0 || input.EquipmentId is not > 0 || input.ExpectedReturnDate is null)
                return Error(400, "invalid_request", "Positive studentId, equipmentId and expectedReturnDate are required.");
            var result = await service.ExecuteAsync(new(input.StudentId.Value, input.EquipmentId.Value, input.ExpectedReturnDate.Value), ct);
            return result.Success && result.Borrowing is not null
                ? Results.Created($"/api/v1/borrowings/{result.Borrowing.Id}", Response(result.Borrowing))
                : ApiFailures.From(result.Error, result.Message);
        }).WithName("API-10").WithSummary("Issue one item; server determines checkout date")
            .Produces<BorrowingResponse>(201).Produces<ApiError>(400).Produces<ApiError>(404)
            .Produces<ApiError>(409).Produces<ApiError>(500);

        borrowings.MapGet("/{id}", async (int id, IBorrowingRepository repository, CancellationToken ct) =>
        {
            var borrowing = await repository.GetByIdAsync(id, ct);
            return borrowing is null ? Error(404, "not_found", "The borrowing was not found.") : Results.Ok(Response(borrowing));
        }).WithName("API-13").WithSummary("Read a saved borrowing")
            .Produces<BorrowingResponse>(200).Produces<ApiError>(400).Produces<ApiError>(404).Produces<ApiError>(500);

        borrowings.MapPost("/{id}/return", async (int id, ReturnInput input, ReturnEquipmentService service, CancellationToken ct) =>
        {
            var result = await service.ExecuteAsync(id, ct);
            return result.Success && result.Borrowing is not null
                ? Results.Ok(Response(result.Borrowing)) : ApiFailures.From(result.Error, result.Message);
        }).WithName("API-12").WithSummary("Return an active loan; send an empty JSON object")
            .Produces<BorrowingResponse>(200).Produces<ApiError>(400).Produces<ApiError>(404)
            .Produces<ApiError>(409).Produces<ApiError>(500);
    }
    internal static BorrowingResponse Response(Borrowing loan) => new(loan.Id, loan.StudentId, loan.EquipmentId,
        loan.BorrowedAt, loan.ExpectedReturnAt, loan.ActualReturnAt, Enum.Parse<BorrowingState>(loan.Status.ToString()));
    private static IResult Error(int status, string code, string message) => Results.Json(new ApiError(code, message), statusCode: status);
}
