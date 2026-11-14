namespace EquipmentBorrowing.Application.Interfaces;

public interface IWorkflowFailures
{
    OperationOutcome Classify(Exception error);
}

public static class WorkflowFeedback
{
    public static string Message(OperationOutcome outcome) => outcome switch
    {
        OperationOutcome.Unknown => "The commit outcome is unknown. Reconnect and refresh before deciding whether to submit again.",
        OperationOutcome.Conflict => "The records changed or another request conflicted. Refresh and review before trying again.",
        _ => "The database operation could not be completed. Refresh after restoring the connection before trying again."
    };
}
