using System.Net.Http.Json;
using System.Text.Json;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;

namespace EquipmentBorrowing.Http;

public sealed record ApiReply<T>(bool Success, T? Value, int? Status, OperationOutcome Outcome, WorkflowError Error, string Message);

public sealed class ApiTransport(HttpClient client)
{
    public const string BaseUrlVariable = "EQUIPMENT_BORROWING_API_BASE_URL";
    public static HttpClient CreateClient()
    {
        var configured = Environment.GetEnvironmentVariable(BaseUrlVariable) ?? "http://127.0.0.1:5085/";
        var valid = Uri.TryCreate(configured, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" && uri.AbsolutePath == "/"
            && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        { BaseAddress = valid ? uri : null, Timeout = TimeSpan.FromSeconds(15) };
    }

    public async Task<ApiReply<T>> SendAsync<T>(HttpMethod method, string path, object? body = null,
        int expectedStatus = 200, CancellationToken ct = default, string? expectedRevision = null)
    {
        if (client.BaseAddress is null)
            return new(false, default, null, OperationOutcome.Failed, WorkflowError.InvalidInput,
                "The API base URL is invalid. Configure an HTTP/HTTPS origin without credentials, query or fragment.");
        var mutation = method != HttpMethod.Get;
        int? status = null;
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (expectedRevision is not null) request.Headers.Add(EntityRevision.Header, expectedRevision);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: ct);
                if (error is null || string.IsNullOrWhiteSpace(error.Code) || string.IsNullOrWhiteSpace(error.Message))
                    return Incomplete<T>(status, mutation);
                if (error.Code == "outcome_unknown")
                    return new(false, default, status, OperationOutcome.Unknown, WorkflowError.UnknownOutcome,
                        "API reported an unknown outcome. Refresh and verify before deciding whether to submit again.");
                var kind = status switch
                {
                    400 => WorkflowError.InvalidInput, 404 => WorkflowError.NotFound,
                    409 => error.Code == "duplicate_identifier" ? WorkflowError.DuplicateIdentifier : WorkflowError.ConcurrentConflict,
                    _ => WorkflowError.DatabaseFailure
                };
                var outcome = status switch { 400 => OperationOutcome.Rejected, 404 or 409 => OperationOutcome.Conflict, _ => OperationOutcome.Failed };
                // Render known validation feedback, but never arbitrary server HTML/exception text.
                var message = status >= 500 ? $"API returned HTTP {status}: server/database failure. Restore service and refresh before retrying."
                    : status is 400 or 404 or 409 ? error.Message : $"API returned HTTP {status}. Refresh and review the request.";
                return new(false, default, status, outcome, kind, message);
            }
            if (status != expectedStatus) return Incomplete<T>(status, mutation);
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return !Valid(value) ? Incomplete<T>(status, mutation)
                : new(true, value, status, OperationOutcome.Succeeded, WorkflowError.None, "Request completed.");
        }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException or JsonException or NotSupportedException or IOException)
        {
            return Incomplete<T>(status, mutation);
        }
    }

    private static bool Valid(object? value) => value switch
    {
        null => false,
        StudentResponse student => student.Id > 0 && !string.IsNullOrWhiteSpace(student.StudentNumber) && !string.IsNullOrWhiteSpace(student.FullName),
        EquipmentResponse equipment => equipment.Id > 0 && !string.IsNullOrWhiteSpace(equipment.EquipmentCode) && !string.IsNullOrWhiteSpace(equipment.Name),
        Array array => array.Cast<object?>().All(Valid),
        _ => true
    };

    private static ApiReply<T> Incomplete<T>(int? status, bool mutation)
    {
        var message = status.HasValue ? $"API returned HTTP {status}, but a complete valid response was not received."
            : "No HTTP response was received from the API. Check its address and whether it is running.";
        if (mutation) message += " The mutation outcome is unknown. Refresh and verify saved state before deciding whether to submit again.";
        return new(false, default, status, mutation ? OperationOutcome.Unknown : OperationOutcome.Failed,
            mutation ? WorkflowError.UnknownOutcome : WorkflowError.DatabaseFailure, message);
    }
}
