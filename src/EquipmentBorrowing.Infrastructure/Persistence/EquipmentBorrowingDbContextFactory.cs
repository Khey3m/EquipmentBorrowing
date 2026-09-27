using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public class EquipmentBorrowingDbContextFactory : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>();

        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "equipment_borrowing.db");
        builder.UseSqlite($"Data Source={dbPath}");

        return new EquipmentBorrowingDbContext(builder.Options);
    }
}