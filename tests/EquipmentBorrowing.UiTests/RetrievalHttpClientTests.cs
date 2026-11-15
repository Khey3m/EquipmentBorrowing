using System.Net;
using System.Net.Http.Json;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Desktop;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EquipmentBorrowing.UiTests;

public class RetrievalHttpClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => action(request, ct);
    }
    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new("http://localhost:5085/") };
    private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = JsonContent.Create(value) };
    private static BorrowingResponse Row(int id) => new(id, id, id, new(2026,11,15), new(2026,11,18), null,
        BorrowingState.Active, "S"+id, "Student "+id, "E"+id, "Item "+id);

    [Fact]
    public async Task SearchLateHttpResponseCannotOverwriteNewCriteria()
    {
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = Client(new Handler(async (request, _) =>
        {
            if (request.RequestUri!.Query.Contains("search=Old")) { await release.Task; return Json(new[] { Row(1) }); }
            return Json(new[] { Row(2) });
        }));
        var vm = new BorrowingSearchViewModel(new HttpBorrowingSearch(new ApiTransport(client)));
        vm.SearchText = "Old"; var old = vm.ApplyCommand.ExecuteAsync(null); Assert.True(vm.IsBusy);
        vm.SearchText = "New"; await vm.ApplyCommand.ExecuteAsync(null);
        release.SetResult(true); await old;
        Assert.Single(vm.Rows); Assert.Equal(2, vm.Rows[0].Id); Assert.False(vm.IsBusy);
        vm.Deactivate();
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("missing")]
    [InlineData("mixed")]
    public async Task HistoryDistinguishesEmptyMissingAndMixedSubjectPayload(string mode)
    {
        using var client = Client(new Handler((request, _) =>
        {
            if (mode == "missing") return Task.FromResult(Json(new ApiError("not_found", "Student not found."), HttpStatusCode.NotFound));
            return Task.FromResult(request.RequestUri!.AbsolutePath.EndsWith("borrowings")
                ? Json(mode == "empty" ? Array.Empty<BorrowingResponse>() : new[] { Row(2) })
                : Json(new StudentResponse(1, "S1", "Student 1", null, true)));
        }));
        var result = await new HttpHistoryWorkflow(new ApiTransport(client)).GetHistoryAsync(HistoryKind.Student, 1, TestContext.Current.CancellationToken);
        if (mode == "empty") { Assert.Equal(OperationOutcome.Succeeded, result.Outcome); Assert.Empty(result.Value!.Rows); }
        if (mode == "missing") { Assert.Equal(WorkflowError.NotFound, result.Error); Assert.Null(result.Value); }
        if (mode == "mixed") { Assert.Equal(OperationOutcome.Failed, result.Outcome); Assert.Null(result.Value); }
    }

    [Fact]
    public async Task HistoryLateHttpResponseCannotMixNewSubjectRows()
    {
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = Client(new Handler(async (request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/v1/students") return Json(new[] { new StudentResponse(1,"S1","Student 1",null,true), new StudentResponse(2,"S2","Student 2",null,true) });
            var id = path.Contains("/1") ? 1 : 2;
            if (path.EndsWith("borrowings"))
            {
                if (id == 1) await release.Task;
                return Json(new[] { Row(id) });
            }
            return Json(new StudentResponse(id,"S"+id,"Student "+id,null,true));
        }));
        var vm = new BorrowingHistoryViewModel(new HttpHistoryWorkflow(new ApiTransport(client)));
        await vm.InitializeAsync(); vm.SelectedSubject = vm.Subjects[0];
        var old = vm.RetryHistoryCommand.ExecuteAsync(null);
        vm.SelectedSubject = vm.Subjects[1]; await vm.RetryHistoryCommand.ExecuteAsync(null);
        release.SetResult(true); await old;
        Assert.Single(vm.Rows); Assert.Equal(2, vm.Rows[0].StudentId); Assert.False(vm.IsLoading);
        vm.Deactivate();
    }

    [Fact]
    public async Task FullDesktopCompositionLoadsThroughHttpWithoutPersistenceServices()
    {
        using var client = Client(new Handler((_, _) => Task.FromResult(Json(Array.Empty<StudentResponse>()))));
        using var provider = DesktopComposition.Build(client);
        var vm = provider.GetRequiredService<MainWindowViewModel>();
        await vm.RetryStartupAsync();
        Assert.True(vm.EquipmentVm.IsReady); Assert.False(vm.HasStartupMessage);
        var references = typeof(MainWindowViewModel).Assembly.GetReferencedAssemblies().Select(name => name.Name).ToArray();
        Assert.DoesNotContain("EquipmentBorrowing.Infrastructure", references);
        Assert.DoesNotContain(references, name => name!.Contains("EntityFrameworkCore") || name.Contains("Npgsql"));
    }
}
