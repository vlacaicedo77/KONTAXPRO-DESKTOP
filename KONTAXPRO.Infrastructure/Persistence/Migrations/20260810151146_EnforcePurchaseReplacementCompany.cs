using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePurchaseReplacementCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_compras_compra_sustituida_id",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.CreateIndex(
                name: "IX_compras_compra_sustituida_id_empresa_id",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "compra_sustituida_id", "empresa_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_compras_compra_sustituida_id_empresa_id",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "compra_sustituida_id", "empresa_id" },
                principalSchema: "s_compras",
                principalTable: "compras",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_compras_compra_sustituida_id_empresa_id",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.DropIndex(
                name: "IX_compras_compra_sustituida_id_empresa_id",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.AddForeignKey(
                name: "FK_compras_compras_compra_sustituida_id",
                schema: "s_compras",
                table: "compras",
                column: "compra_sustituida_id",
                principalSchema: "s_compras",
                principalTable: "compras",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
