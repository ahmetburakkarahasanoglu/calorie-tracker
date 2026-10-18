using KaloriTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaloriTakip.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KaloriTakipDbContext))]
[Migration("20261013153000_UsePlainUsernames")]
public sealed class UsePlainUsernames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_users_username_hash",
            table: "users");

        migrationBuilder.DropColumn(
            name: "username_hash",
            table: "users");

        migrationBuilder.AddColumn<string>(
            name: "username",
            table: "users",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_users_username",
            table: "users",
            column: "username",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_users_username",
            table: "users");

        migrationBuilder.DropColumn(
            name: "username",
            table: "users");

        migrationBuilder.AddColumn<string>(
            name: "username_hash",
            table: "users",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_users_username_hash",
            table: "users",
            column: "username_hash",
            unique: true);
    }
}
