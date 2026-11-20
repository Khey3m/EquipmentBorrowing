using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Http;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace EquipmentBorrowing.ApiTests;

public sealed class RecoveryApiTests(ITestOutputHelper output)
{
    private const string Checkout = "/api/v1/checkout-requests";

    [ApiPostgresFact, Trait("TC6", "E04"), Trait("TC6", "E07b")]
    public async Task AfterWriteFailureRollsBackAndClientReceivesSafe500()
    {
        var fault = new FailAfterFirstWrite();
        await using var scenario = await ApiScenario.CreateAsync(output, fault);
        var before = await scenario.SnapshotAsync();
        using var handler = new ObserveCheckout(scenario.CreateHandler(), loseResponse: false);
        using var client = new HttpClient(handler) { BaseAddress = scenario.Client.BaseAddress };
        var result = await new ApiTransport(client).SendAsync<CheckoutResponse>(HttpMethod.Post, Checkout, Input(scenario), 201);

        Assert.Equal(new[] { 1 }, fault.ObservedCounts);
        Assert.Equal(1, handler.Attempts);
        Assert.Equal(HttpStatusCode.InternalServerError, handler.ServerStatus);
        var error = Assert.IsType<ApiError>(handler.ServerError);
        Assert.Equal("server_error", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        Assert.DoesNotContain(FailAfterFirstWrite.Marker, error.Message);
        Assert.False(result.Success);
        Assert.Equal(500, result.Status);
        Assert.Equal(OperationOutcome.Failed, result.Outcome);
        Assert.Equal(WorkflowError.DatabaseFailure, result.Error);
        Assert.Contains("HTTP 500", result.Message);
        Assert.DoesNotContain(FailAfterFirstWrite.Marker, result.Message);
        var after = await scenario.SnapshotAsync();
        Assert.Equal(before, after);
        output.WriteLine($"After-write observer reached after item 1. HTTP 500: {error.Code} / {error.Message}. Client: {result.Message}");
        output.WriteLine($"PostgreSQL before: {before}\nPostgreSQL after: {after}");
    }

    [Fact, Trait("TC6", "E07a")]
    public async Task UnreachableReadHasNoHttpStatus()
    {
        using var handler = new Unreachable();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var result = await new ApiTransport(client).SendAsync<EquipmentResponse[]>(HttpMethod.Get, "/api/v1/equipment");
        Assert.Equal(1, handler.Attempts);
        Assert.False(result.Success);
        Assert.Null(result.Status);
        Assert.Equal(OperationOutcome.Failed, result.Outcome);
        Assert.Contains("No HTTP response", result.Message);
        output.WriteLine(result.Message);
    }

    [ApiPostgresFact, Trait("TC6", "E07c")]
    public async Task LostCheckoutResponseIsUnknownWithoutReplayAndFreshReadFindsBothLoans()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var input = Input(scenario);
        var before = await scenario.SnapshotAsync();
        using var handler = new ObserveCheckout(scenario.CreateHandler(), loseResponse: true);
        using var client = new HttpClient(handler) { BaseAddress = scenario.Client.BaseAddress };
        var result = await new ApiTransport(client).SendAsync<CheckoutResponse>(HttpMethod.Post, Checkout, input, 201);

        Assert.Equal(HttpStatusCode.Created, handler.ServerStatus);
        Assert.False(result.Success);
        Assert.Null(result.Status);
        Assert.Null(result.Value);
        Assert.Equal(OperationOutcome.Unknown, result.Outcome);
        Assert.Equal(WorkflowError.UnknownOutcome, result.Error);
        Assert.Contains("verify saved state", result.Message);
        Assert.Equal(1, handler.Attempts);

        await using var fresh = scenario.CreateDbContext();
        var loans = await fresh.Borrowings.AsNoTracking().Where(row => row.StudentId == input.StudentId).ToListAsync();
        Assert.Equal(input.EquipmentIds!.Order(), loans.Select(row => row.EquipmentId).Order());
        Assert.Equal(2, loans.Select(row => row.Id).Distinct().Count());
        Assert.All(loans, row =>
        {
            Assert.Equal(BorrowingStatus.Active, row.Status);
            Assert.Equal(ApiScenario.D, row.BorrowedAt);
            Assert.Equal(input.ExpectedReturnDate, row.ExpectedReturnAt);
        });
        Assert.Equal(2, await fresh.Equipment.CountAsync(row => input.EquipmentIds.Contains(row.Id) && !row.IsAvailable));
        Assert.Equal(6, await fresh.Borrowings.CountAsync());
        output.WriteLine($"Server committed and returned 201. Test handler dropped the response. One POST, client outcome Unknown: {result.Message}");
        output.WriteLine($"PostgreSQL before: {before}\nPostgreSQL after: {await scenario.SnapshotAsync()}");
    }

    private static CheckoutInput Input(ApiScenario scenario) => new(scenario.Students["ST-400"].Id,
        [scenario.Equipment["EQ-002"].Id, scenario.Equipment["EQ-006"].Id], ApiScenario.D.AddDays(2));

    private sealed class FailAfterFirstWrite : ICheckoutWriteObserver
    {
        public const string Marker = "session7-private-test-failure";
        public List<int> ObservedCounts { get; } = [];

        public Task AfterItemPersistedAsync(int persistedItemCount, CancellationToken cancellationToken)
        {
            ObservedCounts.Add(persistedItemCount);
            throw new IOException(Marker);
        }
    }

    private sealed class Unreachable : HttpMessageHandler
    {
        public int Attempts { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts++;
            throw new HttpRequestException(HttpRequestError.ConnectionError, "Controlled unreachable API.");
        }
    }

    private sealed class ObserveCheckout(HttpMessageHandler inner, bool loseResponse) : DelegatingHandler(inner)
    {
        public int Attempts { get; private set; }
        public HttpStatusCode? ServerStatus { get; private set; }
        public ApiError? ServerError { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts++;
            var response = await base.SendAsync(request, cancellationToken);
            ServerStatus = response.StatusCode;
            if (loseResponse)
            {
                response.Dispose();
                throw new HttpRequestException(HttpRequestError.ConnectionError, "Controlled loss of the server response.");
            }
            if (!response.IsSuccessStatusCode)
                ServerError = JsonSerializer.Deserialize<ApiError>(
                    await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptions.Web);
            return response;
        }
    }
}
