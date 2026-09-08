using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Desktop;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();

        // 1. Repositories (Singletons preserve state across views)
        serviceCollection.AddSingleton<IStudentRepository, InMemoryStudentRepository>();
        serviceCollection.AddSingleton<IEquipmentRepository, InMemoryEquipmentRepository>();
        serviceCollection.AddSingleton<IBorrowingRepository, InMemoryBorrowingRepository>();

        // 2. Application Services
        serviceCollection.AddTransient<BorrowEquipmentService>();
        serviceCollection.AddTransient<ReturnEquipmentService>();

        // 3. ViewModels
        serviceCollection.AddTransient<EquipmentViewModel>();
        serviceCollection.AddTransient<BorrowingsViewModel>();
        serviceCollection.AddTransient<MainWindowViewModel>();

        Services = serviceCollection.BuildServiceProvider();

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