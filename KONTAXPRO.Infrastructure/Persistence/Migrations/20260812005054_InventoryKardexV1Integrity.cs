using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InventoryKardexV1Integrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_movimientos_inventario_empresa_bodega_fecha",
                schema: "s_inventario",
                table: "movimientos_inventario",
                columns: new[] { "empresa_id", "bodega_id", "fecha_movimiento" });

            migrationBuilder.CreateIndex(
                name: "ux_movimientos_inventario_origen_bodega_tipo",
                schema: "s_inventario",
                table: "movimientos_inventario",
                columns: new[] { "empresa_id", "origen_tipo_id", "origen_id", "bodega_id", "tipo_movimiento_id" },
                unique: true,
                filter: "origen_id > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_movimientos_inventario_empresa_bodega_fecha",
                schema: "s_inventario",
                table: "movimientos_inventario");

            migrationBuilder.DropIndex(
                name: "ux_movimientos_inventario_origen_bodega_tipo",
                schema: "s_inventario",
                table: "movimientos_inventario");
        }
    }
}
