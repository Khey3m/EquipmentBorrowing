using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _startupMessage = "";
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RetryStartupCommand))] private bool _isStarting;
    public bool HasStartupMessage => !string.IsNullOrWhiteSpace(StartupMessage);
    partial void OnStartupMessageChanged(string value) => OnPropertyChanged(nameof(HasStartupMessage));
    public string PageLabel => CurrentView switch
    {
        EquipmentViewModel => "Equipment catalog",
        BorrowingsViewModel => "Active borrowings",
        StudentsViewModel => "Student management",
        EquipmentManagementViewModel => "Equipment management",
        MultiItemCheckoutViewModel => "Multiple checkout",
        BorrowingHistoryViewModel => "Borrowing histories",
        _ => "Search borrowings"
    };
    public bool IsEquipment => ReferenceEquals(CurrentView, EquipmentVm);
    public bool IsBorrowings => ReferenceEquals(CurrentView, BorrowingsVm);
    public bool IsMultiCheckout => ReferenceEquals(CurrentView, MultiCheckoutVm);
    public bool IsStudents => ReferenceEquals(CurrentView, StudentsVm);
    public bool IsEquipmentManagement => ReferenceEquals(CurrentView, EquipmentManagementVm);
    public bool IsHistory => ReferenceEquals(CurrentView, HistoryVm);
    public bool IsSearch => ReferenceEquals(CurrentView, SearchVm);
    private bool CanStart() => !IsStarting;

    [RelayCommand(CanExecute = nameof(CanStart))]
    public async Task RetryStartupAsync()
    {
        if (IsStarting) return;
        IsStarting = true;
        StartupMessage = "Connecting to the API...";
        try
        {
            await EquipmentVm.InitializeAsync();
            StartupMessage = EquipmentVm.IsReady ? "" : EquipmentVm.LoadMessage;
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

    public MainWindowViewModel(EquipmentViewModel equipmentVm, BorrowingsViewModel borrowingsVm, StudentsViewModel studentsVm, EquipmentManagementViewModel equipmentManagementVm, MultiItemCheckoutViewModel multiCheckoutVm, BorrowingHistoryViewModel historyVm, BorrowingSearchViewModel searchVm)
    {
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
        OnPropertyChanged(nameof(PageLabel));
        OnPropertyChanged(nameof(IsEquipment));
        OnPropertyChanged(nameof(IsBorrowings));
        OnPropertyChanged(nameof(IsMultiCheckout));
        OnPropertyChanged(nameof(IsStudents));
        OnPropertyChanged(nameof(IsEquipmentManagement));
        OnPropertyChanged(nameof(IsHistory));
        OnPropertyChanged(nameof(IsSearch));
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
