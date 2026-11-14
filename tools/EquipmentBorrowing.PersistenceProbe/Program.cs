using System.Text.RegularExpressions;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

try
{
    var stage=args.Single();
    var connection=new NpgsqlConnectionStringBuilder(PostgresConfiguration.GetConnectionString());
    if (!Regex.IsMatch(connection.Database ?? "", "^equipment_borrowing_verify_process_[a-z0-9_]+$") || connection.Database!.Length>63)
        throw new InvalidOperationException("The probe requires a dedicated verification database name.");
    if(stage=="init")
    {
        var database=connection.Database;connection.Database="postgres";
        await using var admin=new NpgsqlConnection(connection.ConnectionString);await admin.OpenAsync();
        await using var command=new NpgsqlCommand($"CREATE DATABASE \"{database}\"",admin);
        await command.ExecuteNonQueryAsync();connection.Database=database;
    }
    var factory=new ProbeFactory(connection.ConnectionString);
    var sessions=new PostgresBorrowingSessionFactory(factory);var failures=new PostgresWorkflowFailures();var clock=new CampusClock();
    if(stage=="init")
    {
        await new PostgresBootstrapper(factory).InitializeAsync();
        var student=await new StudentManagementService(sessions,new EfStudentRepository(factory),failures)
            .RegisterAsync(new("PROCESS-S1","Process Student",null));
        var item=await new EquipmentManagementService(sessions,new EfEquipmentRepository(factory),failures)
            .RegisterAsync(new("PROCESS-E1","Process Equipment",null));
        Require(student.Outcome==OperationOutcome.Succeeded && item.Outcome==OperationOutcome.Succeeded);
    }
    else
    {
        await using var context=factory.CreateDbContext();
        var student=await context.Students.AsNoTracking().SingleAsync(s=>s.StudentNumber=="PROCESS-S1");
        var item=await context.Equipment.AsNoTracking().SingleAsync(e=>e.EquipmentIdentifier=="PROCESS-E1");
        switch(stage)
        {
            case "borrow":
                Require((await new BorrowEquipmentService(new EfStudentRepository(factory),new EfEquipmentRepository(factory),
                    new EfBorrowingRepository(factory),clock,sessions,failures).ExecuteAsync(new(student.Id,item.Id,clock.Today.AddDays(2)))).Success);break;
            case "verify-borrow":
                var active=await context.Borrowings.AsNoTracking().SingleAsync();
                Require(active.StudentId==student.Id && active.EquipmentId==item.Id && active.Status==BorrowingStatus.Active && !item.IsAvailable);break;
            case "return":
                var loan=await context.Borrowings.AsNoTracking().SingleAsync();
                Require((await new ReturnEquipmentService(new EfBorrowingRepository(factory),new EfEquipmentRepository(factory),clock,sessions,failures)
                    .ExecuteAsync(loan.Id)).Success);break;
            case "verify-return":
                var returned=await context.Borrowings.AsNoTracking().SingleAsync();
                Require(returned.StudentId==student.Id && returned.EquipmentId==item.Id && returned.Status==BorrowingStatus.Returned
                    && returned.ActualReturnAt==clock.Today && item.IsAvailable);break;
            default:throw new InvalidOperationException("Unknown probe stage.");
        }
    }
    Console.WriteLine("PASS: separate-process persistence stage "+stage);
    return 0;
}
catch(Exception error)
{
    Console.Error.WriteLine("Persistence probe failed ("+error.GetType().Name+"). Credentials are omitted.");return 1;
}

static void Require(bool condition) { if(!condition) throw new InvalidOperationException("Persistence verification failed."); }
sealed class ProbeFactory(string connection) : IDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext()=>new(new DbContextOptionsBuilder<EquipmentBorrowingDbContext>().UseNpgsql(connection).Options);
}
