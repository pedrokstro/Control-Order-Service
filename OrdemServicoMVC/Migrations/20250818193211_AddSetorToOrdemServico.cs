using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdemServicoMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddSetorToOrdemServico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Setor",
                table: "OrdensServico",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Setor",
                table: "OrdensServico");
        }
    }
}
