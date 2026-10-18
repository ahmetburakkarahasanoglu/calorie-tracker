using KaloriTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KaloriTakip.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KaloriTakipDbContext))]
[Migration("20261011183000_CreateCalorieLimitEntries")]
public sealed class CreateCalorieLimitEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "calorie_limit_entries",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                weight_kg = table.Column<double>(type: "double precision", nullable: false),
                height_cm = table.Column<double>(type: "double precision", nullable: false),
                age = table.Column<int>(type: "integer", nullable: false),
                gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                activity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                weekly_loss_kg = table.Column<double>(type: "double precision", nullable: false),
                daily_calorie_limit = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_calorie_limit_entries", item => item.id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "calorie_limit_entries");
    }
}
