using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Api;

public static class QueryAndCheckoutEndpoints
{
    public static void MapQueryAndCheckoutEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/checkout-requests", async (CheckoutInput input, MultiItemCheckoutService service, CancellationToken ct) =>
        {
            if (input.StudentId is not > 0 || input.EquipmentIds is null || input.ExpectedReturnDate is null)
                return Error(400, "invalid_request", "Positive studentId, equipmentIds and expectedReturnDate are required.");
            var result = await service.ExecuteAsync(new(input.StudentId.Value, input.EquipmentIds, input.ExpectedReturnDate.Value), ct);
            return result.Outcome == OperationOutcome.Succeeded && result.Value is not null
                ? Results.Json(new CheckoutResponse(result.Value.Select(BorrowingEndpoints.Response).ToArray()), statusCode: 201)
                : ApiFailures.From(result.Error, result.Message);
        }).WithName("API-11").WithTags("Borrowings").WithSummary("Atomically issue at least two distinct items")
            .Produces<CheckoutResponse>(201).Produces<ApiError>(400).Produces<ApiError>(404)
            .Produces<ApiError>(409).Produces<ApiError>(500);

        app.MapGet("/api/v1/borrowings", async (string? search, string? status, string? sort, string? direction,
            HttpRequest request, BorrowingSearchService service, CancellationToken ct) =>
        {
            if (request.Query.Any(q => q.Key is not ("search" or "status" or "sort" or "direction") || q.Value.Count != 1)
                || status is not (null or "All" or "Active" or "Returned")
                || sort is not (null or "borrowedDate" or "expectedReturnDate")
                || direction is not (null or "asc" or "desc"))
                return Error(400, "invalid_query", "Use status All/Active/Returned, sort borrowedDate/expectedReturnDate and direction asc/desc.");
            BorrowingStatus? filter = status switch { "Active" => BorrowingStatus.Active, "Returned" => BorrowingStatus.Returned, _ => null };
            var result = await service.SearchAsync(new(search, filter,
                sort == "expectedReturnDate" ? BorrowingSortField.ExpectedReturnDate : BorrowingSortField.CheckoutDate,
                direction == "desc"), ct);
            return result.Success ? Results.Ok(result.Value!.Select(Response).ToArray())
                : Error(500, "server_error", result.Message);
        }).WithName("API-14").WithTags("Borrowings").WithSummary("Search/filter/sort history; default All, borrowedDate, asc; ties use ID ascending")
            .Produces<BorrowingResponse[]>(200).Produces<ApiError>(400).Produces<ApiError>(500);

        app.MapGet("/api/v1/students/{id}/borrowings", (int id, BorrowingHistoryService service, CancellationToken ct) =>
            History(service, HistoryKind.Student, id, ct))
            .WithName("API-15").WithTags("History").WithSummary("Student history including active and returned loans")
            .Produces<BorrowingResponse[]>(200).Produces<ApiError>(400).Produces<ApiError>(404).Produces<ApiError>(500);
        app.MapGet("/api/v1/equipment/{id}/borrowings", (int id, BorrowingHistoryService service, CancellationToken ct) =>
            History(service, HistoryKind.Equipment, id, ct))
            .WithName("API-16").WithTags("History").WithSummary("Equipment history including active and returned loans")
            .Produces<BorrowingResponse[]>(200).Produces<ApiError>(400).Produces<ApiError>(404).Produces<ApiError>(500);
    }
    private static async Task<IResult> History(BorrowingHistoryService service, HistoryKind kind, int id, CancellationToken ct)
    {
        var result = await service.GetHistoryAsync(kind, id, ct);
        return result.Outcome == OperationOutcome.Succeeded && result.Value is not null
            ? Results.Ok(result.Value.Rows.Select(Response).ToArray()) : ApiFailures.From(result.Error, result.Message);
    }
    private static BorrowingResponse Response(BorrowingDetails row) => new(row.Id, row.StudentId, row.EquipmentId,
        row.CheckoutDate, row.ExpectedReturnDate, row.ActualReturnDate, Enum.Parse<BorrowingState>(row.Status.ToString()),
        row.StudentNumber, row.StudentName, row.EquipmentIdentifier, row.EquipmentName);
    private static IResult Error(int status, string code, string message) => Results.Json(new ApiError(code, message), statusCode: status);
}
