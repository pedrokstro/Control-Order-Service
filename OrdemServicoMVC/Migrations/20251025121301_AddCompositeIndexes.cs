using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdemServicoMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrdensServico_TecnicoResponsavelId",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "IpOrigem",
                table: "OrdensServico");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_DataConclusao_Status",
                table: "OrdensServico",
                columns: new[] { "DataConclusao", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_DataCriacao_Status",
                table: "OrdensServico",
                columns: new[] { "DataCriacao", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_Prioridade_Status",
                table: "OrdensServico",
                columns: new[] { "Prioridade", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_TecnicoResponsavelId_Status",
                table: "OrdensServico",
                columns: new[] { "TecnicoResponsavelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_UsuarioCriadorId_DataCriacao",
                table: "OrdensServico",
                columns: new[] { "UsuarioCriadorId", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_OrdemServicoId_DataEnvio",
                table: "Mensagens",
                columns: new[] { "OrdemServicoId", "DataEnvio" });

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_OrdemServicoId_DataUpload",
                table: "Anexos",
                columns: new[] { "OrdemServicoId", "DataUpload" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrdensServico_DataConclusao_Status",
                table: "OrdensServico");

            migrationBuilder.DropIndex(
                name: "IX_OrdensServico_DataCriacao_Status",
                table: "OrdensServico");

            migrationBuilder.DropIndex(
                name: "IX_OrdensServico_Prioridade_Status",
                table: "OrdensServico");

            migrationBuilder.DropIndex(
                name: "IX_OrdensServico_TecnicoResponsavelId_Status",
                table: "OrdensServico");

            migrationBuilder.DropIndex(
                name: "IX_OrdensServico_UsuarioCriadorId_DataCriacao",
                table: "OrdensServico");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_OrdemServicoId_DataEnvio",
                table: "Mensagens");

            migrationBuilder.DropIndex(
                name: "IX_Anexos_OrdemServicoId_DataUpload",
                table: "Anexos");

            migrationBuilder.AddColumn<string>(
                name: "IpOrigem",
                table: "OrdensServico",
                type: "TEXT",
                maxLength: 45,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_TecnicoResponsavelId",
                table: "OrdensServico",
                column: "TecnicoResponsavelId");
        }
    }
}
