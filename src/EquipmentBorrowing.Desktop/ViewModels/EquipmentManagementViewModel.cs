using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentManagementViewModel(IEquipmentManagementWorkflow service) : ObservableObject
{
    private EquipmentSnapshot? _original;
    [ObservableProperty] private ObservableCollection<Equipment> _items = new();
    [ObservableProperty] private Equipment? _selectedEquipment;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _equipmentIdentifier = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _availabilityText = "New equipment is available for borrowing.";
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
        RefreshCommand.NotifyCanExecuteChanged(); NewEquipmentCommand.NotifyCanExecuteChanged();
        RegisterCommand.NotifyCanExecuteChanged(); SaveChangesCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        _original = value is null ? null : EquipmentSnapshot.From(value);
        EquipmentIdentifier = value?.EquipmentIdentifier ?? "";
        Name = value?.Name ?? "";
        Description = value?.Description ?? "";
        AvailabilityText = value is null ? "New equipment is available for borrowing." :
            value.IsAvailable ? "Available" : "Unavailable. Use the borrowing/return workflow to change this state.";
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
        LoadMessage = "Loading equipment...";
        SelectedEquipment = null;
        var result = await service.FindAsync(SearchText);
        Items.Clear();
        if (!result.Success || result.Value is null) { LoadMessage = result.Message; return; }
        foreach (var item in result.Value)
        {
            Items.Add(item);
            if (item.Id == selectId) SelectedEquipment = item;
        }
        LoadMessage = Items.Count == 0 ? "No equipment matches your search." : "";
        NeedsVerification = false;
        IsReady = true;
        if (wasUnknown) StatusMessage = "Equipment reloaded. Review the records before deciding whether to submit again.";
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private void NewEquipment()
    {
        SelectedEquipment = null;
        _original = null;
        EquipmentIdentifier = ""; Name = ""; Description = "";
        AvailabilityText = "New equipment is available for borrowing.";
        StatusMessage = "Enter an equipment identifier and name; description is optional.";
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task RegisterAsync()
    {
        if (!CanWrite()) return;
        if (SelectedEquipment is not null) { StatusMessage = "Choose New equipment before registering another item."; return; }
        var request = new RegisterEquipmentRequest(EquipmentIdentifier, Name, Description);
        await RunAsync(() => service.RegisterAsync(request));
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task SaveChangesAsync()
    {
        if (!CanWrite()) return;
        if (SelectedEquipment is null || _original is null) { StatusMessage = "Find and select an item to edit."; return; }
        var request = new UpdateEquipmentRequest(SelectedEquipment.Id, EquipmentIdentifier, Name, Description, _original);
        await RunAsync(() => service.UpdateAsync(request));
    }

    private async Task RunAsync(Func<Task<OperationResult<Equipment>>> action)
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
