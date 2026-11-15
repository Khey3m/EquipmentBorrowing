using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed class MultiItemCheckoutService(IBorrowingSessionFactory sessions, ICampusClock clock,
    IWorkflowFailures failures, ICheckoutWriteObserver? observer = null) : IMultiCheckoutWorkflow
{
    public async Task<OperationResult<IReadOnlyList<Borrowing>>> ExecuteAsync(MultiItemCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Snapshot caller input before the first await; never deduplicate or omit invalid selections.
            var ids = request.EquipmentIds?.ToArray() ?? [];
            await using var session = await sessions.OpenAsync(cancellationToken);
            var checkout = clock.Today;
            var validation = await CheckoutValidation.ValidateAsync(session.Students, session.Equipment, session.Borrowings,
                request.StudentId, ids, request.ExpectedReturnDate, checkout, 2, cancellationToken);
            if (validation.Value is null) return new(validation.Outcome, validation.Message, Error: validation.Error);

            // Every item has been validated before the first mutation/write.
            var loans = new List<Borrowing>();
            foreach (var item in validation.Value.Items)
            {
                var loan = new Borrowing(validation.Value.Student.Id, item.Id, checkout, request.ExpectedReturnDate);
                item.MarkAsBorrowed();
                await session.Equipment.UpdateAsync(item, cancellationToken);
                await session.Borrowings.AddAsync(loan, cancellationToken);
                loans.Add(loan);
                if (observer is not null) await observer.AfterItemPersistedAsync(loans.Count, cancellationToken);
            }
            await session.CommitAsync(cancellationToken);
            return new(OperationOutcome.Succeeded, $"Checked out {loans.Count} items together.", loans);
        }
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
