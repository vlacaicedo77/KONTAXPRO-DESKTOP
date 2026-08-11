using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePurchaseDetailCompanyIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_detalles_compras_compra_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_detalles_plan_cuentas_cuenta_contable_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_detalles_productos_presentaciones_producto_presenta~",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_detalles_productos_producto_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_compras_detalles_compra_detall~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_productos_presentaciones_produ~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_productos_producto_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_compra_detalle_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_producto_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_producto_presentacion_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_detalles_cuenta_contable_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_detalles_producto_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_detalles_producto_presentacion_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.AddColumn<long>(
                name: "empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "empresa_id",
                schema: "s_compras",
                table: "compras_detalles",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE s_compras.compras_detalles AS d
                SET empresa_id = c.empresa_id
                FROM s_compras.compras AS c
                WHERE c.id = d.compra_id;

                UPDATE s_compras.compras_recepciones_detalles AS d
                SET empresa_id = r.empresa_id
                FROM s_compras.compras_recepciones AS r
                WHERE r.id = d.compra_recepcion_id;

                DO $integrity$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM s_compras.compras_detalles d
                        LEFT JOIN s_contabilidad.plan_cuentas pc
                          ON pc.id = d.cuenta_contable_id
                         AND pc.empresa_id = d.empresa_id
                        LEFT JOIN s_inventario.productos p
                          ON p.id = d.producto_id
                         AND p.empresa_id = d.empresa_id
                        LEFT JOIN s_inventario.productos_presentaciones pp
                          ON pp.id = d.producto_presentacion_id
                         AND pp.producto_id = d.producto_id
                         AND pp.empresa_id = d.empresa_id
                        WHERE d.empresa_id IS NULL
                           OR pc.id IS NULL
                           OR (d.producto_id IS NOT NULL AND p.id IS NULL)
                           OR (d.producto_presentacion_id IS NOT NULL AND pp.id IS NULL)
                    ) THEN
                        RAISE EXCEPTION 'Existen detalles de compra con relaciones fuera de su empresa o presentación no perteneciente al producto.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM s_compras.compras_recepciones_detalles rd
                        LEFT JOIN s_compras.compras_detalles d
                          ON d.id = rd.compra_detalle_id
                         AND d.empresa_id = rd.empresa_id
                        LEFT JOIN s_inventario.productos p
                          ON p.id = rd.producto_id
                         AND p.empresa_id = rd.empresa_id
                        LEFT JOIN s_inventario.productos_presentaciones pp
                          ON pp.id = rd.producto_presentacion_id
                         AND pp.producto_id = rd.producto_id
                         AND pp.empresa_id = rd.empresa_id
                        WHERE rd.empresa_id IS NULL
                           OR d.id IS NULL
                           OR p.id IS NULL
                           OR pp.id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Existen detalles de recepción con relaciones fuera de su empresa o presentación no perteneciente al producto.';
                    END IF;
                END
                $integrity$;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "empresa_id",
                schema: "s_compras",
                table: "compras_detalles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_productos_presentaciones_id_producto_empresa",
                schema: "s_inventario",
                table: "productos_presentaciones",
                columns: new[] { "id", "producto_id", "empresa_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_compras_recepciones_detalles_id_empresa",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "id", "empresa_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_compras_detalles_id_empresa",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_compra_detalle_id_empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_detalle_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_compra_recepcion_id_empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_producto_id_empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "producto_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_producto_presentacion_id_produ~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "producto_presentacion_id", "producto_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_compra_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "compra_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_cuenta_contable_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "cuenta_contable_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_producto_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "producto_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_producto_presentacion_id_producto_id_empre~",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "producto_presentacion_id", "producto_id", "empresa_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_compras_detalles_compras_compra_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "compra_id", "empresa_id" },
                principalSchema: "s_compras",
                principalTable: "compras",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_detalles_plan_cuentas_cuenta_contable_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "cuenta_contable_id", "empresa_id" },
                principalSchema: "s_contabilidad",
                principalTable: "plan_cuentas",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_detalles_productos_presentaciones_producto_presenta~",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "producto_presentacion_id", "producto_id", "empresa_id" },
                principalSchema: "s_inventario",
                principalTable: "productos_presentaciones",
                principalColumns: new[] { "id", "producto_id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_detalles_productos_producto_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "producto_id", "empresa_id" },
                principalSchema: "s_inventario",
                principalTable: "productos",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_compras_detalles_compra_detall~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_detalle_id", "empresa_id" },
                principalSchema: "s_compras",
                principalTable: "compras_detalles",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "empresa_id" },
                principalSchema: "s_compras",
                principalTable: "compras_recepciones",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_productos_presentaciones_produ~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "producto_presentacion_id", "producto_id", "empresa_id" },
                principalSchema: "s_inventario",
                principalTable: "productos_presentaciones",
                principalColumns: new[] { "id", "producto_id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_productos_producto_id_empresa_~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "producto_id", "empresa_id" },
                principalSchema: "s_inventario",
                principalTable: "productos",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_detalles_compras_compra_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_detalles_plan_cuentas_cuenta_contable_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_detalles_productos_presentaciones_producto_presenta~",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_detalles_productos_producto_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_compras_detalles_compra_detall~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_productos_presentaciones_produ~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_productos_producto_id_empresa_~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_productos_presentaciones_id_producto_empresa",
                schema: "s_inventario",
                table: "productos_presentaciones");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_compras_recepciones_detalles_id_empresa",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_compra_detalle_id_empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_compra_recepcion_id_empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_producto_id_empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_producto_presentacion_id_produ~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_compras_detalles_id_empresa",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_detalles_compra_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_detalles_cuenta_contable_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_detalles_producto_id_empresa_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_detalles_producto_presentacion_id_producto_id_empre~",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropColumn(
                name: "empresa_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropColumn(
                name: "empresa_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_compra_detalle_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                column: "compra_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_producto_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_producto_presentacion_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_cuenta_contable_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "cuenta_contable_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_producto_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_producto_presentacion_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.AddForeignKey(
                name: "FK_compras_detalles_compras_compra_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "compra_id",
                principalSchema: "s_compras",
                principalTable: "compras",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_detalles_plan_cuentas_cuenta_contable_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "cuenta_contable_id",
                principalSchema: "s_contabilidad",
                principalTable: "plan_cuentas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_detalles_productos_presentaciones_producto_presenta~",
                schema: "s_compras",
                table: "compras_detalles",
                column: "producto_presentacion_id",
                principalSchema: "s_inventario",
                principalTable: "productos_presentaciones",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_detalles_productos_producto_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "producto_id",
                principalSchema: "s_inventario",
                principalTable: "productos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_compras_detalles_compra_detall~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                column: "compra_detalle_id",
                principalSchema: "s_compras",
                principalTable: "compras_detalles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                column: "compra_recepcion_id",
                principalSchema: "s_compras",
                principalTable: "compras_recepciones",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_productos_presentaciones_produ~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                column: "producto_presentacion_id",
                principalSchema: "s_inventario",
                principalTable: "productos_presentaciones",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_productos_producto_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                column: "producto_id",
                principalSchema: "s_inventario",
                principalTable: "productos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
