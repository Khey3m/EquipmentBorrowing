using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EquipmentBorrowing.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace EquipmentBorrowing.ApiTests;

public sealed class ProtocolApiTests(ITestOutputHelper output)
{
    private const string Loans = "/api/v1/borrowings";

    [ApiPostgresFact, Trait("TC6", "U09")]
    public async Task U09_SearchFiltersSortsAndResets()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        await using var fresh = scenario.CreateDbContext();
        var source = await fresh.Borrowings.AsNoTracking().ToListAsync();
        var camera = source.Single(row => row.EquipmentId == scenario.Equipment["EQ-001"].Id).Id;
        var projector = source.Single(row => row.EquipmentId == scenario.Equipment["EQ-003"].Id).Id;
        var tied = source.Where(row => row.StudentId == scenario.Students["ST-300"].Id).Select(row => row.Id).Order().ToArray();
        Assert.Equal(2, tied.Length);
        var defaults = new[] { camera, projector }.Concat(tied).ToArray();
        var before = await scenario.SnapshotAsync();

        async Task Check(string query, IEnumerable<int> expected)
        {
            var rows = await Read<BorrowingResponse[]>(scenario, HttpMethod.Get, Loans + query, HttpStatusCode.OK);
            Assert.Equal(expected, rows.Select(row => row.Id));
        }

        await Check("?search=%20aDa%20", [camera, projector]);
        await Check("?search=t-30", tied);
        await Check("?search=JeCt", [projector]);
        await Check("?status=All", defaults);
        await Check("?status=Active", new[] { camera }.Concat(tied));
        await Check("?status=Returned", [projector]);
        await Check("?sort=borrowedDate&direction=asc", defaults);
        await Check("?sort=borrowedDate&direction=desc", tied.Concat([projector, camera]));
        var dueTie = new[] { projector }.Concat(tied).Order().ToArray();
        await Check("?sort=expectedReturnDate&direction=asc", new[] { camera }.Concat(dueTie));
        await Check("?sort=expectedReturnDate&direction=desc", dueTie.Append(camera));
        await Check("?search=CaRa&status=Active&sort=expectedReturnDate&direction=desc", tied);
        await Check("?search=ada&status=Active", [camera]);
        await Check("?search=no-match", []);
        await Check("", defaults);
        Assert.Equal(before, await scenario.SnapshotAsync());
        output.WriteLine("Fresh PostgreSQL snapshot unchanged after all search and reset requests.");
    }

    [ApiPostgresFact, Trait("TC6", "E03")]
    public async Task E03_ProtocolErrorsHaveSafeJsonAndNoWrites()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var eligible = scenario.Students["ST-400"].Id;
        var ineligible = scenario.Students["ST-200"].Id;
        var laptop = scenario.Equipment["EQ-002"].Id;
        var tripod = scenario.Equipment["EQ-006"].Id;
        var due = ApiScenario.D.AddDays(2);

        async Task Reject(HttpMethod method, string path, int status, string code, string? json = null)
        {
            var before = await scenario.SnapshotAsync();
            var result = await Read<ApiError>(scenario, method, path, (HttpStatusCode)status, json);
            Assert.Equal(code, result.Code);
            Assert.False(string.IsNullOrWhiteSpace(result.Message));
            foreach (var detail in new[] { "Npgsql", "PostgresException", "System.Text.Json", "stack trace", "password=", "connection string" })
                Assert.DoesNotContain(detail, result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(before, await scenario.SnapshotAsync());
            output.WriteLine("Fresh PostgreSQL snapshot unchanged after rejected request.");
        }

        await Reject(HttpMethod.Post, "/api/v1/students", 400, "invalid_request", "{");
        await Reject(HttpMethod.Post, "/api/v1/students", 400, "invalid_request", """{"studentNumber":"ST-500"}""");
        await Reject(HttpMethod.Post, "/api/v1/equipment", 400, "invalid_request", """{"equipmentCode":8,"name":"Camera"}""");
        await Reject(HttpMethod.Post, Loans, 400, "invalid_request",
            JsonSerializer.Serialize(new BorrowInput(eligible, laptop, ApiScenario.D), JsonSerializerOptions.Web));
        await Reject(HttpMethod.Post, "/api/v1/checkout-requests", 400, "invalid_request",
            JsonSerializer.Serialize(new CheckoutInput(eligible, [laptop, laptop], due), JsonSerializerOptions.Web));
        await Reject(HttpMethod.Post, "/api/v1/checkout-requests", 404, "not_found",
            JsonSerializer.Serialize(new CheckoutInput(int.MaxValue, [laptop, tripod], due), JsonSerializerOptions.Web));
        await Reject(HttpMethod.Get, Loans + "?status=invalid", 400, "invalid_query");
        await Reject(HttpMethod.Get, "/api/v1/no-such-route", 404, "not_found");
        await Reject(HttpMethod.Delete, "/api/v1/equipment", 405, "method_not_allowed");
        await Reject(HttpMethod.Post, Loans, 409, "business_conflict",
            JsonSerializer.Serialize(new BorrowInput(ineligible, laptop, due), JsonSerializerOptions.Web));
    }

    [ApiPostgresFact]
    public async Task OpenApiMatchesTheReviewedContract()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        using var response = await scenario.Client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var reviewed = typeof(ProtocolApiTests).Assembly.GetManifestResourceStream("ReviewedOpenApi.json")!;
        var expected = JsonNode.Parse(reviewed)!;
        var actual = JsonNode.Parse(body)!;
        var count = 0;
        foreach (var (path, methods) in expected["paths"]!.AsObject())
        {
            foreach (var (method, operation) in methods!.AsObject())
            {
                var id = operation?["operationId"]?.GetValue<string>();
                if (id is null || !id.StartsWith("API-", StringComparison.Ordinal)) continue;
                var live = actual["paths"]?[path]?[method];
                Assert.NotNull(live);
                Assert.Equal(id, live["operationId"]!.GetValue<string>());
                foreach (var key in new[] { "parameters", "requestBody", "responses" })
                    Assert.True(JsonNode.DeepEquals(operation![key], live[key]), $"{id} {key} differs from reviewed OpenAPI.");
                count++;
            }
        }
        Assert.Equal(16, count);
        var liveIds = actual["paths"]!.AsObject().SelectMany(path => path.Value!.AsObject())
            .Select(method => method.Value?["operationId"]?.GetValue<string>())
            .Where(id => id?.StartsWith("API-", StringComparison.Ordinal) == true).Order();
        Assert.Equal(Enumerable.Range(1, 16).Select(i => $"API-{i:00}"), liveIds);
        Assert.True(JsonNode.DeepEquals(expected["components"]!["schemas"], actual["components"]!["schemas"]),
            "Live component schemas differ from reviewed OpenAPI.");
        output.WriteLine("Live OpenAPI matched all 16 reviewed operations, including query parameters, request bodies, response statuses and component schemas.");
        output.WriteLine(body);
    }

    private async Task<T> Read<T>(ApiScenario scenario, HttpMethod method, string path, HttpStatusCode status, string? json = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (json is not null)
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await scenario.Client.SendAsync(request);
        output.WriteLine($"{method} {path} {json} => {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return Assert.IsType<T>(await response.Content.ReadFromJsonAsync<T>());
    }
}
