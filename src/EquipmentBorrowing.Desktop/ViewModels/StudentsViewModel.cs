using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class StudentsViewModel(StudentManagementService service) : ObservableObject
{
    private StudentSnapshot? _original;
    [ObservableProperty] private ObservableCollection<Student> _students = new();
    [ObservableProperty] private Student? _selectedStudent;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _studentNumber = "";
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _contactInformation = "";
    [ObservableProperty] private string _eligibilityText = "New students are eligible to borrow.";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _loadMessage = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isReady;
    [ObservableProperty] private bool _needsVerification;
    public bool CanEdit => !IsBusy && IsReady && !NeedsVerification;
    private bool CanRefresh() => !IsBusy;
    private bool CanWrite() => CanEdit;

    partial void OnIsBusyChanged(bool value) => NotifyState();
    partial void OnIsReadyChanged(bool value) => NotifyState();
    partial void OnNeedsVerificationChanged(bool value) => NotifyState();
    private void NotifyState()
    {
        OnPropertyChanged(nameof(CanEdit));
        RefreshCommand.NotifyCanExecuteChanged(); NewStudentCommand.NotifyCanExecuteChanged();
        RegisterCommand.NotifyCanExecuteChanged(); SaveChangesCommand.NotifyCanExecuteChanged();
        PermitCommand.NotifyCanExecuteChanged(); RestrictCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedStudentChanged(Student? value)
    {
        _original = value is null ? null : StudentSnapshot.From(value);
        StudentNumber = value?.StudentNumber ?? "";
        FullName = value?.FullName ?? "";
        ContactInformation = value?.ContactInformation ?? "";
        EligibilityText = value is null ? "New students are eligible to borrow." :
            value.IsEligibleToBorrow ? "Eligible to borrow" : "Restricted from new borrowing; existing loans can be returned.";
    }

    public Task InitializeAsync() => RefreshAsync();
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await LoadAsync(); }
        finally { IsBusy = false; }
    }

    private async Task LoadAsync(int? selectId = null)
    {
        var wasUnknown = NeedsVerification;
        IsReady = false;
        LoadMessage = "Loading students...";
        SelectedStudent = null;
        var result = await service.FindAsync(SearchText);
        Students.Clear();
        if (!result.Success || result.Value is null) { LoadMessage = result.Message; return; }
        foreach (var student in result.Value)
        {
            Students.Add(student);
            if (student.Id == selectId) SelectedStudent = student;
        }
        LoadMessage = Students.Count == 0 ? "No students match your search." : "";
        NeedsVerification = false;
        IsReady = true;
        if (wasUnknown) StatusMessage = "Students reloaded. Review the records before deciding whether to submit again.";
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private void NewStudent()
    {
        SelectedStudent = null;
        _original = null;
        StudentNumber = ""; FullName = ""; ContactInformation = "";
        EligibilityText = "New students are eligible to borrow.";
        StatusMessage = "Enter a student number and full name; contact information is optional.";
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task RegisterAsync()
    {
        if (!CanWrite()) return;
        if (SelectedStudent is not null) { StatusMessage = "Choose New student before registering another student."; return; }
        var request = new RegisterStudentRequest(StudentNumber, FullName, ContactInformation);
        await RunAsync(() => service.RegisterAsync(request));
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task SaveChangesAsync()
    {
        if (!CanWrite()) return;
        if (SelectedStudent is null || _original is null) { StatusMessage = "Find and select a student to edit."; return; }
        var request = new UpdateStudentRequest(SelectedStudent.Id, StudentNumber, FullName, ContactInformation, _original);
        await RunAsync(() => service.UpdateAsync(request));
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private Task PermitAsync() => ChangeEligibilityAsync(true);
    [RelayCommand(CanExecute = nameof(CanWrite))]
    private Task RestrictAsync() => ChangeEligibilityAsync(false);

    private async Task ChangeEligibilityAsync(bool eligible)
    {
        if (!CanWrite()) return;
        if (SelectedStudent is null || _original is null) { StatusMessage = "Find and select a student first."; return; }
        if (StudentNumber != _original.StudentNumber || FullName != _original.FullName || ContactInformation != (_original.ContactInformation ?? ""))
        { StatusMessage = "Save descriptive edits, or reselect the record, before changing eligibility."; return; }
        var request = new SetStudentEligibilityRequest(SelectedStudent.Id, eligible, _original);
        await RunAsync(() => service.SetEligibilityAsync(request));
    }

    private async Task RunAsync(Func<Task<OperationResult<Student>>> action)
    {
        IsBusy = true;
        try
        {
            var result = await action();
            StatusMessage = result.Message;
            NeedsVerification = result.Outcome == OperationOutcome.Unknown;
            if (result.Outcome == OperationOutcome.Failed) IsReady = false;
            if (result.Outcome is OperationOutcome.Succeeded or OperationOutcome.Conflict)
                await LoadAsync(result.Value?.Id);
        }
        finally { IsBusy = false; }
    }
}
