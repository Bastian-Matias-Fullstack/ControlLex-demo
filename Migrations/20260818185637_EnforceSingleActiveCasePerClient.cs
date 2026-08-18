using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveCasePerClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Casos_ClienteId",
                table: "Casos");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Casos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "UX_Casos_ClienteId_Activo",
                table: "Casos",
                column: "ClienteId",
                unique: true,
                filter: "[Estado] <> N'Cerrado'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Casos_ClienteId_Activo",
                table: "Casos");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Casos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_Casos_ClienteId",
                table: "Casos",
                column: "ClienteId");
        }
    }
}
