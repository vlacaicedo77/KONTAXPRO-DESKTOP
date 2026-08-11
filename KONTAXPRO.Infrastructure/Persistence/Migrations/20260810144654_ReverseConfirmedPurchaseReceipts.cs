using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReverseConfirmedPurchaseReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "movimiento_inventario_detalle_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ultimo_costo_efectivo_anterior",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ultimo_precio_compra_anterior",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "anulada_at",
                schema: "s_compras",
                table: "compras_recepciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "anulada_por_usuario_id",
                schema: "s_compras",
                table: "compras_recepciones",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_anulacion",
                schema: "s_compras",
                table: "compras_recepciones",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_compras_recepciones_detalles_movimiento",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                column: "movimiento_inventario_detalle_id",
                unique: true,
                filter: "movimiento_inventario_detalle_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_anulada_por_usuario_id",
                schema: "s_compras",
                table: "compras_recepciones",
                column: "anulada_por_usuario_id");

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_usuarios_anulada_por_usuario_id",
                schema: "s_compras",
                table: "compras_recepciones",
                column: "anulada_por_usuario_id",
                principalSchema: "s_seguridad",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_movimientos_inventario_detalle~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                column: "movimiento_inventario_detalle_id",
                principalSchema: "s_inventario",
                principalTable: "movimientos_inventario_detalles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_usuarios_anulada_por_usuario_id",
                schema: "s_compras",
                table: "compras_recepciones");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_movimientos_inventario_detalle~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "ux_compras_recepciones_detalles_movimiento",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_anulada_por_usuario_id",
                schema: "s_compras",
                table: "compras_recepciones");

            migrationBuilder.DropColumn(
                name: "movimiento_inventario_detalle_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropColumn(
                name: "ultimo_costo_efectivo_anterior",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropColumn(
                name: "ultimo_precio_compra_anterior",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropColumn(
                name: "anulada_at",
                schema: "s_compras",
                table: "compras_recepciones");

            migrationBuilder.DropColumn(
                name: "anulada_por_usuario_id",
                schema: "s_compras",
                table: "compras_recepciones");

            migrationBuilder.DropColumn(
                name: "motivo_anulacion",
                schema: "s_compras",
                table: "compras_recepciones");
        }
    }
}
