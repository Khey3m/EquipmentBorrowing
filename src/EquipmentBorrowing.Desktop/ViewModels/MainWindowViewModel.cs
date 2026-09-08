using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private ObservableObject _currentView;

    public EquipmentViewModel EquipmentVm { get; }
    public BorrowingsViewModel BorrowingsVm { get; }

    public MainWindowViewModel(EquipmentViewModel equipmentVm, BorrowingsViewModel borrowingsVm)
    {
        EquipmentVm = equipmentVm;
        BorrowingsVm = borrowingsVm;
        _currentView = EquipmentVm;

        _ = EquipmentVm.InitializeAsync();
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
}