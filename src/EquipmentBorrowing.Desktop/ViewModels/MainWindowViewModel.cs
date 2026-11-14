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

    public MainWindowViewModel(EquipmentViewModel equipmentVm, BorrowingsViewModel borrowingsVm, ApplicationStartupService startup, StudentsViewModel studentsVm)
    {
        _startup = startup;
        StudentsVm = studentsVm;
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
}
