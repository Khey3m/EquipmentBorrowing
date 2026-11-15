using System.Net.Http;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Desktop;

public static class DesktopComposition
{
    public static ServiceProvider Build(HttpClient? client = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => client ?? ApiTransport.CreateClient());
        services.AddSingleton<ApiTransport>();
        services.AddTransient<IStudentManagementWorkflow, HttpStudentManagement>();
        services.AddTransient<IEquipmentManagementWorkflow, HttpEquipmentManagement>();
        services.AddTransient<ICatalogReader, HttpCatalogReader>();
        services.AddTransient<ISingleCheckoutWorkflow, HttpSingleCheckout>();
        services.AddTransient<IMultiCheckoutWorkflow, HttpMultiCheckout>();
        services.AddTransient<IReturnWorkflow, HttpReturnWorkflow>();
        services.AddTransient<IActiveBorrowingReader, HttpActiveBorrowingReader>();
        services.AddTransient<IHistoryWorkflow, HttpHistoryWorkflow>();
        services.AddTransient<IBorrowingSearchWorkflow, HttpBorrowingSearch>();
        services.AddSingleton<ICampusClock, CampusClock>();
        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<BorrowingSearchViewModel>();
        services.AddTransient<BorrowingHistoryViewModel>();
        services.AddTransient<MultiItemCheckoutViewModel>();
        services.AddTransient<BorrowingsViewModel>();
        services.AddTransient<StudentsViewModel>();
        services.AddTransient<EquipmentManagementViewModel>();
        services.AddTransient<MainWindowViewModel>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
