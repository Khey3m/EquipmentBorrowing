using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Http;

public sealed class HttpBorrowingSearch(ApiTransport api) : IBorrowingSearchWorkflow
{
    public async Task<QueryResult<IReadOnlyList<BorrowingDetails>>> SearchAsync(BorrowingSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.SortBy) || (request.Status.HasValue && !Enum.IsDefined(request.Status.Value)))
            return new(false, null, "Choose a valid status and date sort field.");
        var path = "api/v1/borrowings?search=" + Uri.EscapeDataString(request.SearchText?.Trim() ?? "")
            + "&status=" + (request.Status?.ToString() ?? "All")
            + "&sort=" + (request.SortBy == BorrowingSortField.ExpectedReturnDate ? "expectedReturnDate" : "borrowedDate")
            + "&direction=" + (request.Descending ? "desc" : "asc");
        var result = await api.SendAsync<BorrowingResponse[]>(HttpMethod.Get, path, ct: cancellationToken);
        if (!result.Success) return new(false, null, result.Message);
        if (result.Value!.Any(row => !BorrowingMapping.HasDisplayFields(row)))
            return new(false, null, "The API returned incomplete related borrowing details. Check the server and retry.");
        return new(true, result.Value!.Select(BorrowingMapping.Details).ToArray(),
            result.Value!.Length == 0 ? "No borrowings match these criteria." : "");
    }
}

public sealed class HttpHistoryWorkflow(ApiTransport api) : IHistoryWorkflow
{
    public async Task<QueryResult<IReadOnlyList<HistorySubject>>> GetSubjectsAsync(HistoryKind kind, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(kind)) return new(false, null, "Choose Student or Equipment history.");
        if (kind == HistoryKind.Student)
        {
            var result = await api.SendAsync<StudentResponse[]>(HttpMethod.Get, "api/v1/students", ct: cancellationToken);
            return result.Success ? new(true, result.Value!.OrderBy(row => row.FullName).ThenBy(row => row.Id)
                .Select(row => new HistorySubject(row.Id, row.StudentNumber + " - " + row.FullName)).ToArray(), "") : new(false, null, result.Message);
        }
        var items = await api.SendAsync<EquipmentResponse[]>(HttpMethod.Get, "api/v1/equipment", ct: cancellationToken);
        return items.Success ? new(true, items.Value!.OrderBy(row => row.Name).ThenBy(row => row.Id)
            .Select(row => new HistorySubject(row.Id, row.EquipmentCode + " - " + row.Name)).ToArray(), "") : new(false, null, items.Message);
    }
    public async Task<OperationResult<HistorySnapshot>> GetHistoryAsync(HistoryKind kind, int subjectId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(kind)) return new(OperationOutcome.Rejected, "Choose Student or Equipment history.", Error: WorkflowError.InvalidInput);
        var resource = kind == HistoryKind.Student ? "students" : "equipment";
        var path = $"api/v1/{resource}/{subjectId}";
        HistorySubject subject;
        if (kind == HistoryKind.Student)
        {
            var selected = await api.SendAsync<StudentResponse>(HttpMethod.Get, path, ct: cancellationToken);
            if (!selected.Success) return PresentationMapping.Failure<HistorySnapshot, StudentResponse>(selected);
            if (selected.Value!.Id != subjectId) return Invalid();
            subject = new(subjectId, selected.Value.StudentNumber + " - " + selected.Value.FullName);
        }
        else
        {
            var selected = await api.SendAsync<EquipmentResponse>(HttpMethod.Get, path, ct: cancellationToken);
            if (!selected.Success) return PresentationMapping.Failure<HistorySnapshot, EquipmentResponse>(selected);
            if (selected.Value!.Id != subjectId) return Invalid();
            subject = new(subjectId, selected.Value.EquipmentCode + " - " + selected.Value.Name);
        }
        var rows = await api.SendAsync<BorrowingResponse[]>(HttpMethod.Get, path + "/borrowings", ct: cancellationToken);
        if (!rows.Success) return PresentationMapping.Failure<HistorySnapshot, BorrowingResponse[]>(rows);
        if (rows.Value!.Any(row => !BorrowingMapping.HasDisplayFields(row)
            || (kind == HistoryKind.Student ? row.StudentId : row.EquipmentId) != subjectId)) return Invalid();
        return new(OperationOutcome.Succeeded, rows.Value!.Length == 0 ? "No borrowing history for this selection." : "",
            new(subject, rows.Value!.Select(BorrowingMapping.Details).ToArray()));
    }
    private static OperationResult<HistorySnapshot> Invalid() => new(OperationOutcome.Failed,
        "The API returned history for an unexpected or incomplete subject. Refresh and retry.", Error: WorkflowError.DatabaseFailure);
}
