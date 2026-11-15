using System.Net;
using System.Text;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Http;
using Xunit;

namespace EquipmentBorrowing.UiTests;

public class HttpClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        { Calls++; return action(request); }
    }
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
        { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new("http://localhost:5085/") };

    [Fact]
    public async Task ReadTransportFailureAndReceived500AreDistinctAndSafe()
    {
        using var unreachable = Client(new Handler(_ => throw new HttpRequestException("private transport detail")));
        var noResponse = await new ApiTransport(unreachable).SendAsync<StudentResponse[]>(HttpMethod.Get, "api/v1/students", ct: TestContext.Current.CancellationToken);
        Assert.Null(noResponse.Status); Assert.Contains("No HTTP response", noResponse.Message);
        Assert.Equal(OperationOutcome.Failed, noResponse.Outcome);
        using var server = Client(new Handler(_ => Task.FromResult(Json(HttpStatusCode.InternalServerError,
            "{\"code\":\"server_error\",\"message\":\"private database detail\"}"))));
        var received = await new ApiTransport(server).SendAsync<StudentResponse[]>(HttpMethod.Get, "api/v1/students", ct: TestContext.Current.CancellationToken);
        Assert.Equal(500, received.Status); Assert.Contains("HTTP 500", received.Message);
        Assert.DoesNotContain("private", received.Message); Assert.DoesNotContain("private", noResponse.Message);
    }

    [Fact]
    public async Task LostMutationResponseBlocksWritesUntilFreshReadWithoutReplay()
    {
        var handler = new Handler(request => request.Method == HttpMethod.Post
            ? throw new HttpRequestException("lost response")
            : Task.FromResult(Json(HttpStatusCode.OK, "[]")));
        using var client = Client(handler);
        var vm = new StudentsViewModel(new HttpStudentManagement(new ApiTransport(client)));
        await vm.InitializeAsync(); vm.StudentNumber = "ST-HTTP"; vm.FullName = "Synthetic Student";
        await vm.RegisterCommand.ExecuteAsync(null);
        Assert.True(vm.NeedsVerification); Assert.False(vm.CanEdit);
        Assert.False(vm.RegisterCommand.CanExecute(null)); Assert.Contains("unknown", vm.StatusMessage);
        Assert.Equal(2, handler.Calls); // Initial read and exactly one attempted POST.
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.False(vm.NeedsVerification); Assert.True(vm.CanEdit); Assert.Equal(3, handler.Calls);
        Assert.Contains("Review", vm.StatusMessage);
    }

    [Fact]
    public async Task SlowReadReturnsPendingTaskAndExposesBusyState()
    {
        var release = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = Client(new Handler(_ => release.Task));
        var vm = new StudentsViewModel(new HttpStudentManagement(new ApiTransport(client)));
        var loading = vm.InitializeAsync();
        Assert.False(loading.IsCompleted); Assert.True(vm.IsBusy); Assert.False(vm.CanEdit);
        release.SetResult(Json(HttpStatusCode.OK, "[]"));
        await loading;
        Assert.False(vm.IsBusy); Assert.True(vm.IsReady); Assert.Empty(vm.Students);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("{\"id\":1,\"studentNumber\":\"S1\",\"fullName\":\"Student\"}")]
    public async Task MalformedSuccessfulMutationIsUnknownNotFalseSuccess(string body)
    {
        var handler = new Handler(_ => Task.FromResult(Json(HttpStatusCode.Created, body)));
        using var client = Client(handler);
        var result = await new ApiTransport(client).SendAsync<StudentResponse>(HttpMethod.Post, "api/v1/students",
            new StudentInput("S1", "Student", null), 201, TestContext.Current.CancellationToken);
        Assert.False(result.Success); Assert.Equal(OperationOutcome.Unknown, result.Outcome);
        Assert.Equal(201, result.Status); Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ChangedSelectionIsRefreshedBeforeAnyPutIsSent()
    {
        var handler = new Handler(_ => Task.FromResult(Json(HttpStatusCode.OK,
            "{\"id\":1,\"studentNumber\":\"S1\",\"fullName\":\"Changed elsewhere\",\"contact\":null,\"isEligible\":true}")));
        using var client = Client(handler);
        var result = await new HttpStudentManagement(new ApiTransport(client)).UpdateAsync(
            new(1, "S1", "My edit", null, new("S1", "Original", null, true)), TestContext.Current.CancellationToken);
        Assert.Equal(WorkflowError.StaleState, result.Error); Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ServerUnknownOutcomeIsNotTreatedAsOrdinary500()
    {
        using var client = Client(new Handler(_ => Task.FromResult(Json(HttpStatusCode.InternalServerError,
            "{\"code\":\"outcome_unknown\",\"message\":\"Verify saved state\"}"))));
        var result = await new ApiTransport(client).SendAsync<StudentResponse>(HttpMethod.Post, "api/v1/students",
            new StudentInput("S1", "Student", null), 201, TestContext.Current.CancellationToken);
        Assert.Equal(500, result.Status); Assert.Equal(OperationOutcome.Unknown, result.Outcome);
    }
}
