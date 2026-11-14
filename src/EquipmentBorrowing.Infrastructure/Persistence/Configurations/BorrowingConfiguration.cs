using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Persistence.Configurations;

public class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
    public void Configure(EntityTypeBuilder<Borrowing> builder)
    {
        builder.ToTable("Borrowings", t => {
            t.HasCheckConstraint("CK_Borrowings_Due", "\"ExpectedReturnAt\" > \"BorrowedAt\"");
            t.HasCheckConstraint("CK_Borrowings_State", "(\"Status\" = 'Active' AND \"ActualReturnAt\" IS NULL) OR (\"Status\" = 'Returned' AND \"ActualReturnAt\" >= \"BorrowedAt\" AND \"ActualReturnAt\" IS NOT NULL)");
        });
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).UseIdentityByDefaultColumn();
        builder.Property(b => b.BorrowedAt).HasColumnType("date");
        builder.Property(b => b.ExpectedReturnAt).HasColumnType("date");
        builder.Property(b => b.ActualReturnAt).HasColumnType("date");
        builder.Property(b => b.Status).HasConversion<string>().IsRequired();
        builder.HasIndex(b => b.EquipmentId).IsUnique().HasFilter("\"Status\" = 'Active'");
        builder.HasOne<Student>().WithMany().HasForeignKey(b => b.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Equipment>().WithMany().HasForeignKey(b => b.EquipmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
