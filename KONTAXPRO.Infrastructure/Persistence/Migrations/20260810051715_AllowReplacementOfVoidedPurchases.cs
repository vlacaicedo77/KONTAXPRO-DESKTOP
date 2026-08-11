using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowReplacementOfVoidedPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_compras_empresa_proveedor_tipo_numero",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.CreateIndex(
                name: "ux_compras_empresa_proveedor_tipo_numero",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "empresa_id", "empresa_tercero_id", "tipo_comprobante_id", "numero_documento" },
                unique: true,
                filter: "numero_documento IS NOT NULL AND estado <> 'ANULADA'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_compras_empresa_proveedor_tipo_numero",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.CreateIndex(
                name: "ux_compras_empresa_proveedor_tipo_numero",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "empresa_id", "empresa_tercero_id", "tipo_comprobante_id", "numero_documento" },
                unique: true,
                filter: "numero_documento IS NOT NULL");
        }
    }
}
