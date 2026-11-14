using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace EquipmentBorrowing.Infrastructure.Persistence.Migrations;

internal static class InitialPostgresModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12").HasAnnotation("Relational:MaxIdentifierLength", 63);
        modelBuilder.UseIdentityByDefaultColumns();
        modelBuilder.Entity("EquipmentBorrowing.Domain.Student", b =>
        {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<string>("StudentNumber").HasColumnType("text").IsRequired();
            b.Property<string>("NormalizedStudentNumber").HasColumnType("text").IsRequired().ValueGeneratedOnAddOrUpdate().HasComputedColumnSql("upper(btrim(\"StudentNumber\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288)))", true);
            b.Property<string>("FullName").HasColumnType("text").IsRequired();
            b.Property<string>("ContactInformation").HasColumnType("text");
            b.Property<bool>("IsEligibleToBorrow").HasColumnType("boolean");
            b.HasKey("Id");
            b.HasIndex("NormalizedStudentNumber").IsUnique();
            b.ToTable("Students", t => {
                t.HasCheckConstraint("CK_Students_Number", "length(btrim(\"StudentNumber\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0 AND \"StudentNumber\" = btrim(\"StudentNumber\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))");
                t.HasCheckConstraint("CK_Students_Name", "length(btrim(\"FullName\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0");
                t.HasCheckConstraint("CK_Students_Contact", "\"ContactInformation\" IS NULL OR length(btrim(\"ContactInformation\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0");
            });
        });
        modelBuilder.Entity("EquipmentBorrowing.Domain.Equipment", b =>
        {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<string>("EquipmentIdentifier").HasColumnType("text").IsRequired();
            b.Property<string>("NormalizedEquipmentIdentifier").HasColumnType("text").IsRequired().ValueGeneratedOnAddOrUpdate().HasComputedColumnSql("upper(btrim(\"EquipmentIdentifier\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288)))", true);
            b.Property<string>("Name").HasColumnType("text").IsRequired();
            b.Property<string>("Description").HasColumnType("text");
            b.Property<bool>("IsAvailable").HasColumnType("boolean");
            b.HasKey("Id");
            b.HasIndex("NormalizedEquipmentIdentifier").IsUnique();
            b.ToTable("Equipment", t => {
                t.HasCheckConstraint("CK_Equipment_Identifier", "length(btrim(\"EquipmentIdentifier\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0 AND \"EquipmentIdentifier\" = btrim(\"EquipmentIdentifier\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))");
                t.HasCheckConstraint("CK_Equipment_Name", "length(btrim(\"Name\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0");
                t.HasCheckConstraint("CK_Equipment_Description", "\"Description\" IS NULL OR length(btrim(\"Description\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0");
            });
        });
        modelBuilder.Entity("EquipmentBorrowing.Domain.Borrowing", b =>
        {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("StudentId").HasColumnType("integer");
            b.Property<int>("EquipmentId").HasColumnType("integer");
            b.Property<DateOnly>("BorrowedAt").HasColumnType("date");
            b.Property<DateOnly>("ExpectedReturnAt").HasColumnType("date");
            b.Property<DateOnly?>("ActualReturnAt").HasColumnType("date");
            b.Property<string>("Status").HasColumnType("text").IsRequired();
            b.HasKey("Id");
            b.HasIndex("StudentId");
            b.HasIndex("EquipmentId").IsUnique().HasFilter("\"Status\" = 'Active'");
            b.ToTable("Borrowings", t => {
                t.HasCheckConstraint("CK_Borrowings_Due", "\"ExpectedReturnAt\" > \"BorrowedAt\"");
                t.HasCheckConstraint("CK_Borrowings_State", "(\"Status\" = 'Active' AND \"ActualReturnAt\" IS NULL) OR (\"Status\" = 'Returned' AND \"ActualReturnAt\" >= \"BorrowedAt\" AND \"ActualReturnAt\" IS NOT NULL)");
            });
        });
        modelBuilder.Entity("EquipmentBorrowing.Domain.Borrowing", b =>
        {
            b.HasOne("EquipmentBorrowing.Domain.Student", null).WithMany().HasForeignKey("StudentId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne("EquipmentBorrowing.Domain.Equipment", null).WithMany().HasForeignKey("EquipmentId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
    }
}
