using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ObservableObject
{
    private readonly BorrowEquipmentService _borrow;
    private readonly ExistingWorkflowQueryService _queries;
    [ObservableProperty] private ObservableCollection<Equipment> _equipmentList = new();
    [ObservableProperty] private ObservableCollection<Student> _studentList = new();
    [ObservableProperty] private Equipment? _selectedEquipment;
    [ObservableProperty] private Student? _selectedStudent;
    [ObservableProperty] private DateTimeOffset? _expectedReturnDate;
    [ObservableProperty] private bool _availableOnly;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _loadMessage = "";
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(BorrowCommand)), NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isBusy;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(BorrowCommand))] private bool _isReady;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(BorrowCommand))] private bool _needsVerification;

    public EquipmentViewModel(BorrowEquipmentService borrow, ExistingWorkflowQueryService queries, ICampusClock clock)
    {
        _borrow = borrow;
        _queries = queries;
        ExpectedReturnDate = new DateTimeOffset(clock.Today.AddDays(3).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    public Task InitializeAsync() => RefreshAsync();
    private bool CanRefresh() => !IsBusy;
    private bool CanBorrow() => !IsBusy && IsReady && !NeedsVerification;

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await LoadAsync(); }
        finally { IsBusy = false; }
    }

    private async Task LoadAsync()
    {
        var wasUnknown = NeedsVerification;
        LoadMessage = "Loading catalog...";
        IsReady = false;
        IsEmpty = false;
        var selectedStudentId = SelectedStudent?.Id;
        SelectedEquipment = null;
        var result = await _queries.GetCatalogAsync(AvailableOnly);
        EquipmentList.Clear();
        StudentList.Clear();
        SelectedStudent = null;
        if (!result.Success || result.Value is null) { LoadMessage = result.Message; return; }
        foreach (var item in result.Value.Equipment) EquipmentList.Add(item);
        foreach (var student in result.Value.Students)
        {
            StudentList.Add(student);
            if (student.Id == selectedStudentId) SelectedStudent = student;
        }
        IsEmpty = EquipmentList.Count == 0;
        LoadMessage = IsEmpty ? "No equipment matches this view." : "";
        NeedsVerification = false;
        IsReady = true;
        if (wasUnknown) StatusMessage = "Records reloaded. Review the current catalog and active borrowings before deciding whether to submit again.";
    }

    [RelayCommand(CanExecute = nameof(CanBorrow))]
    private async Task BorrowAsync()
    {
        if (!CanBorrow()) return;
        IsSuccess = false;
        if (SelectedStudent is null || SelectedEquipment is null || ExpectedReturnDate is null)
        { StatusMessage = "Select a student, equipment item and expected return date."; return; }
        var request = new BorrowRequest(SelectedStudent.Id, SelectedEquipment.Id, DateOnly.FromDateTime(ExpectedReturnDate.Value.Date));
        IsBusy = true;
        try
        {
            var result = await _borrow.ExecuteAsync(request);
            StatusMessage = result.Message;
            IsSuccess = result.Success;
            NeedsVerification = result.Outcome == OperationOutcome.Unknown;
            if (result.Outcome == OperationOutcome.Failed) IsReady = false;
            if (result.Success || result.Outcome == OperationOutcome.Conflict) await LoadAsync();
        }
        finally { IsBusy = false; }
    }
}
