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

    public override async void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();

        // Infrastructure owns connection configuration, contexts and migrations.
        serviceCollection.AddDbContextFactory<EquipmentBorrowingDbContext>(PostgresConfiguration.Configure);
        serviceCollection.AddTransient<PostgresBootstrapper>();
        serviceCollection.AddSingleton<ICampusClock, CampusClock>();
        serviceCollection.AddSingleton<IBorrowingSessionFactory, PostgresBorrowingSessionFactory>();
        serviceCollection.AddTransient<IStudentRepository, EfStudentRepository>();
        serviceCollection.AddTransient<IEquipmentRepository, EfEquipmentRepository>();
        serviceCollection.AddTransient<IBorrowingRepository, EfBorrowingRepository>();

        // 3. Register Application Services
        serviceCollection.AddTransient<BorrowEquipmentService>();
        serviceCollection.AddTransient<ReturnEquipmentService>();

        // 4. Register ViewModels
        serviceCollection.AddTransient<EquipmentViewModel>();
        serviceCollection.AddTransient<BorrowingsViewModel>();
        serviceCollection.AddTransient<MainWindowViewModel>();

        Services = serviceCollection.BuildServiceProvider();

        // Startup delegates migration/seeding to Infrastructure; no UI database logic.
        await Services.GetRequiredService<PostgresBootstrapper>().InitializeAsync();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}