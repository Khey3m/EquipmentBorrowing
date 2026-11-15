using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;

namespace EquipmentBorrowing.Api;

public static class BackendServices
{
    public static IServiceCollection AddBorrowingBackend(this IServiceCollection services)
    {
        // Contexts are created per operation; registration does not connect or seed on host startup.
        services.AddDbContextFactory<EquipmentBorrowingDbContext>(PostgresConfiguration.Configure);
        services.AddTransient<IApplicationInitializer, PostgresBootstrapper>();
        services.AddSingleton<ICampusClock, CampusClock>();
        services.AddSingleton<IWorkflowFailures, PostgresWorkflowFailures>();
        services.AddSingleton<PostgresBorrowingSessionFactory>();
        services.AddSingleton<IBorrowingSessionFactory>(sp => sp.GetRequiredService<PostgresBorrowingSessionFactory>());
        services.AddSingleton<IStudentManagementSessionFactory>(sp => sp.GetRequiredService<PostgresBorrowingSessionFactory>());
        services.AddSingleton<IEquipmentManagementSessionFactory>(sp => sp.GetRequiredService<PostgresBorrowingSessionFactory>());
        services.AddTransient<IStudentRepository, EfStudentRepository>();
        services.AddTransient<IEquipmentRepository, EfEquipmentRepository>();
        services.AddTransient<IBorrowingRepository, EfBorrowingRepository>();
        services.AddTransient<IExistingWorkflowQueries, EfExistingWorkflowQueries>();
        services.AddTransient<IHistoryQueries, EfHistoryQueries>();
        services.AddTransient<IBorrowingSearchQueries, EfBorrowingSearchQueries>();
        services.AddTransient<ExistingWorkflowQueryService>();
        services.AddTransient<BorrowingHistoryService>();
        services.AddTransient<BorrowingSearchService>();
        services.AddTransient<StudentManagementService>();
        services.AddTransient<EquipmentManagementService>();
        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<MultiItemCheckoutService>();
        services.AddTransient<ReturnEquipmentService>();
        return services;
    }
}
