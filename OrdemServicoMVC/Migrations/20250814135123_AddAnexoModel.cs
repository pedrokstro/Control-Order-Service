using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdemServicoMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddAnexoModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anexos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeArquivo = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    TipoMime = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    DadosArquivo = table.Column<byte[]>(type: "BLOB", nullable: false),
                    DataUpload = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OrdemServicoId = table.Column<int>(type: "INTEGER", nullable: true),
                    MensagemId = table.Column<int>(type: "INTEGER", nullable: true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anexos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anexos_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anexos_Mensagens_MensagemId",
                        column: x => x.MensagemId,
                        principalTable: "Mensagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Anexos_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_DataUpload",
                table: "Anexos",
                column: "DataUpload");

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_MensagemId",
                table: "Anexos",
                column: "MensagemId");

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_OrdemServicoId",
                table: "Anexos",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_UsuarioId",
                table: "Anexos",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anexos");
        }
    }
}
