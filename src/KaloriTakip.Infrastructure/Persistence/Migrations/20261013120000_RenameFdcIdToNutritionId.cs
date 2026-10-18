using KaloriTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaloriTakip.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KaloriTakipDbContext))]
[Migration("20261013120000_RenameFdcIdToNutritionId")]
public sealed class RenameFdcIdToNutritionId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.RenameColumn(
            name: "fdc_id",
            table: "food_log_entries",
            newName: "nutrition_id");

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.RenameColumn(
            name: "nutrition_id",
            table: "food_log_entries",
            newName: "fdc_id");
}
