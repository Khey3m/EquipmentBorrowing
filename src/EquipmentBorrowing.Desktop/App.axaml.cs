using System;
using EquipmentBorrowing.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Desktop;

public partial class App : Avalonia.Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();

        // Infrastructure owns connection configuration, contexts and migrations.
        serviceCollection.AddDbContextFactory<EquipmentBorrowingDbContext>(PostgresConfiguration.Configure);
        // Transitional only: unmigrated workflows below still use PostgreSQL until Sessions 7-8.
        serviceCollection.AddSingleton(_ => ApiTransport.CreateClient());
        serviceCollection.AddSingleton<ApiTransport>();
        serviceCollection.AddTransient<IStudentManagementWorkflow, HttpStudentManagement>();
        serviceCollection.AddTransient<IEquipmentManagementWorkflow, HttpEquipmentManagement>();
        serviceCollection.AddTransient<ICatalogReader, HttpCatalogReader>();
        serviceCollection.AddTransient<ISingleCheckoutWorkflow, HttpSingleCheckout>();
        serviceCollection.AddTransient<IMultiCheckoutWorkflow, HttpMultiCheckout>();
        serviceCollection.AddTransient<IReturnWorkflow, HttpReturnWorkflow>();
        serviceCollection.AddTransient<IActiveBorrowingReader, HttpActiveBorrowingReader>();
        serviceCollection.AddSingleton<ICampusClock, CampusClock>();
        serviceCollection.AddTransient<IHistoryQueries, EfHistoryQueries>();
        serviceCollection.AddTransient<BorrowingHistoryService>();
        serviceCollection.AddTransient<IBorrowingSearchQueries, EfBorrowingSearchQueries>();
        serviceCollection.AddTransient<BorrowingSearchService>();

        // 4. Register ViewModels
        serviceCollection.AddTransient<EquipmentViewModel>();
        serviceCollection.AddTransient<BorrowingSearchViewModel>();
        serviceCollection.AddTransient<BorrowingHistoryViewModel>();
        serviceCollection.AddTransient<MultiItemCheckoutViewModel>();
        serviceCollection.AddTransient<BorrowingsViewModel>();
        serviceCollection.AddTransient<StudentsViewModel>();
        serviceCollection.AddTransient<EquipmentManagementViewModel>();
        serviceCollection.AddTransient<MainWindowViewModel>();

        Services = serviceCollection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm
            };
            desktop.MainWindow.Opened += async (_, _) => await mainVm.RetryStartupAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}