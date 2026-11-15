using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Http;

// Domain objects are presentation snapshots here; no client-side changes are persisted directly.
internal static class PresentationMapping
{
    public static Student Student(StudentResponse dto) => new(dto.Id, dto.StudentNumber, dto.FullName, dto.Contact, dto.IsEligible);
    public static Equipment Equipment(EquipmentResponse dto)
    {
        var item = new Equipment(dto.Id, dto.EquipmentCode, dto.Name, dto.Description);
        if (!dto.IsAvailable) item.MarkAsBorrowed();
        return item;
    }
    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    public static OperationResult<T> Failure<T, TResponse>(ApiReply<TResponse> reply) => new(reply.Outcome, reply.Message, Error: reply.Error);
}

public sealed class HttpStudentManagement(ApiTransport api) : IStudentManagementWorkflow
{
    public async Task<QueryResult<IReadOnlyList<Student>>> FindAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        var result = await api.SendAsync<StudentResponse[]>(HttpMethod.Get,
            "api/v1/students?search=" + Uri.EscapeDataString(searchText ?? ""), ct: cancellationToken);
        return result.Success ? new(true, result.Value!.Select(PresentationMapping.Student).ToArray(), "") : new(false, null, result.Message);
    }
    public async Task<OperationResult<Student>> RegisterAsync(RegisterStudentRequest request, CancellationToken cancellationToken = default) =>
        Map(await api.SendAsync<StudentResponse>(HttpMethod.Post, "api/v1/students",
            new StudentInput(request.StudentNumber, request.FullName, PresentationMapping.Optional(request.ContactInformation)), 201, cancellationToken), "Student registered and eligible to borrow.");
    public async Task<OperationResult<Student>> UpdateAsync(UpdateStudentRequest request, CancellationToken cancellationToken = default)
    {
        var stale = await CheckOriginal(request.StudentId, request.Expected, cancellationToken);
        if (stale is not null) return stale;
        return Map(await api.SendAsync<StudentResponse>(HttpMethod.Put, $"api/v1/students/{request.StudentId}",
            new StudentUpdateInput(request.StudentNumber, request.FullName, PresentationMapping.Optional(request.ContactInformation)), ct: cancellationToken,
            expectedRevision: EntityRevision.For(new StudentResponse(request.StudentId, request.Expected.StudentNumber, request.Expected.FullName,
                request.Expected.ContactInformation, request.Expected.IsEligibleToBorrow))), "Student details saved.");
    }
    public async Task<OperationResult<Student>> SetEligibilityAsync(SetStudentEligibilityRequest request, CancellationToken cancellationToken = default)
    {
        var stale = await CheckOriginal(request.StudentId, request.Expected, cancellationToken);
        if (stale is not null) return stale;
        return Map(await api.SendAsync<StudentResponse>(HttpMethod.Patch, $"api/v1/students/{request.StudentId}/eligibility",
            new EligibilityInput(request.IsEligible), ct: cancellationToken,
            expectedRevision: EntityRevision.For(new StudentResponse(request.StudentId, request.Expected.StudentNumber, request.Expected.FullName,
                request.Expected.ContactInformation, request.Expected.IsEligibleToBorrow))), "Student eligibility confirmed. Existing loans and history are preserved.");
    }
    private async Task<OperationResult<Student>?> CheckOriginal(int id, StudentSnapshot expected, CancellationToken ct)
    {
        var current = await api.SendAsync<StudentResponse>(HttpMethod.Get, $"api/v1/students/{id}", ct: ct);
        if (!current.Success) return PresentationMapping.Failure<Student, StudentResponse>(current);
        return StudentSnapshot.From(PresentationMapping.Student(current.Value!)) == expected ? null
            : new(OperationOutcome.Conflict, "The student changed since selection. Refresh and select again.", Error: WorkflowError.StaleState);
    }
    private static OperationResult<Student> Map(ApiReply<StudentResponse> reply, string success) => reply.Success
        ? new(OperationOutcome.Succeeded, success, PresentationMapping.Student(reply.Value!)) : PresentationMapping.Failure<Student, StudentResponse>(reply);
}

public sealed class HttpEquipmentManagement(ApiTransport api) : IEquipmentManagementWorkflow
{
    public async Task<QueryResult<IReadOnlyList<Equipment>>> FindAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        var result = await api.SendAsync<EquipmentResponse[]>(HttpMethod.Get, "api/v1/equipment", ct: cancellationToken);
        if (!result.Success) return new(false, null, result.Message);
        var text = searchText?.Trim() ?? "";
        // API-01 has no equipment-search parameter; filter the fetched catalog for this management picker.
        return new(true, result.Value!.Where(item => item.EquipmentCode.Contains(text, StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).Select(PresentationMapping.Equipment).ToArray(), "");
    }
    public async Task<OperationResult<Equipment>> RegisterAsync(RegisterEquipmentRequest request, CancellationToken cancellationToken = default) =>
        Map(await api.SendAsync<EquipmentResponse>(HttpMethod.Post, "api/v1/equipment",
            new EquipmentInput(request.EquipmentIdentifier, request.Name, PresentationMapping.Optional(request.Description)), 201, cancellationToken), "Equipment registered and available for borrowing.");
    public async Task<OperationResult<Equipment>> UpdateAsync(UpdateEquipmentRequest request, CancellationToken cancellationToken = default)
    {
        var current = await api.SendAsync<EquipmentResponse>(HttpMethod.Get, $"api/v1/equipment/{request.EquipmentId}", ct: cancellationToken);
        if (!current.Success) return PresentationMapping.Failure<Equipment, EquipmentResponse>(current);
        if (EquipmentSnapshot.From(PresentationMapping.Equipment(current.Value!)) != request.Expected)
            return new(OperationOutcome.Conflict, "The equipment changed since selection. Refresh and select again.", Error: WorkflowError.StaleState);
        return Map(await api.SendAsync<EquipmentResponse>(HttpMethod.Put, $"api/v1/equipment/{request.EquipmentId}",
            new EquipmentUpdateInput(request.EquipmentIdentifier, request.Name, PresentationMapping.Optional(request.Description)), ct: cancellationToken,
            expectedRevision: EntityRevision.For(new EquipmentResponse(request.EquipmentId, request.Expected.EquipmentIdentifier, request.Expected.Name,
                request.Expected.Description, request.Expected.IsAvailable))), "Equipment details saved.");
    }
    private static OperationResult<Equipment> Map(ApiReply<EquipmentResponse> reply, string success) => reply.Success
        ? new(OperationOutcome.Succeeded, success, PresentationMapping.Equipment(reply.Value!)) : PresentationMapping.Failure<Equipment, EquipmentResponse>(reply);
}

public sealed class HttpCatalogReader(ApiTransport api) : ICatalogReader
{
    public async Task<QueryResult<CatalogSnapshot>> GetCatalogAsync(bool availableOnly, CancellationToken cancellationToken = default)
    {
        var equipment = api.SendAsync<EquipmentResponse[]>(HttpMethod.Get,
            "api/v1/equipment" + (availableOnly ? "?available=true" : ""), ct: cancellationToken);
        var students = api.SendAsync<StudentResponse[]>(HttpMethod.Get, "api/v1/students", ct: cancellationToken);
        await Task.WhenAll(equipment, students);
        var items = await equipment; var people = await students;
        if (!items.Success || !people.Success) return new(false, null, !items.Success ? items.Message : people.Message);
        return new(true, new(items.Value!.Select(PresentationMapping.Equipment).ToArray(),
            people.Value!.Select(PresentationMapping.Student).ToArray()), "");
    }
}
