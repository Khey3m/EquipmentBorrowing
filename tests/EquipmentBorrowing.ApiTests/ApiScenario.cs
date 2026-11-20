using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit.Abstractions;

namespace EquipmentBorrowing.ApiTests;

public sealed class ApiPostgresFactAttribute : FactAttribute
{
    public ApiPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ApiScenario.AdminSetting)))
            Skip = $"Set {ApiScenario.AdminSetting} privately to run disposable PostgreSQL/API checks.";
    }
}

/// <summary>One owned database and real ASP.NET pipeline per scenario; never share across tests.</summary>
internal sealed class ApiScenario : IDbContextFactory<EquipmentBorrowingDbContext>, IAsyncDisposable
{
    public const string AdminSetting = "POSTGRES_INTEGRATION_ADMIN_CONNECTION";
    public static readonly DateOnly D = new(2026, 11, 18);
    private readonly string adminConnection;
    private readonly string connection;
    private readonly ITestOutputHelper output;
    private readonly TestApi host;
    private readonly ICheckoutWriteObserver? observer;
    private bool disposed;
    public string DatabaseName { get; } = "equipment_borrowing_lab6_" + Guid.NewGuid().ToString("N");
    public HttpClient Client { get; private set; } = null!;
    public IReadOnlyDictionary<string, StudentResponse> Students { get; private set; } = new Dictionary<string, StudentResponse>();
    public IReadOnlyDictionary<string, EquipmentResponse> Equipment { get; private set; } = new Dictionary<string, EquipmentResponse>();

    private ApiScenario(ITestOutputHelper output, ICheckoutWriteObserver? observer)
    {
        this.output = output;
        this.observer = observer;
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable(AdminSetting)
            ?? throw new InvalidOperationException($"Set {AdminSetting} privately."))
        {
            Database = "postgres", Pooling = false, IncludeErrorDetail = false,
            Timeout = 10, CommandTimeout = 30
        };
        adminConnection = builder.ConnectionString;
        builder.Database = DatabaseName;
        connection = builder.ConnectionString;
        host = new TestApi(this);
    }

    public static async Task<ApiScenario> CreateAsync(ITestOutputHelper output, ICheckoutWriteObserver? observer = null)
    {
        var scenario = new ApiScenario(output, observer);
        var created = false;
        try
        {
            await using (var admin = new NpgsqlConnection(scenario.adminConnection))
            {
                await admin.OpenAsync();
                // Generated hexadecimal suffix; never a caller-supplied database identifier.
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{scenario.DatabaseName}\"", admin);
                await create.ExecuteNonQueryAsync();
                created = true;
            }
            await using (var context = scenario.CreateDbContext())
                await context.Database.MigrateAsync(); // No demo bootstrapper/seeding.
            scenario.Client = scenario.host.CreateClient();
            await scenario.ResetAsync();
            output.WriteLine($"Created isolated database {scenario.DatabaseName}; fixed campus D={D:yyyy-MM-dd}; pooling disabled.");
            return scenario;
        }
        catch
        {
            if (created) await scenario.DisposeAsync();
            else await scenario.host.DisposeAsync();
            // Provider exceptions may contain connection details. Keep private setup failures out of TRX.
            throw new InvalidOperationException("Isolated API/PostgreSQL setup failed; check private configuration and CREATE DATABASE permission.");
        }
    }

    public EquipmentBorrowingDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<EquipmentBorrowingDbContext>().UseNpgsql(connection).Options);

    public NpgsqlConnection CreateFreshConnection() => new(connection);

    public HttpClient CreateClient() => host.CreateClient();

    public HttpMessageHandler CreateHandler() => host.Server.CreateHandler();

    public async Task ResetAsync(bool seed = true)
    {
        await using (var context = CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync("TRUNCATE \"Borrowings\", \"Equipment\", \"Students\" RESTART IDENTITY CASCADE;");
            if (seed)
            {
                var students = new[]
                {
                    new Student(0, "ST-100", "Ada"), new Student(0, "ST-200", "Ben", isEligibleToBorrow: false),
                    new Student(0, "ST-300", "Cara"), new Student(0, "ST-400", "Dani")
                };
                var items = new[] { "Camera", "Laptop", "Projector", "Tablet", "Microphone", "Tripod", "Speaker" }
                    .Select((name, index) => new Equipment(0, $"EQ-{index + 1:000}", name)).ToArray();
                context.AddRange(students);
                context.AddRange(items);
                await context.SaveChangesAsync();
                foreach (var (student, item, borrowed, due, returned) in new (int, int, int, int, int?)[]
                {
                    (0, 0, -9, -1, null), (0, 2, -8, 2, -2), (2, 3, -7, 2, null), (2, 4, -7, 2, null)
                })
                {
                    var loan = new Borrowing(students[student].Id, items[item].Id, D.AddDays(borrowed), D.AddDays(due));
                    if (returned is int offset) loan.MarkAsReturned(D.AddDays(offset));
                    else items[item].MarkAsBorrowed();
                    context.Add(loan);
                }
                await context.SaveChangesAsync();
            }
        }
        // Callers use opaque IDs discovered through the real API, not assumed sequence values.
        Students = (await GetAsync<StudentResponse[]>("/api/v1/students")).ToDictionary(s => s.StudentNumber);
        Equipment = (await GetAsync<EquipmentResponse[]>("/api/v1/equipment")).ToDictionary(e => e.EquipmentCode);
    }

    public async Task<T> GetAsync<T>(string path)
    {
        using var response = await Client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public async Task<string> SnapshotAsync()
    {
        // A new physical connection (Pooling=false), independent of the writer/context and HTTP host.
        await using var fresh = CreateFreshConnection();
        await fresh.OpenAsync();
        await using var query = new NpgsqlCommand("""
            SELECT json_build_object(
              'students', COALESCE((SELECT json_agg(s ORDER BY s."Id") FROM "Students" s), '[]'::json),
              'equipment', COALESCE((SELECT json_agg(e ORDER BY e."Id") FROM "Equipment" e), '[]'::json),
              'borrowings', COALESCE((SELECT json_agg(b ORDER BY b."Id") FROM "Borrowings" b), '[]'::json))::text;
            """, fresh);
        return (string)(await query.ExecuteScalarAsync())!;
    }

    public async Task<bool> DatabaseExistsAsync()
    {
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var query = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = @name)", admin);
        query.Parameters.AddWithValue("name", DatabaseName);
        return (bool)(await query.ExecuteScalarAsync())!;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        // Close only this scenario's client/host; do not clear global pools or stop shared PostgreSQL.
        Client?.Dispose();
        await host.DisposeAsync();
        if (!Regex.IsMatch(DatabaseName, "^equipment_borrowing_lab6_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Refusing cleanup of an unowned database name.");
        try
        {
            await using var admin = new NpgsqlConnection(adminConnection);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{DatabaseName}\"", admin);
            await drop.ExecuteNonQueryAsync();
            disposed = true;
            output.WriteLine($"Dropped owned database {DatabaseName}.");
        }
        catch
        {
            throw new InvalidOperationException($"Cleanup failed for owned database {DatabaseName}; check it privately before retrying cleanup.");
        }
    }

    private sealed class FixedClock : ICampusClock { public DateOnly Today => D; }

    private sealed class TestApi(ApiScenario scenario) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICampusClock>();
                services.AddSingleton<ICampusClock, FixedClock>();
                services.RemoveAll<IDbContextFactory<EquipmentBorrowingDbContext>>();
                services.AddSingleton<IDbContextFactory<EquipmentBorrowingDbContext>>(scenario);
                if (scenario.observer is not null)
                    services.AddSingleton<ICheckoutWriteObserver>(scenario.observer);
            });
        }
    }
}
