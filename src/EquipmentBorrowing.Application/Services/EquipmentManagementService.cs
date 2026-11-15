using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed class EquipmentManagementService(IEquipmentManagementSessionFactory sessions,
    IEquipmentRepository reads, IWorkflowFailures failures) : IEquipmentManagementWorkflow
{
    public async Task<QueryResult<IReadOnlyList<Equipment>>> FindAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        try
        {
            var text = searchText?.Trim() ?? "";
            var items = await reads.GetAllAsync(cancellationToken);
            var results = items.Where(s => text.Length == 0 ||
                s.EquipmentIdentifier.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name).ThenBy(s => s.Id).ToList();
            return new(true, results, "");
        }
        catch (Exception) { return new(false, null, "Unable to load equipment. Restore the connection and search or refresh again."); }
    }

    public Task<OperationResult<Equipment>> RegisterAsync(RegisterEquipmentRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (repository, ct) =>
        {
            var equipment = new Equipment(0, request.EquipmentIdentifier, request.Name, request.Description);
            if (await repository.IdentifierExistsAsync(equipment.EquipmentIdentifier, cancellationToken: ct))
                return new(OperationOutcome.Rejected, "That equipment identifier is already registered.", Error: WorkflowError.DuplicateIdentifier);
            await repository.AddAsync(equipment, ct);
            return new(OperationOutcome.Succeeded, "Equipment registered and available for borrowing.", equipment);
        }, cancellationToken);

    public Task<OperationResult<Equipment>> UpdateAsync(UpdateEquipmentRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (repository, ct) =>
        {
            var equipment = await repository.GetByIdAsync(request.EquipmentId, ct);
            if (equipment is null)
                return new(OperationOutcome.Conflict, "The equipment was not found.", Error: WorkflowError.NotFound);
            if (EquipmentSnapshot.From(equipment) != request.Expected)
                return new(OperationOutcome.Conflict, "The equipment changed since selection. Refresh and select again.", Error: WorkflowError.StaleState);
            equipment.UpdateInformation(request.EquipmentIdentifier, request.Name, request.Description);
            if (await repository.IdentifierExistsAsync(equipment.EquipmentIdentifier, equipment.Id, ct))
                return new(OperationOutcome.Rejected, "That equipment identifier belongs to another item.", Error: WorkflowError.DuplicateIdentifier);
            await repository.UpdateAsync(equipment, ct);
            return new(OperationOutcome.Succeeded, "Equipment details updated; identity and borrowing history preserved.", equipment);
        }, cancellationToken);

    private async Task<OperationResult<Equipment>> ExecuteAsync(
        Func<IEquipmentManagementRepository, CancellationToken, Task<OperationResult<Equipment>>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var session = await sessions.OpenAsync(cancellationToken);
            var result = await action(session.Equipment, cancellationToken);
            if (result.Outcome == OperationOutcome.Succeeded) await session.CommitAsync(cancellationToken);
            return result;
        }
        catch (ArgumentException) { return new(OperationOutcome.Rejected, "Equipment identifier and name are required; correct the supplied fields.", Error: WorkflowError.InvalidInput); }
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
