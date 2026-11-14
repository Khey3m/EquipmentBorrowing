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
                new Student(1, "STU-001", "Alice Guo"),
                new Student(2, "STU-002", "Bob Smith", isEligibleToBorrow: false),
                new Student(3, "STU-003", "Charlie Brown")
            );
        }

        if (!await context.Equipment.AnyAsync())
        {
            context.Equipment.AddRange(
                new Equipment(101, "EQ-101", "Oscilloscope Rigol DS1054Z"),
                new Equipment(102, "EQ-102", "Digital Multimeter Fluke 117"),
                new Equipment(103, "EQ-103", "Soldering Station Weller WE1010")
            );
        }

        await context.SaveChangesAsync();
    }
}