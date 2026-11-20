using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace EquipmentBorrowing.ApiTests;

public sealed class PersistenceApiTests(ITestOutputHelper output)
{
    [ApiPostgresFact, Trait("TC6", "E08")]
    public async Task E08_RecordsAndReturnSurviveApiProcessRestarts()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        StudentResponse student;
        EquipmentResponse item;
        BorrowingResponse issued;
        int firstPid;
        var campusDate = CampusDate();

        await using (var api = await RunningApi.Start(scenario))
        {
            firstPid = api.Id;
            student = await Post<StudentResponse>(api.Client, "/api/v1/students",
                new StudentInput("ST-800", "Persistence Student", "Persistence Contact"), HttpStatusCode.Created);
            item = await Post<EquipmentResponse>(api.Client, "/api/v1/equipment",
                new EquipmentInput("EQ-800", "Persistence Monitor", "Persistence Description"), HttpStatusCode.Created);
            Assert.True(student.IsEligible);
            Assert.True(item.IsAvailable);
            Assert.Equal(("ST-800", "Persistence Student", "Persistence Contact"), (student.StudentNumber, student.FullName, student.Contact));
            Assert.Equal(("EQ-800", "Persistence Monitor", "Persistence Description"), (item.EquipmentCode, item.Name, item.Description));
            issued = await Post<BorrowingResponse>(api.Client, "/api/v1/borrowings",
                new BorrowInput(student.Id, item.Id, campusDate.AddDays(3)), HttpStatusCode.Created);
            Assert.Equal((student.Id, item.Id, campusDate.AddDays(3), BorrowingState.Active),
                (issued.StudentId, issued.EquipmentId, issued.ExpectedReturnDate, issued.Status));
            Assert.InRange(issued.BorrowedDate, campusDate, CampusDate());
            Assert.Null(issued.ReturnedDate);
            await AssertStored(scenario, student, item, issued);
            output.WriteLine($"Process {firstPid} created student {student.Id}, item {item.Id}, and active loan {issued.Id}.");
        }

        BorrowingResponse returned;
        int secondPid;
        await using (var api = await RunningApi.Start(scenario))
        {
            secondPid = api.Id;
            Assert.NotEqual(firstPid, secondPid);
            await AssertHttpState(api.Client, student, item, issued);
            var returnDate = CampusDate();
            returned = await Post<BorrowingResponse>(api.Client, $"/api/v1/borrowings/{issued.Id}/return",
                new ReturnInput(), HttpStatusCode.OK);
            Assert.Equal(issued with { Status = BorrowingState.Returned, ReturnedDate = returned.ReturnedDate }, returned);
            Assert.InRange(Assert.IsType<DateOnly>(returned.ReturnedDate), returnDate, CampusDate());
            await AssertStored(scenario, student, item, returned);
            output.WriteLine($"Restarted as process {secondPid}. GET confirmed the issued loan. Return retained loan {returned.Id}.");
        }

        var beforeRestart = await scenario.SnapshotAsync();
        await using (var api = await RunningApi.Start(scenario))
        {
            Assert.NotEqual(secondPid, api.Id);
            await AssertHttpState(api.Client, student, item, returned);
            await AssertStored(scenario, student, item, returned);
            Assert.Equal(beforeRestart, await scenario.SnapshotAsync());
            output.WriteLine($"Restarted as process {api.Id}. HTTP and fresh PostgreSQL reads both confirmed returned history and restored availability.");
        }
        output.WriteLine($"Initial campus date {campusDate:yyyy-MM-dd}. Borrowed {issued.BorrowedDate:yyyy-MM-dd}. Returned {returned.ReturnedDate:yyyy-MM-dd}.");
    }

    private static DateOnly CampusDate() => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).DateTime);

    private async Task<T> Post<T>(HttpClient client, string path, object body, HttpStatusCode status)
    {
        using var response = await client.PostAsJsonAsync(path, body);
        var result = await response.Content.ReadAsStringAsync();
        output.WriteLine($"POST {path} => {(int)response.StatusCode} {result}");
        Assert.Equal(status, response.StatusCode);
        return Assert.IsType<T>(await response.Content.ReadFromJsonAsync<T>());
    }

    private static async Task AssertHttpState(HttpClient client, StudentResponse student, EquipmentResponse item, BorrowingResponse loan)
    {
        Assert.Equal(student, await client.GetFromJsonAsync<StudentResponse>($"/api/v1/students/{student.Id}"));
        Assert.Equal(item with { IsAvailable = loan.Status == BorrowingState.Returned },
            await client.GetFromJsonAsync<EquipmentResponse>($"/api/v1/equipment/{item.Id}"));
        Assert.Equal(loan, await client.GetFromJsonAsync<BorrowingResponse>($"/api/v1/borrowings/{loan.Id}"));
        var history = await client.GetFromJsonAsync<BorrowingResponse[]>($"/api/v1/students/{student.Id}/borrowings");
        Assert.Equal(loan with { StudentNumber = student.StudentNumber, StudentName = student.FullName,
            EquipmentCode = item.EquipmentCode, EquipmentName = item.Name }, Assert.Single(history!));
    }

    private static async Task AssertStored(ApiScenario scenario, StudentResponse student, EquipmentResponse item, BorrowingResponse loan)
    {
        await using var fresh = scenario.CreateDbContext();
        var savedStudent = await fresh.Students.AsNoTracking().SingleAsync(row => row.Id == student.Id);
        Assert.Equal(student, new StudentResponse(savedStudent.Id, savedStudent.StudentNumber, savedStudent.FullName,
            savedStudent.ContactInformation, savedStudent.IsEligibleToBorrow));
        var savedItem = await fresh.Equipment.AsNoTracking().SingleAsync(row => row.Id == item.Id);
        Assert.Equal(item with { IsAvailable = loan.Status == BorrowingState.Returned },
            new EquipmentResponse(savedItem.Id, savedItem.EquipmentIdentifier, savedItem.Name, savedItem.Description, savedItem.IsAvailable));
        var savedLoan = await fresh.Borrowings.AsNoTracking().SingleAsync(row => row.StudentId == student.Id);
        Assert.Equal(loan, new BorrowingResponse(savedLoan.Id, savedLoan.StudentId, savedLoan.EquipmentId, savedLoan.BorrowedAt,
            savedLoan.ExpectedReturnAt, savedLoan.ActualReturnAt, Enum.Parse<BorrowingState>(savedLoan.Status.ToString())));
    }

    private sealed class RunningApi(Process process, HttpClient client) : IAsyncDisposable
    {
        public int Id => process.Id;
        public HttpClient Client { get; } = client;

        public static async Task<RunningApi> Start(ApiScenario scenario)
        {
            var dll = typeof(Program).Assembly.Location;
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Path.GetDirectoryName(dll)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            start.ArgumentList.Add(dll);
            start.ArgumentList.Add("--urls");
            start.ArgumentList.Add("http://127.0.0.1:0");
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
            start.Environment["DOTNET_ENVIRONMENT"] = "Production";
            start.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";
            scenario.ConfigureProcessDatabase(start);
            var address = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var process = new Process { StartInfo = start, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, args) =>
            {
                const string prefix = "Now listening on: ";
                var line = args.Data?.TrimStart();
                if (line?.StartsWith(prefix, StringComparison.Ordinal) == true &&
                    Uri.TryCreate(line[prefix.Length..], UriKind.Absolute, out var uri) && uri.IsLoopback)
                    address.TrySetResult(uri);
            };
            process.ErrorDataReceived += (_, _) => { };
            process.Exited += (_, _) => address.TrySetException(new InvalidOperationException("API process exited before startup."));
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var api = new RunningApi(process, client);
            try
            {
                if (!process.Start())
                    throw new InvalidOperationException("Could not start API process.");
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                client.BaseAddress = await address.Task.WaitAsync(timeout.Token);
                using var status = await client.GetAsync("/api/v1/status", timeout.Token);
                status.EnsureSuccessStatusCode();
                return api;
            }
            catch
            {
                await api.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await process.WaitForExitAsync(timeout.Token);
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
