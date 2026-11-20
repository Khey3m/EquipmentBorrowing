using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace EquipmentBorrowing.ApiTests;

public sealed class ManagementApiTests(ITestOutputHelper output)
{
    private const string Students = "/api/v1/students";
    private const string Equipment = "/api/v1/equipment";
    private const string Loans = "/api/v1/borrowings";

    [ApiPostgresFact, Trait("TC6", "U12")]
    public async Task U12_RegisterStudent()
    {
        await using var s = await ApiScenario.CreateAsync(output);
        using var response = await s.Client.PostAsJsonAsync(Students, new StudentInput("ST-500", "New Student", "Contact"));
        var row = await Read<StudentResponse>(response, HttpStatusCode.Created);
        Assert.True(row.Id > 0);
        Assert.Equal(new StudentResponse(row.Id, "ST-500", "New Student", "Contact", true), row);
        Assert.Equal($"{Students}/{row.Id}", response.Headers.Location?.ToString());
        Assert.Equal(row, await s.GetAsync<StudentResponse>(response.Headers.Location!.ToString()));
        await using var fresh = s.CreateDbContext();
        Assert.Equal(5, await fresh.Students.CountAsync());
        var saved = await fresh.Students.SingleAsync(x => x.Id == row.Id);
        Assert.Equal(row, new StudentResponse(saved.Id, saved.StudentNumber, saved.FullName, saved.ContactInformation, saved.IsEligibleToBorrow));
    }

    [ApiPostgresFact, Trait("TC6", "U13")]
    public async Task U13_EditStudentAndPreserveHistory()
    {
        await using var s = await ApiScenario.CreateAsync(output);
        var original = s.Students["ST-100"];
        Assert.Equal(original, Assert.Single(await s.GetAsync<StudentResponse[]>(Students + "?search=ST-100")));
        Assert.Empty(await s.GetAsync<StudentResponse[]>(Students + "?search=no-match"));
        var path = $"{Students}/{original.Id}";
        Assert.Equal(original, await s.GetAsync<StudentResponse>(path));
        var history = await s.GetAsync<BorrowingResponse[]>(path + "/borrowings");
        Assert.Equal(new[] { BorrowingState.Active, BorrowingState.Returned }, history.Select(x => x.Status).Order());
        var before = await Table(s, "borrowings");
        using var response = await s.Client.PutAsJsonAsync(path, new StudentUpdateInput("ST-101", "Ada Updated", "New contact"));
        var expected = original with { StudentNumber = "ST-101", FullName = "Ada Updated", Contact = "New contact" };
        Assert.Equal(expected, await Read<StudentResponse>(response));
        var after = await s.GetAsync<BorrowingResponse[]>(path + "/borrowings");
        Assert.Equal(history.OrderBy(x => x.Id).Select(x => x with { StudentNumber = "ST-101", StudentName = "Ada Updated" }), after.OrderBy(x => x.Id));
        Assert.Equal(before, await Table(s, "borrowings"));
        await using var fresh = s.CreateDbContext();
        var saved = await fresh.Students.SingleAsync(x => x.Id == original.Id);
        Assert.Equal(expected, new StudentResponse(saved.Id, saved.StudentNumber, saved.FullName, saved.ContactInformation, saved.IsEligibleToBorrow));
    }

    [ApiPostgresFact, Trait("TC6", "U14")]
    public async Task U14_RestrictionBlocksIssueButAllowsReturnAndReissue()
    {
        await using var s = await ApiScenario.CreateAsync(output);
        var student = s.Students["ST-100"];
        var camera = s.Equipment["EQ-001"];
        var laptop = s.Equipment["EQ-002"];
        var historyPath = $"{Students}/{student.Id}/borrowings";
        var history = await s.GetAsync<BorrowingResponse[]>(historyPath);
        var active = Assert.Single(history, x => x.EquipmentId == camera.Id && x.Status == BorrowingState.Active);
        var loanState = await Table(s, "borrowings");
        using var restrict = await s.Client.PatchAsJsonAsync($"{Students}/{student.Id}/eligibility", new EligibilityInput(false));
        Assert.Equal(student with { IsEligible = false }, await Read<StudentResponse>(restrict));
        Assert.Equal(loanState, await Table(s, "borrowings"));
        var input = new BorrowInput(student.Id, laptop.Id, ApiScenario.D.AddDays(2));
        await Reject(s, () => s.Client.PostAsJsonAsync(Loans, input), HttpStatusCode.Conflict);
        using var returned = await s.Client.PostAsJsonAsync($"{Loans}/{active.Id}/return", new ReturnInput());
        var closed = await Read<BorrowingResponse>(returned);
        Assert.Equal(active.Id, closed.Id);
        Assert.Equal(BorrowingState.Returned, closed.Status);
        Assert.Equal(ApiScenario.D, closed.ReturnedDate);
        using var permit = await s.Client.PatchAsJsonAsync($"{Students}/{student.Id}/eligibility", new EligibilityInput(true));
        Assert.Equal(student, await Read<StudentResponse>(permit));
        using var issue = await s.Client.PostAsJsonAsync(Loans, input);
        var opened = await Read<BorrowingResponse>(issue, HttpStatusCode.Created);
        Assert.Equal(student.Id, opened.StudentId);
        Assert.Equal(laptop.Id, opened.EquipmentId);
        await using var fresh = s.CreateDbContext();
        Assert.True((await fresh.Students.SingleAsync(x => x.Id == student.Id)).IsEligibleToBorrow);
        Assert.True((await fresh.Equipment.SingleAsync(x => x.Id == camera.Id)).IsAvailable);
        Assert.False((await fresh.Equipment.SingleAsync(x => x.Id == laptop.Id)).IsAvailable);
        var rows = await fresh.Borrowings.Where(x => x.StudentId == student.Id).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.Equal(BorrowingStatus.Returned, Assert.Single(rows, x => x.Id == active.Id && x.EquipmentId == camera.Id).Status);
        var earlierReturn = Assert.Single(history, x => x.Status == BorrowingState.Returned);
        Assert.Equal(earlierReturn.ReturnedDate, Assert.Single(rows, x => x.Id == earlierReturn.Id && x.EquipmentId == earlierReturn.EquipmentId).ActualReturnAt);
        Assert.Equal(opened.Id, Assert.Single(rows, x => x.Status == BorrowingStatus.Active && x.EquipmentId == laptop.Id).Id);
    }

    [ApiPostgresFact, Trait("TC6", "U15")]
    public async Task U15_RegisterEquipmentAndRejectDuplicate()
    {
        await using var s = await ApiScenario.CreateAsync(output);
        using var response = await s.Client.PostAsJsonAsync(Equipment, new EquipmentInput("EQ-008", "Monitor", "HD"));
        var row = await Read<EquipmentResponse>(response, HttpStatusCode.Created);
        Assert.True(row.Id > 0);
        Assert.Equal(new EquipmentResponse(row.Id, "EQ-008", "Monitor", "HD", true), row);
        Assert.Equal($"{Equipment}/{row.Id}", response.Headers.Location?.ToString());
        Assert.Equal(row, await s.GetAsync<EquipmentResponse>(response.Headers.Location!.ToString()));
        await Reject(s, () => s.Client.PostAsJsonAsync(Equipment, new EquipmentInput("EQ-001", "Duplicate", null)), HttpStatusCode.Conflict);
        await using var fresh = s.CreateDbContext();
        Assert.Equal(8, await fresh.Equipment.CountAsync());
        var saved = await fresh.Equipment.SingleAsync(x => x.Id == row.Id);
        Assert.Equal(row, new EquipmentResponse(saved.Id, saved.EquipmentIdentifier, saved.Name, saved.Description, saved.IsAvailable));
    }

    [ApiPostgresFact, Trait("TC6", "U16")]
    public async Task U16_EditEquipmentAndPreserveAvailabilityAndHistory()
    {
        await using var s = await ApiScenario.CreateAsync(output);
        var original = s.Equipment["EQ-001"];
        var path = $"{Equipment}/{original.Id}";
        Assert.Equal(original, await s.GetAsync<EquipmentResponse>(path));
        var history = Assert.Single(await s.GetAsync<BorrowingResponse[]>(path + "/borrowings"));
        Assert.Equal(BorrowingState.Active, history.Status);
        var before = await Table(s, "borrowings");
        using var response = await s.Client.PutAsJsonAsync(path, new EquipmentUpdateInput(original.EquipmentCode, "Camera Updated", "New description"));
        var expected = original with { Name = "Camera Updated", Description = "New description" };
        Assert.Equal(expected, await Read<EquipmentResponse>(response));
        Assert.Equal(expected, await s.GetAsync<EquipmentResponse>(path));
        Assert.Equal(history with { EquipmentName = expected.Name }, Assert.Single(await s.GetAsync<BorrowingResponse[]>(path + "/borrowings")));
        Assert.Equal(before, await Table(s, "borrowings"));
        await using var fresh = s.CreateDbContext();
        var saved = await fresh.Equipment.SingleAsync(x => x.Id == original.Id);
        Assert.Equal(expected, new EquipmentResponse(saved.Id, saved.EquipmentIdentifier, saved.Name, saved.Description, saved.IsAvailable));
    }

    [ApiPostgresFact, Trait("TC6", "E06")]
    public async Task E06_ClearOptionalFieldsAndRejectInvalidEdits()
    {
        await using var s = await ApiScenario.CreateAsync(output);
        var student = s.Students["ST-100"];
        var item = s.Equipment["EQ-001"];
        var studentPath = $"{Students}/{student.Id}";
        var itemPath = $"{Equipment}/{item.Id}";
        var history = await Table(s, "borrowings");
        using var addContact = await s.Client.PutAsJsonAsync(studentPath, new StudentUpdateInput(student.StudentNumber, student.FullName, "Contact"));
        Assert.Equal("Contact", (await Read<StudentResponse>(addContact)).Contact);
        using var addDescription = await s.Client.PutAsJsonAsync(itemPath, new EquipmentUpdateInput(item.EquipmentCode, item.Name, "Description"));
        Assert.Equal("Description", (await Read<EquipmentResponse>(addDescription)).Description);
        await using (var seeded = s.CreateDbContext())
        {
            Assert.Equal("Contact", (await seeded.Students.SingleAsync(x => x.Id == student.Id)).ContactInformation);
            Assert.Equal("Description", (await seeded.Equipment.SingleAsync(x => x.Id == item.Id)).Description);
        }
        using var clearContact = await s.Client.PutAsJsonAsync(studentPath, new StudentUpdateInput(student.StudentNumber, student.FullName, null));
        Assert.Equal(student, await Read<StudentResponse>(clearContact));
        using var clearDescription = await s.Client.PutAsJsonAsync(itemPath, new EquipmentUpdateInput(item.EquipmentCode, item.Name, null));
        Assert.Equal(item, await Read<EquipmentResponse>(clearDescription));
        await Reject(s, () => s.Client.PutAsJsonAsync(studentPath, new StudentUpdateInput(student.StudentNumber, " ", null)), HttpStatusCode.BadRequest);
        await Reject(s, () => s.Client.PutAsJsonAsync(itemPath, new EquipmentUpdateInput(item.EquipmentCode, " ", null)), HttpStatusCode.BadRequest);
        await Reject(s, () => s.Client.PutAsJsonAsync(studentPath, new StudentUpdateInput("ST-200", student.FullName, null)), HttpStatusCode.Conflict);
        await Reject(s, () => s.Client.PutAsJsonAsync(itemPath, new EquipmentUpdateInput("EQ-002", item.Name, null)), HttpStatusCode.Conflict);
        await Reject(s, () => s.Client.PutAsJsonAsync(itemPath, new { equipmentCode = item.EquipmentCode, name = item.Name, description = (string?)null, isAvailable = true }), HttpStatusCode.BadRequest);
        Assert.Equal(history, await Table(s, "borrowings"));
        await using var fresh = s.CreateDbContext();
        Assert.Null((await fresh.Students.SingleAsync(x => x.Id == student.Id)).ContactInformation);
        var saved = await fresh.Equipment.SingleAsync(x => x.Id == item.Id);
        Assert.Null(saved.Description);
        Assert.False(saved.IsAvailable);
    }

    private async Task<T> Read<T>(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var body = await response.Content.ReadAsStringAsync();
        output.WriteLine($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.PathAndQuery} => {(int)response.StatusCode} {body}");
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return Assert.IsType<T>(JsonSerializer.Deserialize<T>(body, JsonSerializerOptions.Web));
    }

    private async Task Reject(ApiScenario scenario, Func<Task<HttpResponseMessage>> send, HttpStatusCode status)
    {
        var before = await scenario.SnapshotAsync();
        using var response = await send();
        Assert.False(string.IsNullOrWhiteSpace((await Read<ApiError>(response, status)).Code));
        Assert.Equal(before, await scenario.SnapshotAsync());
        output.WriteLine("Fresh PostgreSQL snapshot unchanged after rejection.");
    }

    private static async Task<string> Table(ApiScenario scenario, string name)
    {
        using var snapshot = JsonDocument.Parse(await scenario.SnapshotAsync());
        return snapshot.RootElement.GetProperty(name).GetRawText();
    }
}
