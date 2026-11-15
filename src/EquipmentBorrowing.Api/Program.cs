using EquipmentBorrowing.Api;
using EquipmentBorrowing.Contracts;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);
// Explicit --urls / ASPNETCORE_URLS / configuration override the local default.
if (builder.Configuration["urls"] is null) builder.WebHost.UseUrls("http://127.0.0.1:5085");
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
});
builder.Services.AddOpenApi();
builder.Services.AddBorrowingBackend();

var app = builder.Build();
// No developer exception page or raw exception logging: database errors can include secrets.
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
    catch (BadHttpRequestException)
    {
        if (context.Response.HasStarted) throw;
        context.Response.Clear(); context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new ApiError("invalid_request", "The request syntax or fields are invalid."));
    }
    catch (Exception error)
    {
        if (context.Response.HasStarted) throw;
        app.Logger.LogError("Request {TraceId} failed with {ExceptionType}.", context.TraceIdentifier, error.GetType().Name);
        context.Response.Clear(); context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new ApiError("server_error", "The server could not complete the request. Verify current state before retrying a change."));
    }
});
app.UseStatusCodePages(async status =>
{
    var response = status.HttpContext.Response;
    var code = response.StatusCode;
    await response.WriteAsJsonAsync(new ApiError(code switch
    {
        400 => "invalid_request", 404 => "not_found", 405 => "method_not_allowed", _ => "http_error"
    }, code switch
    {
        400 => "The request syntax or fields are invalid.",
        404 => "The requested resource or route was not found.",
        405 => "This HTTP method is not supported for this route.",
        _ => "The request could not be completed."
    }));
});
app.MapOpenApi();
app.MapStudentEndpoints();
app.MapGet("/api/v1/status", () => new ApiStatus("CampusEQ API", "v1", "Session 2 student endpoints; remaining business endpoints pending"))
    .WithName("ApiStatus").WithSummary("Host liveness only; does not establish database readiness.")
    .Produces<ApiStatus>(200).Produces<ApiError>(405).Produces<ApiError>(500);
app.Run();

public partial class Program { }
