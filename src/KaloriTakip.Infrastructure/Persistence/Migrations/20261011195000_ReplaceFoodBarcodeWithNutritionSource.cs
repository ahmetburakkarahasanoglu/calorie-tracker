using KaloriTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaloriTakip.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KaloriTakipDbContext))]
[Migration("20261011195000_ReplaceFoodBarcodeWithNutritionSource")]
public sealed class ReplaceFoodBarcodeWithNutritionSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "barcode", table: "food_log_entries");

        migrationBuilder.AddColumn<long>(
            name: "fdc_id",
            table: "food_log_entries",
            type: "bigint",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "fdc_id", table: "food_log_entries");

        migrationBuilder.AddColumn<string>(
            name: "barcode",
            table: "food_log_entries",
            type: "character varying(14)",
            maxLength: 14,
            nullable: false,
            defaultValue: "");
    }
}
