using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Http;

internal static class BorrowingMapping
{
    public static Borrowing Snapshot(BorrowingResponse dto)
    {
        var borrowing = new Borrowing(dto.StudentId, dto.EquipmentId, dto.BorrowedDate, dto.ExpectedReturnDate);
        borrowing.AssignIdentity(dto.Id);
        if (dto.Status == BorrowingState.Returned) borrowing.MarkAsReturned(dto.ReturnedDate!.Value);
        return borrowing;
    }
    public static bool HasDisplayFields(BorrowingResponse dto) => !string.IsNullOrWhiteSpace(dto.StudentNumber)
        && !string.IsNullOrWhiteSpace(dto.StudentName) && !string.IsNullOrWhiteSpace(dto.EquipmentCode) && !string.IsNullOrWhiteSpace(dto.EquipmentName);
    public static BorrowingDetails Details(BorrowingResponse dto) => new(dto.Id, dto.StudentId, dto.StudentNumber!, dto.StudentName!,
        dto.EquipmentId, dto.EquipmentCode!, dto.EquipmentName!, dto.BorrowedDate, dto.ExpectedReturnDate, dto.ReturnedDate,
        Enum.Parse<BorrowingStatus>(dto.Status.ToString()));
    public const string UnknownMessage = "The API mutation response does not match the request. Outcome is unknown; refresh and verify before deciding whether to submit again.";
}

public sealed class HttpSingleCheckout(ApiTransport api) : ISingleCheckoutWorkflow
{
    public async Task<BorrowResult> ExecuteAsync(BorrowRequest request, CancellationToken cancellationToken = default)
    {
        var result = await api.SendAsync<BorrowingResponse>(HttpMethod.Post, "api/v1/borrowings",
            new BorrowInput(request.StudentId, request.EquipmentId, request.ExpectedReturnDate), 201, cancellationToken);
        if (!result.Success) return new(false, result.Message, Outcome: result.Outcome, Error: result.Error);
        var row = result.Value!;
        if (row.StudentId != request.StudentId || row.EquipmentId != request.EquipmentId
            || row.ExpectedReturnDate != request.ExpectedReturnDate || row.Status != BorrowingState.Active)
            return new(false, BorrowingMapping.UnknownMessage, Outcome: OperationOutcome.Unknown, Error: WorkflowError.UnknownOutcome);
        return new(true, "Equipment successfully borrowed through the API.", BorrowingMapping.Snapshot(row), OperationOutcome.Succeeded);
    }
}

public sealed class HttpReturnWorkflow(ApiTransport api) : IReturnWorkflow
{
    public async Task<ReturnResult> ExecuteAsync(int borrowingId, CancellationToken cancellationToken = default)
    {
        var result = await api.SendAsync<BorrowingResponse>(HttpMethod.Post, $"api/v1/borrowings/{borrowingId}/return",
            new ReturnInput(), ct: cancellationToken);
        if (!result.Success) return new(false, result.Message, result.Outcome, result.Error);
        var row = result.Value!;
        if (row.Id != borrowingId || row.Status != BorrowingState.Returned)
            return new(false, BorrowingMapping.UnknownMessage, OperationOutcome.Unknown, WorkflowError.UnknownOutcome);
        return new(true, "Equipment successfully returned through the API.", OperationOutcome.Succeeded, Borrowing: BorrowingMapping.Snapshot(row));
    }
}

public sealed class HttpMultiCheckout(ApiTransport api) : IMultiCheckoutWorkflow
{
    public async Task<OperationResult<IReadOnlyList<Borrowing>>> ExecuteAsync(MultiItemCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var ids = request.EquipmentIds?.ToArray();
        var result = await api.SendAsync<CheckoutResponse>(HttpMethod.Post, "api/v1/checkout-requests",
            new CheckoutInput(request.StudentId, ids, request.ExpectedReturnDate), 201, cancellationToken);
        if (!result.Success) return new(result.Outcome, result.Message, Error: result.Error);
        var rows = result.Value!.Borrowings;
        if (ids is null || rows.Count != ids.Length || !rows.Select(row => row.EquipmentId).Order().SequenceEqual(ids.Order())
            || rows.Any(row => row.StudentId != request.StudentId || row.Status != BorrowingState.Active || row.ExpectedReturnDate != request.ExpectedReturnDate))
            return new(OperationOutcome.Unknown, BorrowingMapping.UnknownMessage, Error: WorkflowError.UnknownOutcome);
        return new(OperationOutcome.Succeeded, $"Checked out {rows.Count} items together through the API.", rows.Select(BorrowingMapping.Snapshot).ToArray());
    }
}

public sealed class HttpActiveBorrowingReader(ApiTransport api) : IActiveBorrowingReader
{
    public async Task<QueryResult<IReadOnlyList<BorrowingDetails>>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default)
    {
        var result = await api.SendAsync<BorrowingResponse[]>(HttpMethod.Get, "api/v1/borrowings?status=Active", ct: cancellationToken);
        if (!result.Success) return new(false, null, result.Message);
        if (result.Value!.Any(row => row.Status != BorrowingState.Active || !BorrowingMapping.HasDisplayFields(row)))
            return new(false, null, "The API returned invalid active borrowing details. Refresh after checking the server.");
        return new(true, result.Value!.Select(BorrowingMapping.Details).ToArray(), "");
    }
}
