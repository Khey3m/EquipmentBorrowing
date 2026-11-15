using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed class StudentManagementService(IStudentManagementSessionFactory sessions,
    IStudentRepository reads, IWorkflowFailures failures) : IStudentManagementWorkflow
{
    public async Task<QueryResult<IReadOnlyList<Student>>> FindAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        try
        {
            var text = searchText?.Trim() ?? "";
            var students = await reads.GetAllAsync(cancellationToken);
            var results = students.Where(s => text.Length == 0 ||
                s.StudentNumber.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                s.FullName.Contains(text, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.FullName).ThenBy(s => s.Id).ToList();
            return new(true, results, "");
        }
        catch (Exception) { return new(false, null, "Unable to load students. Restore the connection and search or refresh again."); }
    }

    public Task<OperationResult<Student>> RegisterAsync(RegisterStudentRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (repository, ct) =>
        {
            var student = new Student(0, request.StudentNumber, request.FullName, request.ContactInformation);
            if (await repository.NumberExistsAsync(student.StudentNumber, cancellationToken: ct))
                return new(OperationOutcome.Rejected, "That institutional student number is already registered.", Error: WorkflowError.DuplicateIdentifier);
            await repository.AddAsync(student, ct);
            return new(OperationOutcome.Succeeded, "Student registered and eligible to borrow.", student);
        }, cancellationToken);

    public Task<OperationResult<Student>> UpdateAsync(UpdateStudentRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (repository, ct) =>
        {
            var student = await repository.GetByIdAsync(request.StudentId, ct);
            if (student is null)
                return new(OperationOutcome.Conflict, "The student was not found.", Error: WorkflowError.NotFound);
            if (StudentSnapshot.From(student) != request.Expected)
                return new(OperationOutcome.Conflict, "The student changed since selection. Refresh and select again.", Error: WorkflowError.StaleState);
            student.UpdateInformation(request.StudentNumber, request.FullName, request.ContactInformation);
            if (await repository.NumberExistsAsync(student.StudentNumber, student.Id, ct))
                return new(OperationOutcome.Rejected, "That institutional student number belongs to another student.", Error: WorkflowError.DuplicateIdentifier);
            await repository.UpdateAsync(student, ct);
            return new(OperationOutcome.Succeeded, "Student details updated; identity and borrowing history preserved.", student);
        }, cancellationToken);

    public Task<OperationResult<Student>> SetEligibilityAsync(SetStudentEligibilityRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (repository, ct) =>
        {
            var student = await repository.GetByIdAsync(request.StudentId, ct);
            if (student is null)
                return new(OperationOutcome.Conflict, "The student was not found.", Error: WorkflowError.NotFound);
            if (StudentSnapshot.From(student) != request.Expected)
                return new(OperationOutcome.Conflict, "The student changed since selection. Refresh and select again.", Error: WorkflowError.StaleState);
            if (!student.SetEligibility(request.IsEligible))
                return new(OperationOutcome.Rejected, "The student already has the requested eligibility; no changes were made.", student, WorkflowError.NoChange);
            await repository.UpdateAsync(student, ct);
            return new(OperationOutcome.Succeeded, request.IsEligible
                ? "Student permitted to borrow. Existing records are unchanged."
                : "Student restricted from new borrowing. Existing loans remain and can still be returned.", student);
        }, cancellationToken);

    private async Task<OperationResult<Student>> ExecuteAsync(
        Func<IStudentManagementRepository, CancellationToken, Task<OperationResult<Student>>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var session = await sessions.OpenAsync(cancellationToken);
            var result = await action(session.Students, cancellationToken);
            if (result.Outcome == OperationOutcome.Succeeded) await session.CommitAsync(cancellationToken);
            return result;
        }
        catch (ArgumentException) { return new(OperationOutcome.Rejected, "Student number and full name are required; correct the supplied fields.", Error: WorkflowError.InvalidInput); }
        catch (Exception error)
        {
            var outcome = error is CommitOutcomeUnknownException ? OperationOutcome.Unknown : failures.Classify(error);
            return new(outcome, WorkflowFeedback.Message(outcome), Error: outcome switch
            {
                OperationOutcome.Unknown => WorkflowError.UnknownOutcome,
                OperationOutcome.Conflict => WorkflowError.ConcurrentConflict,
                _ => WorkflowError.DatabaseFailure
            });
        }
    }
}
