using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingHistoryViewModel(BorrowingHistoryService service) : ObservableObject
{
    private readonly LatestRequestGate _subjectsGate = new();
    private readonly LatestRequestGate _historyGate = new();
    public HistoryKind[] Kinds { get; } = Enum.GetValues<HistoryKind>();
    [ObservableProperty] private HistoryKind _selectedKind;
    [ObservableProperty] private ObservableCollection<HistorySubject> _subjects = new();
    [ObservableProperty] private HistorySubject? _selectedSubject;
    [ObservableProperty] private ObservableCollection<BorrowingDetails> _rows = new();
    [ObservableProperty] private string _subjectMessage = "";
    [ObservableProperty] private string _historyMessage = "Select a student or equipment item.";
    [ObservableProperty] private string _currentSubjectLabel = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsLoading))] private bool _isSubjectsLoading;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsLoading))] private bool _isHistoryLoading;
    public bool IsLoading => IsSubjectsLoading || IsHistoryLoading;

    partial void OnSelectedKindChanged(HistoryKind value)
    {
        SelectedSubject = null;
        _ = RefreshAsync();
    }
    partial void OnSelectedSubjectChanged(HistorySubject? value) => _ = LoadHistoryAsync(value);
    public Task InitializeAsync() => RefreshAsync();

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task RefreshAsync()
    {
        var request = _subjectsGate.Begin();
        var kind = SelectedKind;
        var previousId = SelectedSubject?.Id;
        _historyGate.Begin();
        SelectedSubject = null;
        Rows.Clear(); Subjects.Clear(); CurrentSubjectLabel = "";
        IsHistoryLoading = false;
        HistoryMessage = "Select a student or equipment item.";
        IsSubjectsLoading = true;
        SubjectMessage = "Loading subjects...";
        try
        {
            var result = await service.GetSubjectsAsync(kind, request.Token);
            if (!request.IsCurrent) return;
            if (!result.Success || result.Value is null) { SubjectMessage = result.Message; return; }
            foreach (var subject in result.Value) Subjects.Add(subject);
            SubjectMessage = Subjects.Count == 0 ? "No students or equipment in this view." : "";
            if (previousId.HasValue) SelectedSubject = Subjects.FirstOrDefault(s => s.Id == previousId.Value);
        }
        catch (OperationCanceledException) when (request.Token.IsCancellationRequested) { }
        catch (Exception) { if (request.IsCurrent) SubjectMessage = "Unable to load subjects. Refresh to retry."; }
        finally { if (request.IsCurrent) IsSubjectsLoading = false; }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task RetryHistoryAsync() => LoadHistoryAsync(SelectedSubject);

    private async Task LoadHistoryAsync(HistorySubject? subject)
    {
        var request = _historyGate.Begin();
        Rows.Clear(); CurrentSubjectLabel = "";
        IsHistoryLoading = subject is not null;
        HistoryMessage = subject is null ? "Select a student or equipment item." : "Loading borrowing history...";
        if (subject is null) return;
        var kind = SelectedKind;
        try
        {
            var result = await service.GetHistoryAsync(kind, subject.Id, request.Token);
            if (!request.IsCurrent) return;
            HistoryMessage = result.Message;
            if (result.Value is null) return;
            CurrentSubjectLabel = result.Value.Subject.Label;
            foreach (var row in result.Value.Rows) Rows.Add(row);
        }
        catch (OperationCanceledException) when (request.Token.IsCancellationRequested) { }
        catch (Exception) { if (request.IsCurrent) HistoryMessage = "Unable to load history. Retry after restoring the connection."; }
        finally { if (request.IsCurrent) IsHistoryLoading = false; }
    }

    public void Deactivate()
    {
        _subjectsGate.Dispose(); _historyGate.Dispose();
        IsSubjectsLoading = false; IsHistoryLoading = false;
    }
}
