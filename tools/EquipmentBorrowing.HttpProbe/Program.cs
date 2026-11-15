using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Http;

// This verification tool never reads PostgreSQL configuration or constructs persistence services.
var trace = new ConcurrentQueue<object>();
try
{
    using var client = new HttpClient(new TraceHandler(trace, "desktop"))
    { BaseAddress = new Uri(Environment.GetEnvironmentVariable(ApiTransport.BaseUrlVariable)!), Timeout = TimeSpan.FromSeconds(15) };
    using var independent = new HttpClient(new TraceHandler(trace, "independent")) { BaseAddress = client.BaseAddress };
    var api = new ApiTransport(client);
    var students = new StudentsViewModel(new HttpStudentManagement(api));
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

    var equipment = new EquipmentManagementViewModel(new HttpEquipmentManagement(api));
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
    Console.WriteLine(JsonSerializer.Serialize(new { success = true, studentId, equipmentId,
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
        var response = await base.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        trace.Enqueue(new { source, method = request.Method.Method, path = request.RequestUri!.PathAndQuery,
            request = body, expectedRevision = request.Headers.TryGetValues(EntityRevision.Header, out var revisions) ? revisions.Single() : null,
            status = (int)response.StatusCode, response = content });
        return response;
    }
}
