using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ApplicationStartupService _startup;
    [ObservableProperty] private string _startupMessage = "";
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RetryStartupCommand))] private bool _isStarting;
    private bool CanStart() => !IsStarting;

    [RelayCommand(CanExecute = nameof(CanStart))]
    public async Task RetryStartupAsync()
    {
        if (IsStarting) return;
        IsStarting = true;
        StartupMessage = "Connecting and preparing the database...";
        try
        {
            if (!await _startup.InitializeAsync())
            {
                StartupMessage = "Database initialization failed. Check local configuration and connection, then retry startup.";
                return;
            }
            StartupMessage = "";
            await EquipmentVm.InitializeAsync();
        }
        finally { IsStarting = false; }
    }

    [ObservableProperty] private ObservableObject _currentView;

    public EquipmentViewModel EquipmentVm { get; }
    public BorrowingsViewModel BorrowingsVm { get; }
    public StudentsViewModel StudentsVm { get; }
    public BorrowingHistoryViewModel HistoryVm { get; }
    public BorrowingSearchViewModel SearchVm { get; }
    public MultiItemCheckoutViewModel MultiCheckoutVm { get; }
    public EquipmentManagementViewModel EquipmentManagementVm { get; }

    public MainWindowViewModel(EquipmentViewModel equipmentVm, BorrowingsViewModel borrowingsVm, ApplicationStartupService startup, StudentsViewModel studentsVm, EquipmentManagementViewModel equipmentManagementVm, MultiItemCheckoutViewModel multiCheckoutVm, BorrowingHistoryViewModel historyVm, BorrowingSearchViewModel searchVm)
    {
        _startup = startup;
        HistoryVm = historyVm;
        SearchVm = searchVm;
        StudentsVm = studentsVm;
        EquipmentManagementVm = equipmentManagementVm;
        MultiCheckoutVm = multiCheckoutVm;
        EquipmentVm = equipmentVm;
        BorrowingsVm = borrowingsVm;
        _currentView = EquipmentVm;
    }

    [RelayCommand]
    private async Task NavigateToEquipmentAsync()
    {
        await EquipmentVm.InitializeAsync();
        CurrentView = EquipmentVm;
    }

    [RelayCommand]
    private async Task NavigateToBorrowingsAsync()
    {
        await BorrowingsVm.InitializeAsync();
        CurrentView = BorrowingsVm;
    }
    [RelayCommand]
    private async Task NavigateToStudentsAsync()
    {
        CurrentView = StudentsVm;
        await StudentsVm.InitializeAsync();
    }
    [RelayCommand]
    private async Task NavigateToEquipmentManagementAsync()
    {
        CurrentView = EquipmentManagementVm;
        await EquipmentManagementVm.InitializeAsync();
    }
    [RelayCommand]
    private async Task NavigateToMultiCheckoutAsync()
    {
        CurrentView = MultiCheckoutVm;
        await MultiCheckoutVm.InitializeAsync();
    }
    partial void OnCurrentViewChanged(ObservableObject? oldValue, ObservableObject newValue)
    {
        if (ReferenceEquals(oldValue, SearchVm) && !ReferenceEquals(newValue, SearchVm)) SearchVm.Deactivate();
        if (ReferenceEquals(oldValue, HistoryVm) && !ReferenceEquals(newValue, HistoryVm)) HistoryVm.Deactivate();
    }

    [RelayCommand]
    private async Task NavigateToHistoryAsync()
    {
        CurrentView = HistoryVm;
        await HistoryVm.InitializeAsync();
    }
    [RelayCommand]
    private async Task NavigateToSearchAsync()
    {
        CurrentView = SearchVm;
        await SearchVm.InitializeAsync();
    }
}
