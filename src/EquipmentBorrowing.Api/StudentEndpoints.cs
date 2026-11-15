using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Api;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(this WebApplication app)
    {
        var students = app.MapGroup("/api/v1/students").WithTags("Students");
        students.MapGet("", async (string? search, HttpRequest request, StudentManagementService service, CancellationToken ct) =>
        {
            if (request.Query.Any(q => q.Key != "search" || q.Value.Count != 1))
                return Error(400, "invalid_query", "Use one optional search value.");
            var result = await service.FindAsync(search, ct);
            return result.Success ? Results.Ok(result.Value!.Select(Response).ToArray())
                : Error(500, "server_error", result.Message);
        }).WithName("API-02").WithSummary("Find students by name or number")
            .Produces<StudentResponse[]>(200).Produces<ApiError>(400).Produces<ApiError>(500);

        students.MapGet("/{id}", async (int id, IStudentRepository repository, CancellationToken ct) =>
        {
            var student = await repository.GetByIdAsync(id, ct);
            return student is null ? Error(404, "not_found", "The student was not found.") : Results.Ok(Response(student));
        }).WithName("API-03").WithSummary("Read one student")
            .Produces<StudentResponse>(200).Produces<ApiError>(400).Produces<ApiError>(404).Produces<ApiError>(500);

        students.MapPost("", async (StudentInput input, StudentManagementService service, CancellationToken ct) =>
        {
            if (Invalid(input.StudentNumber, input.FullName)) return InvalidFields();
            var result = await service.RegisterAsync(new(input.StudentNumber!, input.FullName!, input.Contact), ct);
            return result.Outcome == OperationOutcome.Succeeded
                ? Results.Created($"/api/v1/students/{result.Value!.Id}", Response(result.Value)) : MapResult(result);
        }).WithName("API-04").WithSummary("Register an eligible student")
            .Produces<StudentResponse>(201).Produces<ApiError>(400).Produces<ApiError>(409).Produces<ApiError>(500);

        students.MapPut("/{id}", async (int id, StudentUpdateInput input, IStudentRepository repository,
            StudentManagementService service, CancellationToken ct) =>
        {
            if (Invalid(input.StudentNumber, input.FullName)) return InvalidFields();
            var original = await repository.GetByIdAsync(id, ct);
            if (original is null) return Error(404, "not_found", "The student was not found.");
            return MapResult(await service.UpdateAsync(new(id, input.StudentNumber!, input.FullName!, input.Contact,
                StudentSnapshot.From(original)), ct));
        }).WithName("API-05").WithSummary("Replace editable student fields; null clears contact")
            .Produces<StudentResponse>(200).Produces<ApiError>(400).Produces<ApiError>(404)
            .Produces<ApiError>(409).Produces<ApiError>(500);

        students.MapPatch("/{id}/eligibility", async (int id, EligibilityInput input, IStudentRepository repository,
            StudentManagementService service, CancellationToken ct) =>
        {
            if (input.IsEligible is null) return Error(400, "invalid_request", "isEligible must be a Boolean value.");
            var original = await repository.GetByIdAsync(id, ct);
            if (original is null) return Error(404, "not_found", "The student was not found.");
            return MapResult(await service.SetEligibilityAsync(new(id, input.IsEligible.Value,
                StudentSnapshot.From(original)), ct));
        }).WithName("API-06").WithSummary("Permit or restrict borrowing; repeated state is a no-op")
            .Produces<StudentResponse>(200).Produces<ApiError>(400).Produces<ApiError>(404)
            .Produces<ApiError>(409).Produces<ApiError>(500);
    }

    private static bool Invalid(string? number, string? name) =>
        string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(name);
    private static IResult InvalidFields() => Error(400, "invalid_request", "studentNumber and fullName are required and cannot be blank.");
    private static StudentResponse Response(Student student) => new(student.Id, student.StudentNumber,
        student.FullName, student.ContactInformation, student.IsEligibleToBorrow);
    private static IResult MapResult(OperationResult<Student> result)
    {
        if ((result.Outcome == OperationOutcome.Succeeded || result.Error == WorkflowError.NoChange) && result.Value is not null)
            return Results.Ok(Response(result.Value));
        return ApiFailures.From(result.Error, result.Message);
    }

    private static IResult Error(int status, string code, string message) =>
        Results.Json(new ApiError(code, message), statusCode: status);
}
