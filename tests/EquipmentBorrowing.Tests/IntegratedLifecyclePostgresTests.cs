using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace EquipmentBorrowing.Tests;

public class IntegratedLifecyclePostgresTests(ITestOutputHelper output)
{
    private sealed class Clock : ICampusClock { public DateOnly Today => new(2026,11,14); }
    private static readonly Clock CampusClock = new();

    [PostgresIntegrationFact]
    public async Task RegisterEditBorrowRestrictReturnCheckoutAndReloadPreserveHistory()
    {
        var factory = await PostgresVerificationDatabase.CreateAsync(output);
        await new PostgresBootstrapper(factory).InitializeAsync();
        await using (var initial = factory.CreateDbContext())
        {
            Assert.Equal(3, await initial.Students.CountAsync()); Assert.Equal(3, await initial.Equipment.CountAsync());
            Assert.Single(await initial.Database.GetAppliedMigrationsAsync());
        }
        var sessions = new PostgresBorrowingSessionFactory(factory); var failures = new PostgresWorkflowFailures();
        var students = new StudentManagementService(sessions, new EfStudentRepository(factory), failures);
        var equipment = new EquipmentManagementService(sessions, new EfEquipmentRepository(factory), failures);
        var registered = await students.RegisterAsync(new(" INTEGRATED-1 ", " Registered Student ", "contact"));
        Assert.Equal(OperationOutcome.Succeeded, registered.Outcome);
        var student = registered.Value!; Assert.True(student.Id > 0);
        Assert.Equal(OperationOutcome.Rejected, (await students.RegisterAsync(new("integrated-1", "Duplicate", null))).Outcome);
        var edited = await students.UpdateAsync(new(student.Id, "INTEGRATED-EDIT", "Edited Student", " ", StudentSnapshot.From(student)));
        Assert.Equal(OperationOutcome.Succeeded, edited.Outcome); student = edited.Value!;
        Assert.Null(student.ContactInformation);
        var items = new List<Equipment>();
        for (var i=1;i<=4;i++)
        {
            var result = await equipment.RegisterAsync(new("INT-E"+i, "Integrated Item "+i, "description"));
            Assert.Equal(OperationOutcome.Succeeded, result.Outcome); Assert.True(result.Value!.IsAvailable); items.Add(result.Value);
        }
        Assert.Equal(OperationOutcome.Rejected, (await equipment.RegisterAsync(new(" int-e1 ", "Duplicate", null))).Outcome);
        var edit = await equipment.UpdateAsync(new(items[0].Id, "INT-EDIT", "Edited Item", " ", EquipmentSnapshot.From(items[0])));
        Assert.Equal(OperationOutcome.Succeeded, edit.Outcome); items[0] = edit.Value!; Assert.Null(items[0].Description);
        BorrowEquipmentService Borrow() => new(new EfStudentRepository(factory), new EfEquipmentRepository(factory), new EfBorrowingRepository(factory), CampusClock, new PostgresBorrowingSessionFactory(factory), failures);
        var issue = await Borrow().ExecuteAsync(new(student.Id, items[0].Id, CampusClock.Today.AddDays(3)));
        Assert.True(issue.Success, issue.Message);
        await using (var fresh = factory.CreateDbContext())
        {
            Assert.Equal(1, await fresh.Borrowings.CountAsync());
            Assert.False((await fresh.Equipment.SingleAsync(e=>e.Id==items[0].Id)).IsAvailable);
        }
        var restrict = await students.SetEligibilityAsync(new(student.Id, false, StudentSnapshot.From(student)));
        Assert.Equal(OperationOutcome.Succeeded, restrict.Outcome); student = restrict.Value!;
        Assert.Equal(OperationOutcome.Rejected, (await Borrow().ExecuteAsync(new(student.Id, items[1].Id, CampusClock.Today.AddDays(3)))).Outcome);
        var returned = await new ReturnEquipmentService(new EfBorrowingRepository(factory), new EfEquipmentRepository(factory), CampusClock,
            new PostgresBorrowingSessionFactory(factory), failures).ExecuteAsync(issue.Borrowing!.Id);
        Assert.True(returned.Success, returned.Message);
        await using (var fresh = factory.CreateDbContext())
        {
            var row = await fresh.Borrowings.SingleAsync();
            Assert.Equal(BorrowingStatus.Returned, row.Status); Assert.Equal(CampusClock.Today, row.ActualReturnAt);
            Assert.True((await fresh.Equipment.SingleAsync(e=>e.Id==items[0].Id)).IsAvailable);
        }
        var permit = await students.SetEligibilityAsync(new(student.Id, true, StudentSnapshot.From(student)));
        Assert.Equal(OperationOutcome.Succeeded, permit.Outcome);
        var multi = await new MultiItemCheckoutService(sessions, CampusClock, failures).ExecuteAsync(new(student.Id,
            [items[0].Id,items[1].Id], CampusClock.Today.AddDays(2)));
        Assert.Equal(OperationOutcome.Succeeded, multi.Outcome);
        Assert.True((await Borrow().ExecuteAsync(new(student.Id, items[2].Id, CampusClock.Today.AddDays(1)))).Success);
        Assert.Equal(OperationOutcome.Rejected, (await Borrow().ExecuteAsync(new(student.Id, items[3].Id, CampusClock.Today.AddDays(1)))).Outcome);
        var currentItem = (await new EfEquipmentRepository(factory).GetByIdAsync(items[0].Id))!;
        var stale = await equipment.UpdateAsync(new(currentItem.Id, "WRONG", "Stale", null, EquipmentSnapshot.From(items[0])));
        Assert.Equal(OperationOutcome.Conflict, stale.Outcome);
        var updateBorrowed = await equipment.UpdateAsync(new(currentItem.Id, currentItem.EquipmentIdentifier, "Renamed borrowed item", null, EquipmentSnapshot.From(currentItem)));
        Assert.Equal(OperationOutcome.Succeeded, updateBorrowed.Outcome); Assert.False(updateBorrowed.Value!.IsAvailable);
        await new PostgresBootstrapper(factory).InitializeAsync(); // Existing edits are not overwritten.
        var history = (await new EfHistoryQueries(factory).GetHistoryAsync(HistoryKind.Student,student.Id))!;
        Assert.Equal(4, history.Rows.Count); Assert.Single(history.Rows,b=>b.Status==BorrowingStatus.Returned);
        var search = await new BorrowingSearchService(new EfBorrowingSearchQueries(factory)).SearchAsync(new(" integrated-edit "));
        Assert.True(search.Success,search.Message); Assert.Equal(4,search.Value!.Count);
        await using var final = factory.CreateDbContext();
        Assert.Equal(3, await final.Borrowings.CountAsync(b=>b.StudentId==student.Id && b.Status==BorrowingStatus.Active));
        Assert.Equal("Edited Student",(await final.Students.SingleAsync(s=>s.Id==student.Id)).FullName);
        Assert.Equal(4,await final.Students.CountAsync()); Assert.Equal(7,await final.Equipment.CountAsync());
        output.WriteLine("Lifecycle verified across new service/context instances; this is not an OS process restart demonstration.");
    }

    [PostgresIntegrationFact]
    public async Task CompetingRequestsCannotExceedStudentQuotaOrDuplicateActiveItem()
    {
        var factory = await PostgresVerificationDatabase.CreateAsync(output);
        var failures = new PostgresWorkflowFailures();
        int studentId; int[] ids;
        await using (var context=factory.CreateDbContext())
        {
            var student=new Student(0,"CONCURRENT","Concurrent Student");
            var items=Enumerable.Range(1,4).Select(i=>new Equipment(0,"CON-E"+i,"Concurrent "+i)).ToArray();
            context.Add(student);context.AddRange(items);await context.SaveChangesAsync();studentId=student.Id;ids=items.Select(i=>i.Id).ToArray();
        }
        MultiItemCheckoutService Multi()=>new(new PostgresBorrowingSessionFactory(factory),CampusClock,failures);
        var results=await Task.WhenAll(Multi().ExecuteAsync(new(studentId,ids.Take(2).ToArray(),CampusClock.Today.AddDays(1))),
            Multi().ExecuteAsync(new(studentId,ids.Skip(2).ToArray(),CampusClock.Today.AddDays(1))));
        Assert.Single(results,r=>r.Outcome==OperationOutcome.Succeeded);
        Assert.All(results.Where(r=>r.Outcome!=OperationOutcome.Succeeded),r=>Assert.Contains(r.Outcome,new[]{OperationOutcome.Rejected,OperationOutcome.Conflict}));
        int freeItem;
        await using(var fresh=factory.CreateDbContext())
        {
            Assert.Equal(2,await fresh.Borrowings.CountAsync(b=>b.StudentId==studentId));
            Assert.Equal(2,await fresh.Equipment.CountAsync(e=>ids.Contains(e.Id)&&!e.IsAvailable));
            freeItem=await fresh.Equipment.Where(e=>ids.Contains(e.Id)&&e.IsAvailable).Select(e=>e.Id).FirstAsync();
        }
        BorrowEquipmentService Single()=>new(new EfStudentRepository(factory),new EfEquipmentRepository(factory),new EfBorrowingRepository(factory),CampusClock,new PostgresBorrowingSessionFactory(factory),failures);
        var sameItem=await Task.WhenAll(Single().ExecuteAsync(new(studentId,freeItem,CampusClock.Today.AddDays(1))),Single().ExecuteAsync(new(studentId,freeItem,CampusClock.Today.AddDays(1))));
        Assert.Single(sameItem,r=>r.Success);
        await using var readback=factory.CreateDbContext();
        Assert.Equal(3,await readback.Borrowings.CountAsync(b=>b.StudentId==studentId&&b.Status==BorrowingStatus.Active));
        Assert.Equal(1,await readback.Borrowings.CountAsync(b=>b.EquipmentId==freeItem&&b.Status==BorrowingStatus.Active));
    }

    [PostgresIntegrationFact]
    public async Task FailedConnectionDoesNotReportSuccessAndRecoveryWorks()
    {
        var factory=await PostgresVerificationDatabase.CreateAsync(output);
        var bad=new PostgresVerificationDatabase(new DbContextOptionsBuilder<EquipmentBorrowingDbContext>()
            .UseNpgsql("Host=localhost;Port=1;Database=unavailable;Username=verification;Timeout=1;Command Timeout=1").Options);
        Assert.False(await new ApplicationStartupService(new PostgresBootstrapper(bad)).InitializeAsync());
        var failedQuery=await new ExistingWorkflowQueryService(new EfExistingWorkflowQueries(bad)).GetCatalogAsync(false);
        Assert.False(failedQuery.Success);Assert.Null(failedQuery.Value);
        var failedWrite=await new MultiItemCheckoutService(new PostgresBorrowingSessionFactory(bad),CampusClock,new PostgresWorkflowFailures())
            .ExecuteAsync(new(1,[1,2],CampusClock.Today.AddDays(1)));
        Assert.Equal(OperationOutcome.Failed,failedWrite.Outcome);
        Assert.True(await new ApplicationStartupService(new PostgresBootstrapper(factory)).InitializeAsync());
        Assert.True((await new ExistingWorkflowQueryService(new EfExistingWorkflowQueries(factory)).GetCatalogAsync(false)).Success);
        await using var fresh=factory.CreateDbContext();Assert.Equal(0,await fresh.Borrowings.CountAsync());
    }
}
