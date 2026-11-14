using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MultiItemCheckoutViewModel : ObservableObject
{
    private readonly MultiItemCheckoutService _checkout;
    private readonly ExistingWorkflowQueryService _queries;
    [ObservableProperty] private ObservableCollection<CheckoutItem> _equipmentList = new();
    [ObservableProperty] private ObservableCollection<Student> _studentList = new();
    [ObservableProperty] private Student? _selectedStudent;
    [ObservableProperty] private DateTimeOffset? _expectedReturnDate;
    [ObservableProperty] private bool _availableOnly;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _loadMessage = "";
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(CheckoutCommand)), NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyPropertyChangedFor(nameof(CanSelect))]
    private bool _isBusy;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(CheckoutCommand))] [NotifyPropertyChangedFor(nameof(CanSelect))] private bool _isReady;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(CheckoutCommand))] [NotifyPropertyChangedFor(nameof(CanSelect))] private bool _needsVerification;

    public MultiItemCheckoutViewModel(MultiItemCheckoutService checkout, ExistingWorkflowQueryService queries, ICampusClock clock)
    {
        _checkout = checkout;
        _queries = queries;
        ExpectedReturnDate = new DateTimeOffset(clock.Today.AddDays(3).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    public Task InitializeAsync() => RefreshAsync();
    public int SelectedCount => EquipmentList.Count(row => row.IsSelected);
    public bool CanSelect => CanCheckout();
    private bool CanRefresh() => !IsBusy;
    private bool CanCheckout() => !IsBusy && IsReady && !NeedsVerification;

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

        var result = await _queries.GetCatalogAsync(AvailableOnly);
        EquipmentList.Clear();
        OnPropertyChanged(nameof(SelectedCount));
        StudentList.Clear();
        SelectedStudent = null;
        if (!result.Success || result.Value is null) { LoadMessage = result.Message; return; }
        foreach (var item in result.Value.Equipment) EquipmentList.Add(new CheckoutItem(item, () => OnPropertyChanged(nameof(SelectedCount))));
        OnPropertyChanged(nameof(SelectedCount));
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

    [RelayCommand(CanExecute = nameof(CanCheckout))]
    private async Task CheckoutAsync()
    {
        if (!CanCheckout()) return;
        IsSuccess = false;
        if (SelectedStudent is null || ExpectedReturnDate is null || SelectedCount < 2)
        { StatusMessage = "Select a student, at least two distinct items and an expected return date."; return; }
        var ids = EquipmentList.Where(row => row.IsSelected).Select(row => row.Item.Id).ToArray();
        var request = new MultiItemCheckoutRequest(SelectedStudent.Id, ids, DateOnly.FromDateTime(ExpectedReturnDate.Value.Date));
        IsBusy = true;
        try
        {
            var result = await _checkout.ExecuteAsync(request);
            StatusMessage = result.Message;
            IsSuccess = result.Outcome == OperationOutcome.Succeeded;
            NeedsVerification = result.Outcome == OperationOutcome.Unknown;
            if (result.Outcome == OperationOutcome.Failed) IsReady = false;
            if (result.Outcome is OperationOutcome.Succeeded or OperationOutcome.Conflict) await LoadAsync();
        }
        finally { IsBusy = false; }
    }
}

public partial class CheckoutItem(Equipment item, Action changed) : ObservableObject
{
    public Equipment Item => item;
    [ObservableProperty] private bool _isSelected;
    partial void OnIsSelectedChanged(bool value) => changed();
}
