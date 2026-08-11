using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseAccountingIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asientos_origen",
                schema: "s_contabilidad",
                table: "asientos");

            migrationBuilder.AddColumn<string>(
                name: "clasificacion_contable",
                schema: "s_compras",
                table: "compras_detalles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "cuenta_contable_id",
                schema: "s_compras",
                table: "compras_detalles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_cuenta_contable_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "cuenta_contable_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_detalles_clasificacion_contable",
                schema: "s_compras",
                table: "compras_detalles",
                sql: "clasificacion_contable IN ('INVENTARIO', 'GASTO', 'ACTIVO', 'OTRO')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_detalles_clasificacion_producto",
                schema: "s_compras",
                table: "compras_detalles",
                sql: "(es_inventariable AND clasificacion_contable = 'INVENTARIO') OR (NOT es_inventariable AND clasificacion_contable <> 'INVENTARIO')");

            migrationBuilder.CreateIndex(
                name: "ux_asientos_origen",
                schema: "s_contabilidad",
                table: "asientos",
                columns: new[] { "empresa_id", "tipo_origen_asiento_id", "origen_id" },
                unique: true,
                filter: "origen_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_compras_detalles_plan_cuentas_cuenta_contable_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "cuenta_contable_id",
                principalSchema: "s_contabilidad",
                principalTable: "plan_cuentas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_detalles_plan_cuentas_cuenta_contable_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_detalles_cuenta_contable_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_detalles_clasificacion_contable",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_detalles_clasificacion_producto",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "ux_asientos_origen",
                schema: "s_contabilidad",
                table: "asientos");

            migrationBuilder.DropColumn(
                name: "clasificacion_contable",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropColumn(
                name: "cuenta_contable_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.CreateIndex(
                name: "ix_asientos_origen",
                schema: "s_contabilidad",
                table: "asientos",
                columns: new[] { "empresa_id", "tipo_origen_asiento_id", "origen_id" },
                filter: "origen_id IS NOT NULL");
        }
    }
}
