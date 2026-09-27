using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Persistence.Configurations;

public class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
	public void Configure(EntityTypeBuilder<Borrowing> builder)
	{
		builder.HasKey(b => b.Id);

		builder.Property(b => b.StudentId)
			.IsRequired();

		builder.Property(b => b.EquipmentId)
			.IsRequired();

		builder.Property(b => b.BorrowedAt)
			.IsRequired();

		builder.Property(b => b.ExpectedReturnAt)
			.IsRequired();

		builder.Property(b => b.Status)
			.HasConversion<string>()
			.IsRequired();

		// Foreign Key Relationships
		builder.HasOne<Student>()
			.WithMany()
			.HasForeignKey(b => b.StudentId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne<Equipment>()
			.WithMany()
			.HasForeignKey(b => b.EquipmentId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}