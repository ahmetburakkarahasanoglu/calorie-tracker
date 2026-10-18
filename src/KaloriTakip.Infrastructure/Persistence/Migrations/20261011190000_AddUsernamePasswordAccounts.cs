using KaloriTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaloriTakip.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KaloriTakipDbContext))]
[Migration("20261011190000_AddUsernamePasswordAccounts")]
public sealed class AddUsernamePasswordAccounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "email",
            table: "users",
            type: "character varying(254)",
            maxLength: 254,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(254)",
            oldMaxLength: 254);

        migrationBuilder.AlterColumn<string>(
            name: "normalized_email",
            table: "users",
            type: "character varying(254)",
            maxLength: 254,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(254)",
            oldMaxLength: 254);

        migrationBuilder.AddColumn<string>(
            name: "username_hash",
            table: "users",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "password_hash",
            table: "users",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_users_username_hash",
            table: "users",
            column: "username_hash",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_users_username_hash", table: "users");
        migrationBuilder.DropColumn(name: "username_hash", table: "users");
        migrationBuilder.DropColumn(name: "password_hash", table: "users");

        migrationBuilder.AlterColumn<string>(
            name: "email",
            table: "users",
            type: "character varying(254)",
            maxLength: 254,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "character varying(254)",
            oldMaxLength: 254,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "normalized_email",
            table: "users",
            type: "character varying(254)",
            maxLength: 254,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "character varying(254)",
            oldMaxLength: 254,
            oldNullable: true);
    }
}
