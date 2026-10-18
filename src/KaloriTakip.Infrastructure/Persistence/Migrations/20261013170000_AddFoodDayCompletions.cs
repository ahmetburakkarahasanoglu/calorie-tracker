using KaloriTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaloriTakip.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KaloriTakipDbContext))]
[Migration("20261013170000_AddFoodDayCompletions")]
public sealed class AddFoodDayCompletions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "food_day_completions",
            columns: table => new
            {
                user_id = table.Column<long>(type: "bigint", nullable: false),
                date = table.Column<DateOnly>(type: "date", nullable: false),
                daily_calorie_limit = table.Column<int>(type: "integer", nullable: true),
                met_target = table.Column<bool>(type: "boolean", nullable: true),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_food_day_completions", item => new { item.user_id, item.date });
                table.ForeignKey(
                    name: "FK_food_day_completions_users_user_id",
                    column: item => item.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "food_day_completions");
}
