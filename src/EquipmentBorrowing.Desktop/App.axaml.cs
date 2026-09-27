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

        // 1. Configure SQLite DbContext
        string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "equipment_borrowing.db");
        serviceCollection.AddDbContext<EquipmentBorrowingDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // 2. Register EF Repositories
        serviceCollection.AddScoped<IStudentRepository, EfStudentRepository>();
        serviceCollection.AddScoped<IEquipmentRepository, EfEquipmentRepository>();
        serviceCollection.AddScoped<IBorrowingRepository, EfBorrowingRepository>();

        // 3. Register Application Services
        serviceCollection.AddTransient<BorrowEquipmentService>();
        serviceCollection.AddTransient<ReturnEquipmentService>();

        // 4. Register ViewModels
        serviceCollection.AddTransient<EquipmentViewModel>();
        serviceCollection.AddTransient<BorrowingsViewModel>();
        serviceCollection.AddTransient<MainWindowViewModel>();

        Services = serviceCollection.BuildServiceProvider();

        // 5. Initialize and seed database asynchronously
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentBorrowingDbContext>();
            await DbInitializer.SeedAsync(dbContext);
        }

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