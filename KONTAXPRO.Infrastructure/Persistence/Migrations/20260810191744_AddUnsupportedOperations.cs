using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnsupportedOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_documento",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_tipo",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_documento",
                schema: "s_compras",
                table: "compras",
                sql: "tipo_compra = 'FACTURADA' AND tipo_comprobante_id IS NOT NULL AND numero_documento IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_tipo",
                schema: "s_compras",
                table: "compras",
                sql: "tipo_compra = 'FACTURADA'");

            migrationBuilder.CreateTable(
                name: "operaciones_sin_sustento",
                schema: "s_tesoreria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_operacion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tipo_operacion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    medio_salida = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    caja_sesion_id = table.Column<long>(type: "bigint", nullable: true),
                    cuenta_bancaria_id = table.Column<long>(type: "bigint", nullable: true),
                    bodega_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    beneficiario = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    evidencia_ruta_relativa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    evidencia_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    evidencia_tamano = table.Column<long>(type: "bigint", nullable: true),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    es_deducible = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    movimiento_caja_id = table.Column<long>(type: "bigint", nullable: true),
                    movimiento_bancario_id = table.Column<long>(type: "bigint", nullable: true),
                    movimiento_inventario_id = table.Column<long>(type: "bigint", nullable: true),
                    asiento_id = table.Column<long>(type: "bigint", nullable: true),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operaciones_sin_sustento", x => x.id);
                    table.UniqueConstraint("ak_operaciones_sin_sustento_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_operaciones_sin_sustento_estado", "estado IN ('CONFIRMADO','ANULADO')");
                    table.CheckConstraint("ck_operaciones_sin_sustento_evidencia", "(evidencia_ruta_relativa IS NULL AND evidencia_sha256 IS NULL AND evidencia_tamano IS NULL) OR (evidencia_ruta_relativa IS NOT NULL AND evidencia_sha256 IS NOT NULL AND evidencia_tamano > 0)");
                    table.CheckConstraint("ck_operaciones_sin_sustento_fondo", "(medio_salida = 'CAJA' AND caja_sesion_id IS NOT NULL AND cuenta_bancaria_id IS NULL) OR (medio_salida = 'BANCO' AND cuenta_bancaria_id IS NOT NULL AND caja_sesion_id IS NULL)");
                    table.CheckConstraint("ck_operaciones_sin_sustento_inventario", "(tipo_operacion = 'GASTO' AND bodega_id IS NULL AND movimiento_inventario_id IS NULL) OR (tipo_operacion = 'INVENTARIO' AND bodega_id IS NOT NULL AND movimiento_inventario_id IS NOT NULL)");
                    table.CheckConstraint("ck_operaciones_sin_sustento_medio", "medio_salida IN ('CAJA','BANCO')");
                    table.CheckConstraint("ck_operaciones_sin_sustento_no_deducible", "es_deducible = FALSE");
                    table.CheckConstraint("ck_operaciones_sin_sustento_tipo", "tipo_operacion IN ('GASTO','INVENTARIO')");
                    table.CheckConstraint("ck_operaciones_sin_sustento_total", "total > 0");
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_asientos_asiento_id",
                        column: x => x.asiento_id,
                        principalSchema: "s_contabilidad",
                        principalTable: "asientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_cajas_sesiones_caja_sesion_id",
                        column: x => x.caja_sesion_id,
                        principalSchema: "s_tesoreria",
                        principalTable: "cajas_sesiones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_cuentas_bancarias_cuenta_bancaria_~",
                        column: x => x.cuenta_bancaria_id,
                        principalSchema: "s_bancos",
                        principalTable: "cuentas_bancarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_establecimientos_establecimiento_i~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_movimientos_bancarios_movimiento_b~",
                        column: x => x.movimiento_bancario_id,
                        principalSchema: "s_bancos",
                        principalTable: "movimientos_bancarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_movimientos_caja_movimiento_caja_id",
                        column: x => x.movimiento_caja_id,
                        principalSchema: "s_tesoreria",
                        principalTable: "movimientos_caja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_movimientos_inventario_movimiento_~",
                        column: x => x.movimiento_inventario_id,
                        principalSchema: "s_inventario",
                        principalTable: "movimientos_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operaciones_sin_sustento_detalles",
                schema: "s_tesoreria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    operacion_sin_sustento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    cuenta_contable_id = table.Column<long>(type: "bigint", nullable: true),
                    producto_id = table.Column<long>(type: "bigint", nullable: true),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operaciones_sin_sustento_detalles", x => x.id);
                    table.CheckConstraint("ck_operaciones_sin_sustento_detalle_costo", "costo_total > 0 AND cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0 AND costo_unitario_base >= 0");
                    table.CheckConstraint("ck_operaciones_sin_sustento_detalle_destino", "(cuenta_contable_id IS NOT NULL AND producto_id IS NULL AND producto_presentacion_id IS NULL) OR (cuenta_contable_id IS NULL AND producto_id IS NOT NULL AND producto_presentacion_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_detalles_operaciones_sin_sustento_~",
                        columns: x => new { x.operacion_sin_sustento_id, x.empresa_id },
                        principalSchema: "s_tesoreria",
                        principalTable: "operaciones_sin_sustento",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_detalles_plan_cuentas_cuenta_conta~",
                        columns: x => new { x.cuenta_contable_id, x.empresa_id },
                        principalSchema: "s_contabilidad",
                        principalTable: "plan_cuentas",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_detalles_productos_presentaciones_~",
                        columns: x => new { x.producto_presentacion_id, x.empresa_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_sin_sustento_detalles_productos_producto_id_emp~",
                        columns: x => new { x.producto_id, x.empresa_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_anulado_por_usuario_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_asiento_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "asiento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_bodega_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_caja_sesion_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "caja_sesion_id");

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_cuenta_bancaria_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "cuenta_bancaria_id");

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_establecimiento_id_empresa_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_movimiento_bancario_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "movimiento_bancario_id",
                unique: true,
                filter: "movimiento_bancario_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_movimiento_caja_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "movimiento_caja_id",
                unique: true,
                filter: "movimiento_caja_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_movimiento_inventario_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "movimiento_inventario_id",
                unique: true,
                filter: "movimiento_inventario_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_usuario_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_operaciones_sin_sustento_empresa_numero",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                columns: new[] { "empresa_id", "numero_operacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_detalles_cuenta_contable_id_empres~",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento_detalles",
                columns: new[] { "cuenta_contable_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_detalles_operacion_sin_sustento_id~",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento_detalles",
                columns: new[] { "operacion_sin_sustento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_detalles_producto_id_empresa_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento_detalles",
                columns: new[] { "producto_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_detalles_producto_presentacion_id_~",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento_detalles",
                columns: new[] { "producto_presentacion_id", "empresa_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operaciones_sin_sustento_detalles",
                schema: "s_tesoreria");

            migrationBuilder.DropTable(
                name: "operaciones_sin_sustento",
                schema: "s_tesoreria");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_documento",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_tipo",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_documento",
                schema: "s_compras",
                table: "compras",
                sql: "(tipo_compra = 'FACTURADA' AND tipo_comprobante_id IS NOT NULL AND numero_documento IS NOT NULL) OR (tipo_compra = 'SIN_FACTURA')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_tipo",
                schema: "s_compras",
                table: "compras",
                sql: "tipo_compra IN ('FACTURADA', 'SIN_FACTURA')");
        }
    }
}
