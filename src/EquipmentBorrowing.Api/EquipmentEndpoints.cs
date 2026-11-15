using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Api;

public static class EquipmentEndpoints
{
    public static void MapEquipmentEndpoints(this WebApplication app)
    {
        var equipment = app.MapGroup("/api/v1/equipment").WithTags("Equipment");
        equipment.MapGet("", async (bool? available, HttpRequest request,
            EquipmentManagementService service, CancellationToken ct) =>
        {
            if (request.Query.Any(q => q.Key != "available" || q.Value.Count != 1))
                return Error(400, "invalid_query", "Use one optional available Boolean value.");
            var result = await service.FindAsync(null, ct);
            return result.Success
                ? Results.Ok(result.Value!.Where(item => available != true || item.IsAvailable).Select(Response).ToArray())
                : Error(500, "server_error", result.Message);
        }).WithName("API-01").WithSummary("List equipment; available=true selects available items only")
            .Produces<EquipmentResponse[]>(200).Produces<ApiError>(400).Produces<ApiError>(500);

        equipment.MapGet("/{id}", async (int id, IEquipmentRepository repository, CancellationToken ct) =>
        {
            var item = await repository.GetByIdAsync(id, ct);
            return item is null ? Error(404, "not_found", "The equipment was not found.") : Results.Ok(Response(item));
        }).WithName("API-07").WithSummary("Read one equipment item")
            .Produces<EquipmentResponse>(200).Produces<ApiError>(400).Produces<ApiError>(404).Produces<ApiError>(500);

        equipment.MapPost("", async (EquipmentInput input, EquipmentManagementService service, CancellationToken ct) =>
        {
            if (Invalid(input.EquipmentCode, input.Name)) return InvalidFields();
            var result = await service.RegisterAsync(new(input.EquipmentCode!, input.Name!, input.Description), ct);
            return result.Outcome == OperationOutcome.Succeeded
                ? Results.Created($"/api/v1/equipment/{result.Value!.Id}", Response(result.Value)) : MapResult(result);
        }).WithName("API-08").WithSummary("Register equipment initially available")
            .Produces<EquipmentResponse>(201).Produces<ApiError>(400).Produces<ApiError>(409).Produces<ApiError>(500);

        equipment.MapPut("/{id}", async (int id, EquipmentUpdateInput input, HttpRequest request, IEquipmentRepository repository,
            EquipmentManagementService service, CancellationToken ct) =>
        {
            if (Invalid(input.EquipmentCode, input.Name)) return InvalidFields();
            var original = await repository.GetByIdAsync(id, ct);
            if (original is null) return Error(404, "not_found", "The equipment was not found.");
            if (request.Headers.TryGetValue(EntityRevision.Header, out var revision)
                && (revision.Count != 1 || revision[0] != EntityRevision.For(Response(original))))
                return Error(409, "conflict", "The equipment changed since selection. Refresh and select again.");
            return MapResult(await service.UpdateAsync(new(id, input.EquipmentCode!, input.Name!, input.Description,
                EquipmentSnapshot.From(original)), ct));
        }).WithName("API-09").WithSummary("Replace descriptive fields while preserving identity and availability")
            .Produces<EquipmentResponse>(200).Produces<ApiError>(400).Produces<ApiError>(404)
            .Produces<ApiError>(409).Produces<ApiError>(500);
    }
    private static bool Invalid(string? code, string? name) => string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name);
    private static IResult InvalidFields() => Error(400, "invalid_request", "equipmentCode and name are required and cannot be blank.");
    private static EquipmentResponse Response(Equipment item) => new(item.Id, item.EquipmentIdentifier,
        item.Name, item.Description, item.IsAvailable);
    private static IResult MapResult(OperationResult<Equipment> result) => result.Outcome == OperationOutcome.Succeeded && result.Value is not null
        ? Results.Ok(Response(result.Value)) : ApiFailures.From(result.Error, result.Message);
    private static IResult Error(int status, string code, string message) => Results.Json(new ApiError(code, message), statusCode: status);
}
