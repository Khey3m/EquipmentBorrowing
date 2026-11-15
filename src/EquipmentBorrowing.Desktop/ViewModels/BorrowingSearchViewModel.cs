using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public enum BorrowingStatusFilter { All, Active, Returned }

public partial class BorrowingSearchViewModel(IBorrowingSearchWorkflow service) : ObservableObject
{
    private readonly LatestRequestGate _requests = new();
    public BorrowingStatusFilter[] StatusOptions { get; } = Enum.GetValues<BorrowingStatusFilter>();
    public BorrowingSortField[] SortOptions { get; } = Enum.GetValues<BorrowingSortField>();
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private BorrowingStatusFilter _statusFilter;
    [ObservableProperty] private BorrowingSortField _sortBy;
    [ObservableProperty] private bool _descending;
    [ObservableProperty] private ObservableCollection<BorrowingDetails> _rows = new();
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _message = "";

    partial void OnSearchTextChanged(string value) => CriteriaChanged();
    partial void OnStatusFilterChanged(BorrowingStatusFilter value) => CriteriaChanged();
    partial void OnSortByChanged(BorrowingSortField value) => CriteriaChanged();
    partial void OnDescendingChanged(bool value) => CriteriaChanged();
    private void CriteriaChanged()
    {
        _requests.Dispose(); Rows.Clear(); IsBusy = false;
        Message = "Criteria changed. Apply to load matching borrowings.";
    }
    public Task InitializeAsync() => ApplyAsync();

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ApplyAsync()
    {
        var request = _requests.Begin();
        BorrowingStatus? status = StatusFilter switch
        {
            BorrowingStatusFilter.Active => BorrowingStatus.Active,
            BorrowingStatusFilter.Returned => BorrowingStatus.Returned,
            _ => null
        };
        var criteria = new BorrowingSearchRequest(SearchText, status, SortBy, Descending);
        Rows.Clear(); IsBusy = true; Message = "Loading borrowings...";
        try
        {
            var result = await service.SearchAsync(criteria, request.Token);
            if (!request.IsCurrent) return;
            Message = result.Message;
            if (!result.Success || result.Value is null) return;
            foreach (var row in result.Value) Rows.Add(row);
            if (Rows.Count > 0) Message = $"{Rows.Count} matching borrowing record(s).";
        }
        catch (OperationCanceledException) when (request.Token.IsCancellationRequested) { }
        catch (Exception) { if (request.IsCurrent) Message = "Unable to load borrowings. Restore the connection and apply again."; }
        finally { if (request.IsCurrent) IsBusy = false; }
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        SearchText = ""; StatusFilter = BorrowingStatusFilter.All;
        SortBy = BorrowingSortField.CheckoutDate; Descending = false;
        await ApplyAsync();
    }
    public void Deactivate() { _requests.Dispose(); IsBusy = false; }
}
