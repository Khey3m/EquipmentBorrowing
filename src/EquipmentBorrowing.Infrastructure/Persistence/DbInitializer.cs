using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(EquipmentBorrowingDbContext context)
    {
        await context.Database.MigrateAsync();

        if (!await context.Students.AnyAsync())
        {
            context.Students.AddRange(
                new Student(1, "Alice Guo", isEligibleToBorrow: true, maxAllowedBorrowings: 2),
                new Student(2, "Bob Smith", isEligibleToBorrow: false, maxAllowedBorrowings: 2),
                new Student(3, "Charlie Brown", isEligibleToBorrow: true, maxAllowedBorrowings: 3)
            );
        }

        if (!await context.Equipment.AnyAsync())
        {
            context.Equipment.AddRange(
                new Equipment(101, "Oscilloscope Rigol DS1054Z", isAvailable: true),
                new Equipment(102, "Digital Multimeter Fluke 117", isAvailable: true),
                new Equipment(103, "Soldering Station Weller WE1010", isAvailable: true)
            );
        }

        await context.SaveChangesAsync();
    }
}