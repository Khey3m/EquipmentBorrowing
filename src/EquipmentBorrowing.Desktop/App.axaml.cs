using System;
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
        serviceCollection.AddTransient<IApplicationInitializer, PostgresBootstrapper>();
        serviceCollection.AddTransient<ApplicationStartupService>();
        serviceCollection.AddSingleton<ICampusClock, CampusClock>();
        serviceCollection.AddSingleton<IWorkflowFailures, PostgresWorkflowFailures>();
        serviceCollection.AddSingleton<PostgresBorrowingSessionFactory>();
        serviceCollection.AddSingleton<IBorrowingSessionFactory>(sp => sp.GetRequiredService<PostgresBorrowingSessionFactory>());
        serviceCollection.AddSingleton<IStudentManagementSessionFactory>(sp => sp.GetRequiredService<PostgresBorrowingSessionFactory>());
        serviceCollection.AddTransient<IStudentRepository, EfStudentRepository>();
        serviceCollection.AddTransient<IEquipmentRepository, EfEquipmentRepository>();
        serviceCollection.AddTransient<IBorrowingRepository, EfBorrowingRepository>();

        // 3. Register Application Services
        serviceCollection.AddTransient<IExistingWorkflowQueries, EfExistingWorkflowQueries>();
        serviceCollection.AddTransient<ExistingWorkflowQueryService>();
        serviceCollection.AddTransient<BorrowEquipmentService>();
        serviceCollection.AddTransient<ReturnEquipmentService>();
        serviceCollection.AddTransient<StudentManagementService>();

        // 4. Register ViewModels
        serviceCollection.AddTransient<EquipmentViewModel>();
        serviceCollection.AddTransient<BorrowingsViewModel>();
        serviceCollection.AddTransient<StudentsViewModel>();
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