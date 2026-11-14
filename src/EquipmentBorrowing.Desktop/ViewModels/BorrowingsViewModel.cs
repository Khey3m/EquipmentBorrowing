using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ObservableObject
{
    private readonly ReturnEquipmentService _returns;
    private readonly ExistingWorkflowQueryService _queries;
    [ObservableProperty] private ObservableCollection<BorrowingDetails> _activeBorrowings = new();
    [ObservableProperty] private BorrowingDetails? _selectedBorrowing;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _loadMessage = "";
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private bool _confirmReturn;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ReturnCommand)), NotifyCanExecuteChangedFor(nameof(RefreshCommand))] private bool _isBusy;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ReturnCommand))] private bool _isReady;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ReturnCommand))] private bool _needsVerification;

    public BorrowingsViewModel(ReturnEquipmentService returns, ExistingWorkflowQueryService queries)
    { _returns = returns; _queries = queries; }

    public Task InitializeAsync() => RefreshAsync();
    private bool CanRefresh() => !IsBusy;
    private bool CanReturn() => !IsBusy && IsReady && !NeedsVerification;
    partial void OnSelectedBorrowingChanged(BorrowingDetails? value) => ConfirmReturn = false;

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
        IsReady = false;
        IsEmpty = false;
        SelectedBorrowing = null;
        LoadMessage = "Loading active borrowings...";
        var result = await _queries.GetActiveBorrowingsAsync();
        ActiveBorrowings.Clear();
        if (!result.Success || result.Value is null) { LoadMessage = result.Message; return; }
        foreach (var row in result.Value) ActiveBorrowings.Add(row);
        IsEmpty = ActiveBorrowings.Count == 0;
        LoadMessage = IsEmpty ? "No active borrowings." : "";
        NeedsVerification = false;
        IsReady = true;
        if (wasUnknown) StatusMessage = "Records reloaded. Review the current records before deciding whether to submit again.";
    }

    [RelayCommand(CanExecute = nameof(CanReturn))]
    private async Task ReturnAsync()
    {
        if (!CanReturn()) return;
        IsSuccess = false;
        if (SelectedBorrowing is null || !ConfirmReturn)
        { StatusMessage = "Select an active borrowing and confirm its return."; return; }
        var id = SelectedBorrowing.Id;
        IsBusy = true;
        try
        {
            var result = await _returns.ExecuteAsync(id);
            StatusMessage = result.Message;
            IsSuccess = result.Success;
            NeedsVerification = result.Outcome == OperationOutcome.Unknown;
            if (result.Outcome == OperationOutcome.Failed) IsReady = false;
            ConfirmReturn = false;
            if (result.Success || result.Outcome == OperationOutcome.Conflict) await LoadAsync();
        }
        finally { IsBusy = false; }
    }
}
