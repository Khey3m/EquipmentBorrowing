using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;

namespace EquipmentBorrowing.Api;

internal static class ApiFailures
{
    public static IResult From(WorkflowError error, string message)
    {
        var (status, code) = error switch
        {
            WorkflowError.InvalidInput => (400, "invalid_request"),
            WorkflowError.NotFound => (404, "not_found"),
            WorkflowError.DuplicateIdentifier => (409, "duplicate_identifier"),
            WorkflowError.StaleState or WorkflowError.ConcurrentConflict => (409, "conflict"),
            WorkflowError.UnknownOutcome => (500, "outcome_unknown"),
            _ => (500, "server_error")
        };
        return Results.Json(new ApiError(code, message), statusCode: status);
    }
}
