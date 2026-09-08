using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using System.Threading.Tasks;
using System;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ObservableObject
{
	private readonly BorrowEquipmentService _borrowService;
	private readonly IEquipmentRepository _equipmentRepo;
	private readonly IStudentRepository _studentRepo;

	[ObservableProperty] private ObservableCollection<Equipment> _equipmentList = new();
	[ObservableProperty] private ObservableCollection<Student> _studentList = new();
	[ObservableProperty] private Equipment? _selectedEquipment;
	[ObservableProperty] private Student? _selectedStudent;
	[ObservableProperty] private int _borrowDays = 3;
	[ObservableProperty] private string _statusMessage = string.Empty;
	[ObservableProperty] private bool _isSuccess;

	public EquipmentViewModel(
		BorrowEquipmentService borrowService,
		IEquipmentRepository equipmentRepo,
		IStudentRepository studentRepo)
	{
		_borrowService = borrowService;
		_equipmentRepo = equipmentRepo;
		_studentRepo = studentRepo;
	}

	public async Task InitializeAsync()
	{
		var items = await _equipmentRepo.GetAllAsync();
		EquipmentList = new ObservableCollection<Equipment>(items);

		var students = await _studentRepo.GetAllAsync();
		StudentList = new ObservableCollection<Student>(students);
	}

	[RelayCommand]
	private async Task BorrowAsync()
	{
		// 1. Presentation Validation
		if (SelectedStudent == null)
		{
			StatusMessage = "Please select a student.";
			IsSuccess = false;
			return;
		}

		if (SelectedEquipment == null)
		{
			StatusMessage = "Please select an equipment item.";
			IsSuccess = false;
			return;
		}

		if (BorrowDays <= 0)
		{
			StatusMessage = "Borrow duration must be at least 1 day.";
			IsSuccess = false;
			return;
		}

		// 2. Application Service Invocation
		var request = new BorrowRequest(SelectedStudent.Id, SelectedEquipment.Id, TimeSpan.FromDays(BorrowDays));
		var result = await _borrowService.ExecuteAsync(request);

		StatusMessage = result.Message;
		IsSuccess = result.Success;

		if (result.Success)
		{
			await InitializeAsync(); // Refresh state
		}
	}
}