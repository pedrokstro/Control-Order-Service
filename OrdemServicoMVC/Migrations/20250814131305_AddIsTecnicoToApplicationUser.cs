using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdemServicoMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTecnicoToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTecnico",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTecnico",
                table: "AspNetUsers");
        }
    }
}
