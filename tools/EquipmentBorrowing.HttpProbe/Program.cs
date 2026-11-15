using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Http;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Desktop;
using Microsoft.Extensions.DependencyInjection;

// This verification tool never reads PostgreSQL configuration or constructs persistence services.
var trace = new ConcurrentQueue<object>();
try
{
    using var client = new HttpClient(new TraceHandler(trace, "desktop"))
    { BaseAddress = new Uri(Environment.GetEnvironmentVariable(ApiTransport.BaseUrlVariable)!), Timeout = TimeSpan.FromSeconds(15) };
    using var independent = new HttpClient(new TraceHandler(trace, "independent")) { BaseAddress = client.BaseAddress };
    using var desktop = DesktopComposition.Build(client);
    var root = desktop.GetRequiredService<MainWindowViewModel>();
    var api = desktop.GetRequiredService<ApiTransport>();
    async Task<object> Diagnose(string mode)
    {
        var priorCount = trace.Count;
    await root.RetryStartupCommand.ExecuteAsync(null);
    await root.StudentsVm.InitializeAsync();
    var read = await api.SendAsync<StudentResponse[]>(HttpMethod.Get, "api/v1/students");
    string? mutationOutcome = null;
    if (mode == "healthy")
        Require(read.Success && read.Status == 200 && root.StudentsVm.IsReady && !root.HasStartupMessage);
    else
    {
        Require(!read.Success && !root.StudentsVm.IsReady && root.HasStartupMessage);
        if (mode == "offline") Require(read.Status is null && root.StartupMessage.Contains("No HTTP response"));
        else Require(mode == "server-error" && read.Status == 500 && root.StartupMessage.Contains("HTTP 500"));
        var mutation = await new HttpStudentManagement(api).RegisterAsync(new("ST-UNCOMMITTED", "Failure Probe", null));
        Require(mutation.Outcome == (mode == "offline" ? OperationOutcome.Unknown : OperationOutcome.Failed));
        mutationOutcome = mutation.Outcome.ToString();
        Require(trace.Skip(priorCount).Count(item => JsonSerializer.SerializeToElement(item).GetProperty("method").GetString() == "POST") == 1);
    }
        return new { success = true, mode, status = read.Status,
            uiMessage = root.StudentsVm.LoadMessage, startupMessage = root.StartupMessage, mutationOutcome,
            databaseSettingPresent = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EQUIPMENT_BORROWING_CONNECTION_STRING")),
            sameClientProcess = true, trace = trace.Skip(priorCount).ToArray() };
    }
    if (args.Length == 1 && args[0] == "--diagnostic-session")
    {
        Console.WriteLine("{\"ready\":true}");
        while (await Console.In.ReadLineAsync() is { } mode)
            Console.WriteLine(JsonSerializer.Serialize(await Diagnose(mode)));
        return 0;
    }
    if (args.Length == 2 && args[0] == "--diagnostic")
    {
        Console.WriteLine(JsonSerializer.Serialize(await Diagnose(args[1]), new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    var students = root.StudentsVm;
    await students.InitializeAsync(); Require(students.IsReady && students.Students.Count == 4);
    students.StudentNumber = "ST-HTTP"; students.FullName = "HTTP Demo Student"; students.ContactInformation = "demo@example.edu";
    await students.RegisterCommand.ExecuteAsync(null);
    Require(students.SelectedStudent is not null && students.SelectedStudent.IsEligibleToBorrow);
    var studentId = students.SelectedStudent!.Id;
    students.FullName = "HTTP Edited Student"; students.ContactInformation = "";
    await students.SaveChangesCommand.ExecuteAsync(null);
    Require(students.SelectedStudent?.Id == studentId && students.SelectedStudent.ContactInformation is null);
    await students.RestrictCommand.ExecuteAsync(null); Require(students.SelectedStudent?.IsEligibleToBorrow == false);
    await students.PermitCommand.ExecuteAsync(null); Require(students.SelectedStudent?.IsEligibleToBorrow == true);
    using (var response = await independent.PutAsJsonAsync($"api/v1/students/{studentId}", new StudentUpdateInput("ST-HTTP", "Changed outside Desktop", null)))
        Require(response.IsSuccessStatusCode);
    using (var stale = new HttpRequestMessage(HttpMethod.Put, $"api/v1/students/{studentId}"))
    {
        stale.Headers.Add(EntityRevision.Header, EntityRevision.For(new StudentResponse(studentId, "ST-HTTP", "HTTP Edited Student", null, true)));
        stale.Content = JsonContent.Create(new StudentUpdateInput("ST-HTTP", "Must not overwrite", null));
        using var rejected = await independent.SendAsync(stale);
        Require((int)rejected.StatusCode == 409);
    }
    using (var staleEligibility = new HttpRequestMessage(HttpMethod.Patch, $"api/v1/students/{studentId}/eligibility"))
    {
        staleEligibility.Headers.Add(EntityRevision.Header, EntityRevision.For(new StudentResponse(studentId, "ST-HTTP", "HTTP Edited Student", null, true)));
        staleEligibility.Content = JsonContent.Create(new EligibilityInput(false));
        using var rejected = await independent.SendAsync(staleEligibility);
        Require((int)rejected.StatusCode == 409);
    }
    await students.RefreshCommand.ExecuteAsync(null);
    students.SelectedStudent = students.Students.Single(s => s.Id == studentId);
    Require(students.FullName == "Changed outside Desktop");

    var equipment = root.EquipmentManagementVm;
    await equipment.InitializeAsync(); Require(equipment.IsReady && equipment.Items.Count == 7);
    equipment.EquipmentIdentifier = "EQ-HTTP"; equipment.Name = "HTTP Demo Display"; equipment.Description = "USB display";
    await equipment.RegisterCommand.ExecuteAsync(null);
    Require(equipment.SelectedEquipment?.IsAvailable == true);
    var equipmentId = equipment.SelectedEquipment!.Id;
    equipment.Name = "HTTP Edited Display"; equipment.Description = "";
    await equipment.SaveChangesCommand.ExecuteAsync(null);
    Require(equipment.SelectedEquipment?.Id == equipmentId && equipment.SelectedEquipment.Description is null);
    using (var response = await independent.PutAsJsonAsync($"api/v1/equipment/{equipmentId}", new EquipmentUpdateInput("EQ-HTTP", "External Display", null)))
        Require(response.IsSuccessStatusCode);
    using (var staleEquipment = new HttpRequestMessage(HttpMethod.Put, $"api/v1/equipment/{equipmentId}"))
    {
        staleEquipment.Headers.Add(EntityRevision.Header, EntityRevision.For(new EquipmentResponse(equipmentId, "EQ-HTTP", "HTTP Edited Display", null, true)));
        staleEquipment.Content = JsonContent.Create(new EquipmentUpdateInput("EQ-HTTP", "Must not overwrite", null));
        using var rejected = await independent.SendAsync(staleEquipment);
        Require((int)rejected.StatusCode == 409);
    }
    equipment.SearchText = "External";
    await equipment.RefreshCommand.ExecuteAsync(null);
    Require(equipment.Items.Count == 1 && equipment.Items[0].Name == "External Display");
    var catalog = await new HttpCatalogReader(api).GetCatalogAsync(true);
    Require(catalog.Success && catalog.Value!.Equipment.Any(item => item.Id == equipmentId)
        && catalog.Value.Equipment.All(item => item.IsAvailable) && catalog.Value.Students.Any(s => s.Id == studentId));
    object? borrowingVerification = null;
    if (args.Contains("--borrowing") || args.Contains("--retrieval"))
    {
        var catalogReader = new HttpCatalogReader(api);
        var single = root.EquipmentVm;
        await single.InitializeAsync(); Require(single.IsReady);
        single.SelectedStudent = single.StudentList.Single(row => row.Id == studentId);
        single.SelectedEquipment = single.EquipmentList.Single(row => row.Id == equipmentId);
        await single.BorrowCommand.ExecuteAsync(null);
        Require(single.IsSuccess && !single.EquipmentList.Single(row => row.Id == equipmentId).IsAvailable);
        var active = root.BorrowingsVm;
        await active.InitializeAsync(); Require(active.IsReady);
        active.SelectedBorrowing = active.ActiveBorrowings.Single(row => row.StudentId == studentId && row.EquipmentId == equipmentId);
        var singleLoanId = active.SelectedBorrowing.Id;
        var beforeConfirmation = trace.Count;
        await active.ReturnCommand.ExecuteAsync(null); Require(trace.Count == beforeConfirmation);
        active.ConfirmReturn = true; await active.ReturnCommand.ExecuteAsync(null);
        Require(active.IsSuccess && !active.ConfirmReturn && active.ActiveBorrowings.All(row => row.Id != singleLoanId));
        var repeated = await new HttpReturnWorkflow(api).ExecuteAsync(singleLoanId);
        Require(!repeated.Success && repeated.Outcome == OperationOutcome.Conflict);
        await single.RefreshCommand.ExecuteAsync(null);
        Require(single.EquipmentList.Single(row => row.Id == equipmentId).IsAvailable);

        var multi = root.MultiCheckoutVm;
        await multi.InitializeAsync(); multi.SelectedStudent = multi.StudentList.Single(row => row.Id == studentId);
        var chosen = multi.EquipmentList.Where(row => row.Item.EquipmentIdentifier is "EQ-002" or "EQ-006").ToArray();
        Require(chosen.Length == 2); foreach (var row in chosen) row.IsSelected = true;
        var selectedIds = chosen.Select(row => row.Item.Id).Order().ToArray();
        await multi.CheckoutCommand.ExecuteAsync(null);
        Require(multi.IsSuccess && multi.SelectedCount == 0
            && multi.EquipmentList.Where(row => selectedIds.Contains(row.Item.Id)).All(row => !row.Item.IsAvailable));
        await active.RefreshCommand.ExecuteAsync(null);
        Require(active.ActiveBorrowings.Count(row => row.StudentId == studentId) == 2);
        // The earlier catalog is stale: E2 appeared available before the multi-item issue.
        single.SelectedStudent = single.StudentList.Single(row => row.Id == studentId);
        single.SelectedEquipment = single.EquipmentList.Single(row => row.EquipmentIdentifier == "EQ-002");
        Require(single.SelectedEquipment.IsAvailable);
        await single.BorrowCommand.ExecuteAsync(null);
        Require(!single.IsSuccess && single.IsReady && !single.EquipmentList.Single(row => row.EquipmentIdentifier == "EQ-002").IsAvailable);
        // Two active items plus two more is rejected by the server and selections are refreshed.
        multi.SelectedStudent = multi.StudentList.Single(row => row.Id == studentId);
        foreach (var row in multi.EquipmentList.Where(row => row.Item.EquipmentIdentifier is "EQ-HTTP" or "EQ-007")) row.IsSelected = true;
        Require(multi.SelectedCount == 2); await multi.CheckoutCommand.ExecuteAsync(null);
        Require(!multi.IsSuccess && multi.IsReady && multi.SelectedCount == 0);
        var history = await independent.GetFromJsonAsync<BorrowingResponse[]>($"api/v1/students/{studentId}/borrowings");
        Require(history is not null && history.Length == 3 && history.Count(row => row.Status == BorrowingState.Active) == 2
            && history.Single(row => row.Id == singleLoanId).Status == BorrowingState.Returned);
        borrowingVerification = new { singleLoanId, activeMultiIds = active.ActiveBorrowings.Where(row => row.StudentId == studentId).Select(row => row.Id), selectedIds,
            returnConfirmationRequired = true, staleSelectionRejected = true, overLimitRejected = true };
    }
    object? retrievalVerification = null;
    if (args.Contains("--retrieval"))
    {
        var history = root.HistoryVm;
        await history.InitializeAsync();
        history.SelectedSubject = history.Subjects.Single(row => row.Id == studentId);
        await history.RetryHistoryCommand.ExecuteAsync(null);
        Require(history.Rows.Count == 3 && history.Rows.Count(row => row.Status == BorrowingStatus.Active) == 2
            && history.CurrentSubjectLabel.Contains("Changed outside Desktop"));
        history.SelectedSubject = history.Subjects.Single(row => row.Label.StartsWith("ST-400 - "));
        await history.RetryHistoryCommand.ExecuteAsync(null);
        Require(history.Rows.Count == 0 && history.HistoryMessage.Contains("No borrowing history"));
        history.SelectedSubject = new HistorySubject(2147483647, "Stale selection");
        await history.RetryHistoryCommand.ExecuteAsync(null);
        Require(history.Rows.Count == 0 && history.HistoryMessage.Contains("not found"));
        history.SelectedKind = HistoryKind.Equipment; await history.RefreshCommand.ExecuteAsync(null);
        history.SelectedSubject = history.Subjects.Single(row => row.Id == equipmentId);
        await history.RetryHistoryCommand.ExecuteAsync(null);
        Require(history.Rows.Count == 1 && history.Rows[0].Status == BorrowingStatus.Returned);
        history.SelectedSubject = history.Subjects.Single(row => row.Label.StartsWith("EQ-007 - "));
        await history.RetryHistoryCommand.ExecuteAsync(null); Require(history.Rows.Count == 0);

        var search = root.SearchVm;
        search.SearchText = "Changed outside Desktop"; search.StatusFilter = BorrowingStatusFilter.Active;
        search.SortBy = BorrowingSortField.ExpectedReturnDate; search.Descending = true;
        await search.ApplyCommand.ExecuteAsync(null);
        Require(search.Rows.Count == 2 && search.Rows.All(row => row.StudentId == studentId)
            && search.Rows.Select(row => row.Id).SequenceEqual(search.Rows.Select(row => row.Id).Order()));
        search.SearchText = "NON-EXISTENT-999"; await search.ApplyCommand.ExecuteAsync(null);
        Require(search.Rows.Count == 0 && search.Message.Contains("No borrowings match"));
        await search.ClearCommand.ExecuteAsync(null);
        Require(search.Rows.Count == 7 && search.SearchText == "" && search.StatusFilter == BorrowingStatusFilter.All
            && search.SortBy == BorrowingSortField.CheckoutDate && !search.Descending);
        retrievalVerification = new { studentHistory = true, equipmentHistory = true, emptyDistinctFromMissing = true,
            combinedSearch = true, tiedIdsAscending = true, clearRestoresSevenRows = true, fullDesktopComposition = true };
        history.Deactivate(); search.Deactivate();
    }
    Console.WriteLine(JsonSerializer.Serialize(new { success = true, studentId, equipmentId, borrowingVerification, retrievalVerification,
        databaseSettingPresent = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EQUIPMENT_BORROWING_CONNECTION_STRING")), trace },
        new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine("HTTP ViewModel probe failed (" + error.GetType().Name + "). Private details omitted.");
    return 1;
}

static void Require(bool condition) { if (!condition) throw new InvalidOperationException("HTTP ViewModel verification failed."); }
sealed class TraceHandler(ConcurrentQueue<object> trace, string source) : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        try
        {
            var response = await base.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            trace.Enqueue(new { source, method = request.Method.Method, path = request.RequestUri!.PathAndQuery,
                request = body, expectedRevision = request.Headers.TryGetValues(EntityRevision.Header, out var revisions) ? revisions.Single() : null,
                status = (int)response.StatusCode, response = content });
            return response;
        }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException)
        {
            trace.Enqueue(new { source, method = request.Method.Method, path = request.RequestUri!.PathAndQuery,
                request = body, status = (int?)null, transportFailure = error.GetType().Name });
            throw;
        }
    }
}
