using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EquipmentBorrowing.Api;

internal static class ApiOpenApi
{
    public static Task DescribeQueries(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter is not OpenApiParameter query) continue;
            if (operation.OperationId == "API-14")
            {
                string[]? values = query.Name switch
                {
                    "status" => ["All", "Active", "Returned"],
                    "sort" => ["borrowedDate", "expectedReturnDate"],
                    "direction" => ["asc", "desc"],
                    _ => null
                };
                if (values is not null)
                    query.Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String, Default = JsonValue.Create(values[0]),
                        Enum = values.Select(value => (JsonNode)JsonValue.Create(value)!).ToList()
                    };
                if (query.Name == "search") query.Description = "Trimmed case-insensitive partial match on student name/number or equipment name; literal %, _ and backslash.";
            }
            if (operation.OperationId == "API-01" && query.Name == "available")
            {
                query.Description = "true selects available items only; false or omission returns all equipment.";
                query.Schema = new OpenApiSchema { Type = JsonSchemaType.Boolean, Default = JsonValue.Create(false) };
            }
        }
        if (operation.OperationId is "API-04" or "API-08" or "API-10"
            && operation.Responses?.TryGetValue("201", out var created) == true && created is OpenApiResponse response)
        {
            response.Headers ??= new Dictionary<string, IOpenApiHeader>();
            response.Headers["Location"] = new OpenApiHeader
            {
                Description = "Relative URL of the created resource's GET endpoint.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri-reference" }
            };
        }
        return Task.CompletedTask;
    }
}
