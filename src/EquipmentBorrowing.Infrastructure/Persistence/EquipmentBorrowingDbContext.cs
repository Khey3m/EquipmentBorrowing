using System;
using System.IO;
using System.Reflection;
using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public class EquipmentBorrowingDbContext : DbContext
{
	public DbSet<Student> Students => Set<Student>();
	public DbSet<Equipment> Equipment => Set<Equipment>();
	public DbSet<Borrowing> Borrowings => Set<Borrowing>();

	// Parameterless constructor for EF Core design-time tooling
	public EquipmentBorrowingDbContext()
	{
	}

	// Constructor used by Dependency Injection
	public EquipmentBorrowingDbContext(DbContextOptions<EquipmentBorrowingDbContext> options)
		: base(options)
	{
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
		{
			string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "equipment_borrowing.db");
			optionsBuilder.UseSqlite($"Data Source={dbPath}");
		}
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
	}
}