using KaloriTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KaloriTakip.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KaloriTakipDbContext))]
[Migration("20261011194000_AddFoodLogEntries")]
public sealed class AddFoodLogEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "food_log_entries",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                user_id = table.Column<long>(type: "bigint", nullable: false),
                barcode = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                product_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                grams = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                calories_per_100g = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                calories = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_food_log_entries", item => item.id);
                table.ForeignKey(
                    name: "FK_food_log_entries_users_user_id",
                    column: item => item.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_food_log_entries_user_id_created_at",
            table: "food_log_entries",
            columns: new[] { "user_id", "created_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "food_log_entries");
}
