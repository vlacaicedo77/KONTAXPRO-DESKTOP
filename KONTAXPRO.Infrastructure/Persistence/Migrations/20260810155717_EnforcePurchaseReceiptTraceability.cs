using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePurchaseReceiptTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_lotes_compras_recepciones_deta~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_lotes_productos_lotes_producto~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_series_compras_recepciones_det~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_series_productos_series_produc~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series");

            migrationBuilder.DropForeignKey(
                name: "FK_productos_series_productos_lotes_producto_lote_id",
                schema: "s_inventario",
                table: "productos_series");

            migrationBuilder.DropIndex(
                name: "IX_productos_series_producto_lote_id",
                schema: "s_inventario",
                table: "productos_series");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_series_producto_serie_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_lotes_producto_lote_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_compra_recepcion_id_compra_id_~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.AddColumn<long>(
                name: "bodega_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "producto_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "producto_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "bodega_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE s_compras.compras_recepciones_detalles AS d
                SET bodega_id = r.bodega_id
                FROM s_compras.compras_recepciones AS r
                WHERE r.id = d.compra_recepcion_id;

                UPDATE s_compras.compras_recepciones_detalles_lotes AS l
                SET producto_id = d.producto_id,
                    empresa_id = d.empresa_id
                FROM s_compras.compras_recepciones_detalles AS d
                WHERE d.id = l.compra_recepcion_detalle_id;

                UPDATE s_compras.compras_recepciones_detalles_series AS s
                SET producto_id = d.producto_id,
                    bodega_id = d.bodega_id,
                    empresa_id = d.empresa_id
                FROM s_compras.compras_recepciones_detalles AS d
                WHERE d.id = s.compra_recepcion_detalle_id;

                DO $traceability$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM s_inventario.productos_series s
                        JOIN s_inventario.productos_lotes l
                          ON l.id = s.producto_lote_id
                        WHERE s.producto_lote_id IS NOT NULL
                          AND l.producto_id <> s.producto_id
                    ) THEN
                        RAISE EXCEPTION 'Existen series asociadas a lotes de otro producto.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM s_compras.compras_recepciones_detalles_lotes rl
                        LEFT JOIN s_inventario.productos_lotes l
                          ON l.id = rl.producto_lote_id
                         AND l.producto_id = rl.producto_id
                        WHERE rl.producto_id IS NULL
                           OR rl.empresa_id IS NULL
                           OR l.id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Existen lotes de recepción que no pertenecen al producto recibido.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM s_compras.compras_recepciones_detalles_series rs
                        LEFT JOIN s_inventario.productos_series s
                          ON s.id = rs.producto_serie_id
                         AND s.producto_id = rs.producto_id
                         AND s.bodega_id = rs.bodega_id
                        WHERE rs.producto_id IS NULL
                           OR rs.bodega_id IS NULL
                           OR rs.empresa_id IS NULL
                           OR s.id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Existen series de recepción que no pertenecen al producto o bodega recibidos.';
                    END IF;
                END
                $traceability$;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "bodega_id", schema: "s_compras",
                table: "compras_recepciones_detalles", type: "bigint",
                nullable: false, oldClrType: typeof(long), oldType: "bigint",
                oldNullable: true);
            migrationBuilder.AlterColumn<long>(
                name: "producto_id", schema: "s_compras",
                table: "compras_recepciones_detalles_lotes", type: "bigint",
                nullable: false, oldClrType: typeof(long), oldType: "bigint",
                oldNullable: true);
            migrationBuilder.AlterColumn<long>(
                name: "empresa_id", schema: "s_compras",
                table: "compras_recepciones_detalles_lotes", type: "bigint",
                nullable: false, oldClrType: typeof(long), oldType: "bigint",
                oldNullable: true);
            migrationBuilder.AlterColumn<long>(
                name: "producto_id", schema: "s_compras",
                table: "compras_recepciones_detalles_series", type: "bigint",
                nullable: false, oldClrType: typeof(long), oldType: "bigint",
                oldNullable: true);
            migrationBuilder.AlterColumn<long>(
                name: "bodega_id", schema: "s_compras",
                table: "compras_recepciones_detalles_series", type: "bigint",
                nullable: false, oldClrType: typeof(long), oldType: "bigint",
                oldNullable: true);
            migrationBuilder.AlterColumn<long>(
                name: "empresa_id", schema: "s_compras",
                table: "compras_recepciones_detalles_series", type: "bigint",
                nullable: false, oldClrType: typeof(long), oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_productos_series_id_producto_bodega",
                schema: "s_inventario",
                table: "productos_series",
                columns: new[] { "id", "producto_id", "bodega_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_productos_lotes_id_producto",
                schema: "s_inventario",
                table: "productos_lotes",
                columns: new[] { "id", "producto_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_compras_recepciones_detalles_id_producto_bodega_empresa",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "id", "producto_id", "bodega_id", "empresa_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_compras_recepciones_detalles_id_producto_empresa",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "id", "producto_id", "empresa_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_compras_recepciones_id_compra_bodega_empresa",
                schema: "s_compras",
                table: "compras_recepciones",
                columns: new[] { "id", "compra_id", "bodega_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_productos_series_producto_lote_id_producto_id",
                schema: "s_inventario",
                table: "productos_series",
                columns: new[] { "producto_lote_id", "producto_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_series_compra_recepcion_detall~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                columns: new[] { "compra_recepcion_detalle_id", "producto_id", "bodega_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_series_producto_serie_id_produ~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                columns: new[] { "producto_serie_id", "producto_id", "bodega_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_lotes_compra_recepcion_detalle~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                columns: new[] { "compra_recepcion_detalle_id", "producto_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_lotes_producto_lote_id_product~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                columns: new[] { "producto_lote_id", "producto_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_compra_recepcion_id_compra_id_~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "compra_id", "bodega_id", "empresa_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "compra_id", "bodega_id", "empresa_id" },
                principalSchema: "s_compras",
                principalTable: "compras_recepciones",
                principalColumns: new[] { "id", "compra_id", "bodega_id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_lotes_compras_recepciones_deta~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                columns: new[] { "compra_recepcion_detalle_id", "producto_id", "empresa_id" },
                principalSchema: "s_compras",
                principalTable: "compras_recepciones_detalles",
                principalColumns: new[] { "id", "producto_id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_lotes_productos_lotes_producto~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                columns: new[] { "producto_lote_id", "producto_id" },
                principalSchema: "s_inventario",
                principalTable: "productos_lotes",
                principalColumns: new[] { "id", "producto_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_series_compras_recepciones_det~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                columns: new[] { "compra_recepcion_detalle_id", "producto_id", "bodega_id", "empresa_id" },
                principalSchema: "s_compras",
                principalTable: "compras_recepciones_detalles",
                principalColumns: new[] { "id", "producto_id", "bodega_id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_series_productos_series_produc~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                columns: new[] { "producto_serie_id", "producto_id", "bodega_id" },
                principalSchema: "s_inventario",
                principalTable: "productos_series",
                principalColumns: new[] { "id", "producto_id", "bodega_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_productos_series_productos_lotes_producto_lote_id_producto_~",
                schema: "s_inventario",
                table: "productos_series",
                columns: new[] { "producto_lote_id", "producto_id" },
                principalSchema: "s_inventario",
                principalTable: "productos_lotes",
                principalColumns: new[] { "id", "producto_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_lotes_compras_recepciones_deta~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_lotes_productos_lotes_producto~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_series_compras_recepciones_det~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_series_productos_series_produc~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series");

            migrationBuilder.DropForeignKey(
                name: "FK_productos_series_productos_lotes_producto_lote_id_producto_~",
                schema: "s_inventario",
                table: "productos_series");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_productos_series_id_producto_bodega",
                schema: "s_inventario",
                table: "productos_series");

            migrationBuilder.DropIndex(
                name: "IX_productos_series_producto_lote_id_producto_id",
                schema: "s_inventario",
                table: "productos_series");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_productos_lotes_id_producto",
                schema: "s_inventario",
                table: "productos_lotes");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_series_compra_recepcion_detall~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_series_producto_serie_id_produ~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_lotes_compra_recepcion_detalle~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_lotes_producto_lote_id_product~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_compras_recepciones_detalles_id_producto_bodega_empresa",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_compras_recepciones_detalles_id_producto_empresa",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_compra_recepcion_id_compra_id_~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_compras_recepciones_id_compra_bodega_empresa",
                schema: "s_compras",
                table: "compras_recepciones");

            migrationBuilder.DropColumn(
                name: "bodega_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series");

            migrationBuilder.DropColumn(
                name: "empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series");

            migrationBuilder.DropColumn(
                name: "producto_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series");

            migrationBuilder.DropColumn(
                name: "empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes");

            migrationBuilder.DropColumn(
                name: "producto_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes");

            migrationBuilder.DropColumn(
                name: "bodega_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.CreateIndex(
                name: "IX_productos_series_producto_lote_id",
                schema: "s_inventario",
                table: "productos_series",
                column: "producto_lote_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_series_producto_serie_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                column: "producto_serie_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_lotes_producto_lote_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                column: "producto_lote_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_compra_recepcion_id_compra_id_~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "compra_id", "empresa_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "compra_id", "empresa_id" },
                principalSchema: "s_compras",
                principalTable: "compras_recepciones",
                principalColumns: new[] { "id", "compra_id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_lotes_compras_recepciones_deta~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                column: "compra_recepcion_detalle_id",
                principalSchema: "s_compras",
                principalTable: "compras_recepciones_detalles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_lotes_productos_lotes_producto~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                column: "producto_lote_id",
                principalSchema: "s_inventario",
                principalTable: "productos_lotes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_series_compras_recepciones_det~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                column: "compra_recepcion_detalle_id",
                principalSchema: "s_compras",
                principalTable: "compras_recepciones_detalles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_series_productos_series_produc~",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                column: "producto_serie_id",
                principalSchema: "s_inventario",
                principalTable: "productos_series",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_productos_series_productos_lotes_producto_lote_id",
                schema: "s_inventario",
                table: "productos_series",
                column: "producto_lote_id",
                principalSchema: "s_inventario",
                principalTable: "productos_lotes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
