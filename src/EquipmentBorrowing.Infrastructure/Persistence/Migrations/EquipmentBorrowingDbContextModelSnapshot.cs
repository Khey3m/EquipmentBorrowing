using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EquipmentBorrowing.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EquipmentBorrowingDbContext))]
public class EquipmentBorrowingDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => InitialPostgresModel.Build(modelBuilder);
}
