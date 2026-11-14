using EquipmentBorrowing.Application.Interfaces;
using Npgsql;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public sealed class PostgresWorkflowFailures : IWorkflowFailures
{
    public OperationOutcome Classify(Exception error)
    {
        for (Exception? current = error; current is not null; current = current.InnerException)
        {
            if (current is CommitOutcomeUnknownException) return OperationOutcome.Unknown;
            if (current is PostgresException { SqlState: "08007" or "40003" }) return OperationOutcome.Unknown;
            if (current is PostgresException { SqlState: "40001" or "40P01" or "23505" or "23503" })
                return OperationOutcome.Conflict;
        }
        return OperationOutcome.Failed;
    }
}
