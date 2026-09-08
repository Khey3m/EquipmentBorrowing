using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using System.Threading.Tasks;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ObservableObject
{
	private readonly ReturnEquipmentService _returnService;
	private readonly IBorrowingRepository _borrowingRepo;

	[ObservableProperty] private ObservableCollection<Borrowing> _activeBorrowings = new();
	[ObservableProperty] private Borrowing? _selectedBorrowing;
	[ObservableProperty] private string _statusMessage = string.Empty;
	[ObservableProperty] private bool _isSuccess;

	public BorrowingsViewModel(
		ReturnEquipmentService returnService,
		IBorrowingRepository borrowingRepo)
	{
		_returnService = returnService;
		_borrowingRepo = borrowingRepo;
	}

	public async Task InitializeAsync()
	{
		var borrowings = await _borrowingRepo.GetActiveBorrowingsAsync();
		ActiveBorrowings = new ObservableCollection<Borrowing>(borrowings);
	}

	[RelayCommand]
	private async Task ReturnAsync()
	{
		if (SelectedBorrowing == null)
		{
			StatusMessage = "Please select an active borrowing record to return.";
			IsSuccess = false;
			return;
		}

		var result = await _returnService.ExecuteAsync(SelectedBorrowing.Id);
		StatusMessage = result.Message;
		IsSuccess = result.Success;

		if (result.Success)
		{
			await InitializeAsync(); // Refresh active list
		}
	}
}