using KaloriTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KaloriTakip.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KaloriTakipDbContext))]
[Migration("20261011184000_AddUsersAndHistory")]
public sealed class AddUsersAndHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                normalized_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_users", item => item.id));

        migrationBuilder.CreateIndex(
            name: "IX_users_normalized_email",
            table: "users",
            column: "normalized_email",
            unique: true);

        migrationBuilder.AddColumn<long>(
            name: "user_id",
            table: "calorie_limit_entries",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_calorie_limit_entries_user_id",
            table: "calorie_limit_entries",
            column: "user_id");

        migrationBuilder.AddForeignKey(
            name: "FK_calorie_limit_entries_users_user_id",
            table: "calorie_limit_entries",
            column: "user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_calorie_limit_entries_users_user_id",
            table: "calorie_limit_entries");
        migrationBuilder.DropIndex(
            name: "IX_calorie_limit_entries_user_id",
            table: "calorie_limit_entries");
        migrationBuilder.DropColumn(name: "user_id", table: "calorie_limit_entries");
        migrationBuilder.DropTable(name: "users");
    }
}
