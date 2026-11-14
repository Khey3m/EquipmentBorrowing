using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Domain;
using Xunit;

namespace EquipmentBorrowing.UiTests;

public class WorkflowUiTests
{
    private static BorrowingDetails Row(int id, string student) => new(id,id,"S"+id,student,id,"E"+id,"Item "+id,
        new(2026,11,14),new(2026,11,17),null,BorrowingStatus.Active);

    private sealed class SearchQueries : IBorrowingSearchQueries
    {
        public TaskCompletionSource<IReadOnlyList<BorrowingDetails>> Slow { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<BorrowingDetails>> SearchAsync(BorrowingSearchRequest request,CancellationToken cancellationToken=default) =>
            request.SearchText=="Alice" ? Slow.Task : Task.FromResult<IReadOnlyList<BorrowingDetails>>(
                request.SearchText=="Bob" ? [Row(2,"Bob")] : [Row(1,"Alice"),Row(2,"Bob")]);
    }

    [AvaloniaFact]
    public async Task SearchBindingRemainsResponsiveAndLateResultCannotOverwriteNewCriteria()
    {
        var queries=new SearchQueries();var vm=new BorrowingSearchViewModel(new BorrowingSearchService(queries));
        var window=new Window { Width=900,Height=650,Content=new BorrowingSearchView { DataContext=vm } };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var input=((BorrowingSearchView)window.Content!).FindControl<TextBox>("SearchTextInput")!;
            input.Text="Alice";Dispatcher.UIThread.RunJobs();Assert.Equal("Alice",vm.SearchText);
            var old=vm.ApplyCommand.ExecuteAsync(null);Assert.True(vm.IsBusy);
            input.Text="Bob";Dispatcher.UIThread.RunJobs();
            await vm.ApplyCommand.ExecuteAsync(null);Assert.Single(vm.Rows);Assert.Equal("Bob",vm.Rows[0].StudentName);
            queries.Slow.SetResult([Row(1,"Alice")]);await old;
            Assert.Single(vm.Rows);Assert.Equal("Bob",vm.Rows[0].StudentName);
            await vm.ClearCommand.ExecuteAsync(null);Dispatcher.UIThread.RunJobs();
            Assert.Equal("",input.Text);Assert.Equal(2,vm.Rows.Count);Assert.False(vm.IsBusy);
        }
        finally { vm.Deactivate();window.Close(); }
    }

    private sealed class Histories : IHistoryQueries
    {
        public Task<IReadOnlyList<HistorySubject>> GetSubjectsAsync(HistoryKind kind,CancellationToken cancellationToken=default) =>
            Task.FromResult<IReadOnlyList<HistorySubject>>([new(1,"First"),new(2,"Second"),new(3,"Empty"),new(99,"Stale")]);
        public Task<HistorySnapshot?> GetHistoryAsync(HistoryKind kind,int id,CancellationToken cancellationToken=default) =>
            Task.FromResult<HistorySnapshot?>(id==99 ? null : new(new(id,kind+" "+id),id==3 ? [] : [Row(id,id==1?"Alice":"Bob")]));
    }

    [AvaloniaFact]
    public async Task HistorySelectionReplacesRowsAndDistinguishesEmptyFromMissing()
    {
        var vm=new BorrowingHistoryViewModel(new BorrowingHistoryService(new Histories()));
        var window=new Window { Width=900,Height=650,Content=new BorrowingHistoryView { DataContext=vm } };
        window.Show();
        try
        {
            await vm.InitializeAsync();vm.SelectedSubject=vm.Subjects[0];Dispatcher.UIThread.RunJobs();
            Assert.Single(vm.Rows);Assert.Equal("Alice",vm.Rows[0].StudentName);
            vm.SelectedSubject=vm.Subjects[1];Dispatcher.UIThread.RunJobs();
            Assert.Single(vm.Rows);Assert.Equal("Bob",vm.Rows[0].StudentName);
            vm.SelectedSubject=vm.Subjects[2];Assert.Empty(vm.Rows);Assert.Contains("No borrowing history",vm.HistoryMessage);
            vm.SelectedSubject=vm.Subjects[3];Assert.Empty(vm.Rows);Assert.Contains("no longer exists",vm.HistoryMessage);
            vm.SelectedKind=HistoryKind.Equipment;Dispatcher.UIThread.RunJobs();
            Assert.Empty(vm.Rows);Assert.Equal(4,vm.Subjects.Count);
        }
        finally { vm.Deactivate();window.Close(); }
    }

    [AvaloniaFact]
    public void EveryWorkflowViewLoadsAndLaysOutWithoutStartingAProductionDatabase()
    {
        UserControl[] views=[new EquipmentView(),new BorrowingsView(),new StudentsView(),new EquipmentManagementView(),
            new MultiItemCheckoutView(),new BorrowingHistoryView(),new BorrowingSearchView()];
        foreach(var view in views)
        {
            var window=new Window { Width=900,Height=650,Content=view };
            window.Show();Dispatcher.UIThread.RunJobs();
            Assert.True(view.Bounds.Width>0);Assert.True(view.Bounds.Height>0);window.Close();
        }
    }
}
