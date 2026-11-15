using System.Net;
using System.Net.Http.Json;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Http;
using Xunit;

namespace EquipmentBorrowing.UiTests;

public class BorrowingHttpClientTests
{
    private static readonly DateOnly Today = new(2026, 11, 15);
    private sealed class Clock : ICampusClock { public DateOnly Today => BorrowingHttpClientTests.Today; }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        public int Posts;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        { if (request.Method == HttpMethod.Post) Interlocked.Increment(ref Posts); return action(request, ct); }
    }
    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new("http://localhost:5085/") };
    private static HttpResponseMessage Json<T>(T value, HttpStatusCode code = HttpStatusCode.OK) => new(code) { Content = JsonContent.Create(value) };
    private static HttpResponseMessage Catalog(HttpRequestMessage request) => request.RequestUri!.AbsolutePath.EndsWith("students")
        ? Json(new[] { new StudentResponse(1, "S1", "Synthetic Student", null, true) })
        : Json(new[] { new EquipmentResponse(1, "E1", "Camera", null, true), new EquipmentResponse(2, "E2", "Tripod", null, true) });
    private static BorrowingResponse Loan(int id = 7, BorrowingState status = BorrowingState.Active) =>
        new(id, 1, 1, Today, Today.AddDays(3), status == BorrowingState.Returned ? Today : null, status,
            "S1", "Synthetic Student", "E1", "Camera");

    [Fact]
    public async Task LostSingleIssueResponseBlocksAnotherIssueUntilRefresh()
    {
        var handler = new Handler((request, _) => request.Method == HttpMethod.Post
            ? throw new HttpRequestException("lost") : Task.FromResult(Catalog(request)));
        using var client = Client(handler); var api = new ApiTransport(client);
        var vm = new EquipmentViewModel(new HttpSingleCheckout(api), new HttpCatalogReader(api), new Clock());
        await vm.InitializeAsync(); vm.SelectedStudent = vm.StudentList[0]; vm.SelectedEquipment = vm.EquipmentList[0];
        await vm.BorrowCommand.ExecuteAsync(null);
        Assert.True(vm.NeedsVerification); Assert.False(vm.IsSuccess); Assert.Equal(1, handler.Posts);
        Assert.False(vm.BorrowCommand.CanExecute(null));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.False(vm.NeedsVerification); Assert.True(vm.IsReady); Assert.Equal(1, handler.Posts);
    }

    [Fact]
    public async Task ReturnRequiresConfirmationSendsEmptyObjectAndRefreshesActiveList()
    {
        var returned = false;
        var handler = new Handler(async (request, ct) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                Assert.Equal("{}", await request.Content!.ReadAsStringAsync(ct));
                returned = true; return Json(Loan(status: BorrowingState.Returned));
            }
            return Json(returned ? Array.Empty<BorrowingResponse>() : new[] { Loan() });
        });
        using var client = Client(handler); var api = new ApiTransport(client);
        var vm = new BorrowingsViewModel(new HttpReturnWorkflow(api), new HttpActiveBorrowingReader(api));
        await vm.InitializeAsync(); vm.SelectedBorrowing = vm.ActiveBorrowings[0];
        await vm.ReturnCommand.ExecuteAsync(null); Assert.Equal(0, handler.Posts);
        vm.ConfirmReturn = true; await vm.ReturnCommand.ExecuteAsync(null);
        Assert.True(vm.IsSuccess); Assert.Empty(vm.ActiveBorrowings); Assert.False(vm.ConfirmReturn); Assert.Equal(1, handler.Posts);
    }

    [Fact]
    public async Task LostMultiCheckoutResponseBlocksResubmissionWithoutReplay()
    {
        var handler = new Handler((request, _) => request.Method == HttpMethod.Post
            ? throw new HttpRequestException("lost") : Task.FromResult(Catalog(request)));
        using var client = Client(handler); var api = new ApiTransport(client);
        var vm = new MultiItemCheckoutViewModel(new HttpMultiCheckout(api), new HttpCatalogReader(api), new Clock());
        await vm.InitializeAsync(); vm.SelectedStudent = vm.StudentList[0];
        foreach (var row in vm.EquipmentList) row.IsSelected = true;
        await vm.CheckoutCommand.ExecuteAsync(null);
        Assert.True(vm.NeedsVerification); Assert.False(vm.CanSelect); Assert.Equal(1, handler.Posts);
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.False(vm.NeedsVerification); Assert.True(vm.CanSelect); Assert.Equal(0, vm.SelectedCount); Assert.Equal(1, handler.Posts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IncompleteOrMismatchedMultiSuccessIsUnknown(bool empty)
    {
        var rows = empty ? Array.Empty<BorrowingResponse>() : new[] { Loan(), Loan(8) with { EquipmentId = 99 } };
        var handler = new Handler((_, _) => Task.FromResult(Json(new CheckoutResponse(rows), HttpStatusCode.Created)));
        using var client = Client(handler);
        var result = await new HttpMultiCheckout(new ApiTransport(client)).ExecuteAsync(new(1, new[] { 1, 2 }, Today.AddDays(3)), TestContext.Current.CancellationToken);
        Assert.Equal(OperationOutcome.Unknown, result.Outcome); Assert.Equal(1, handler.Posts);
    }

    [Fact]
    public async Task ActiveListWithoutRequiredRelatedDisplayDataIsNotReady()
    {
        using var client = Client(new Handler((_, _) => Task.FromResult(Json(new[] { Loan() with { StudentName = null } }))));
        var api = new ApiTransport(client);
        var vm = new BorrowingsViewModel(new HttpReturnWorkflow(api), new HttpActiveBorrowingReader(api));
        await vm.InitializeAsync(); Assert.False(vm.IsReady); Assert.Empty(vm.ActiveBorrowings);
        Assert.Contains("invalid active", vm.LoadMessage);
    }
}
