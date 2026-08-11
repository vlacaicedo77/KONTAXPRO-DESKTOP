using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ComprasV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_detalles_bodegas_bodega_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_detalles_bodega_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_detalles_compra_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_detalles_producto",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "ix_compras_empresa_numero",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.DropColumn(
                name: "bodega_id",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "fecha_autorizacion",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "ambiente",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "archivo_ruta_relativa",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "archivo_sha256",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "archivo_tamano",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "direccion_establecimiento",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "direccion_matriz",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "establecimiento_codigo",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "estado_validacion",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "firma_presente",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "moneda",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nombre_comercial_emisor",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "propina",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "punto_emision_codigo",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "razon_social_emisor",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "razon_social_receptor",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ruc_emisor",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "secuencial",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tipo_emision",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "codigo_auxiliar_proveedor",
                schema: "s_compras",
                table: "compras_detalles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_principal_proveedor",
                schema: "s_compras",
                table: "compras_detalles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "es_inventariable",
                schema: "s_compras",
                table: "compras_detalles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "estado_reconocimiento",
                schema: "s_compras",
                table: "compras_detalles",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "orden",
                schema: "s_compras",
                table: "compras_detalles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "precio_total_sin_impuesto",
                schema: "s_compras",
                table: "compras_detalles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "fecha_vencimiento",
                schema: "s_compras",
                table: "compras",
                type: "date",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_productos_presentaciones_id_empresa_id",
                schema: "s_inventario",
                table: "productos_presentaciones",
                columns: new[] { "id", "empresa_id" });

            migrationBuilder.CreateTable(
                name: "compras_pagos",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    compra_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_forma_pago_sri = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    plazo = table.Column<int>(type: "integer", nullable: true),
                    unidad_tiempo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_pagos", x => x.id);
                    table.CheckConstraint("ck_compras_pagos_plazo", "plazo IS NULL OR plazo >= 0");
                    table.CheckConstraint("ck_compras_pagos_valor", "valor > 0");
                    table.ForeignKey(
                        name: "FK_compras_pagos_compras_compra_id",
                        column: x => x.compra_id,
                        principalSchema: "s_compras",
                        principalTable: "compras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras_recepciones",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    operacion_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    compra_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_recepcion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fecha_recepcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    movimiento_inventario_id = table.Column<long>(type: "bigint", nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_recepciones", x => x.id);
                    table.UniqueConstraint("ak_compras_recepciones_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_compras_recepciones_estado", "estado IN ('CONFIRMADA', 'ANULADA')");
                    table.ForeignKey(
                        name: "FK_compras_recepciones_bodegas_bodega_id_establecimiento_id",
                        columns: x => new { x.bodega_id, x.establecimiento_id },
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_compras_compra_id_empresa_id",
                        columns: x => new { x.compra_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "compras",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_establecimientos_establecimiento_id_emp~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_movimientos_inventario_movimiento_inven~",
                        column: x => x.movimiento_inventario_id,
                        principalSchema: "s_inventario",
                        principalTable: "movimientos_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proveedores_productos_equivalencias",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_proveedor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    codigo_proveedor_normalizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo_codigo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    descripcion_original = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creado_por_usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedores_productos_equivalencias", x => x.id);
                    table.CheckConstraint("ck_proveedores_productos_equivalencias_tipo", "tipo_codigo IN ('PRINCIPAL', 'AUXILIAR')");
                    table.ForeignKey(
                        name: "FK_proveedores_productos_equivalencias_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proveedores_productos_equivalencias_productos_presentacione~",
                        columns: x => new { x.producto_presentacion_id, x.empresa_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proveedores_productos_equivalencias_terceros_tercero_id",
                        column: x => x.tercero_id,
                        principalSchema: "s_comercial",
                        principalTable: "terceros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proveedores_productos_equivalencias_usuarios_creado_por_usu~",
                        column: x => x.creado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras_recepciones_detalles",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    compra_recepcion_id = table.Column<long>(type: "bigint", nullable: false),
                    compra_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    es_bonificacion = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_recepciones_detalles", x => x.id);
                    table.CheckConstraint("ck_compras_recepciones_detalles_cantidad", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0 AND costo_unitario_base >= 0 AND costo_total >= 0");
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_compras_detalles_compra_detall~",
                        column: x => x.compra_detalle_id,
                        principalSchema: "s_compras",
                        principalTable: "compras_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                        column: x => x.compra_recepcion_id,
                        principalSchema: "s_compras",
                        principalTable: "compras_recepciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_productos_presentaciones_produ~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras_recepciones_detalles_lotes",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    compra_recepcion_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_lote_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_lote = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    fecha_elaboracion = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_caducidad = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_recepciones_detalles_lotes", x => x.id);
                    table.CheckConstraint("ck_compras_recepciones_detalles_lotes_cantidad", "cantidad_base > 0");
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_lotes_compras_recepciones_deta~",
                        column: x => x.compra_recepcion_detalle_id,
                        principalSchema: "s_compras",
                        principalTable: "compras_recepciones_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_lotes_productos_lotes_producto~",
                        column: x => x.producto_lote_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras_recepciones_detalles_series",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    compra_recepcion_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_serie_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_serie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_recepciones_detalles_series", x => x.id);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_series_compras_recepciones_det~",
                        column: x => x.compra_recepcion_detalle_id,
                        principalSchema: "s_compras",
                        principalTable: "compras_recepciones_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_series_productos_series_produc~",
                        column: x => x.producto_serie_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_documentos_recibidos_sri_archivo_sha256",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                column: "archivo_sha256",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_recibidos_sri_archivo",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                sql: "archivo_tamano > 0 AND length(archivo_sha256) = 64");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_recibidos_sri_validacion",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                sql: "estado_validacion IN ('VALIDADO_LOCALMENTE', 'ADVERTENCIA', 'RECHAZADO')");

            migrationBuilder.CreateIndex(
                name: "ux_compras_detalles_compra_orden",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "compra_id", "orden" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_detalles_orden",
                schema: "s_compras",
                table: "compras_detalles",
                sql: "orden > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_detalles_producto",
                schema: "s_compras",
                table: "compras_detalles",
                sql: "(es_inventariable AND producto_id IS NOT NULL AND producto_presentacion_id IS NOT NULL AND estado_reconocimiento = 'RECONOCIDA') OR (NOT es_inventariable AND producto_id IS NULL AND producto_presentacion_id IS NULL AND estado_reconocimiento = 'NO_INVENTARIABLE') OR (estado_reconocimiento IN ('SUGERIDA', 'NO_RECONOCIDA'))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_detalles_reconocimiento",
                schema: "s_compras",
                table: "compras_detalles",
                sql: "estado_reconocimiento IN ('RECONOCIDA', 'SUGERIDA', 'NO_RECONOCIDA', 'NO_INVENTARIABLE')");

            migrationBuilder.CreateIndex(
                name: "ux_compras_empresa_proveedor_tipo_numero",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "empresa_id", "empresa_tercero_id", "tipo_comprobante_id", "numero_documento" },
                unique: true,
                filter: "numero_documento IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_estado",
                schema: "s_compras",
                table: "compras",
                sql: "estado IN ('BORRADOR', 'PENDIENTE_RECEPCION', 'PARCIALMENTE_RECIBIDA', 'RECIBIDA', 'ANULADA')");

            migrationBuilder.CreateIndex(
                name: "IX_compras_pagos_compra_id",
                schema: "s_compras",
                table: "compras_pagos",
                column: "compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_bodega_id_establecimiento_id",
                schema: "s_compras",
                table: "compras_recepciones",
                columns: new[] { "bodega_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_compra_id_empresa_id",
                schema: "s_compras",
                table: "compras_recepciones",
                columns: new[] { "compra_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_establecimiento_id_empresa_id",
                schema: "s_compras",
                table: "compras_recepciones",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_usuario_id",
                schema: "s_compras",
                table: "compras_recepciones",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_compras_recepciones_empresa_numero",
                schema: "s_compras",
                table: "compras_recepciones",
                columns: new[] { "empresa_id", "numero_recepcion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_compras_recepciones_movimiento",
                schema: "s_compras",
                table: "compras_recepciones",
                column: "movimiento_inventario_id",
                unique: true,
                filter: "movimiento_inventario_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_compras_recepciones_operacion_uuid",
                schema: "s_compras",
                table: "compras_recepciones",
                column: "operacion_uuid",
                unique: true);

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
                name: "ux_compras_recepciones_detalles_compra_detalle",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "compra_detalle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_lotes_producto_lote_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                column: "producto_lote_id");

            migrationBuilder.CreateIndex(
                name: "ux_compras_recepciones_detalles_lotes_lote",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                columns: new[] { "compra_recepcion_detalle_id", "producto_lote_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_series_producto_serie_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                column: "producto_serie_id");

            migrationBuilder.CreateIndex(
                name: "ux_compras_recepciones_detalles_series_serie",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                columns: new[] { "compra_recepcion_detalle_id", "producto_serie_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proveedores_productos_equivalencias_creado_por_usuario_id",
                schema: "s_compras",
                table: "proveedores_productos_equivalencias",
                column: "creado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_proveedores_productos_equivalencias_producto_presentacion_i~",
                schema: "s_compras",
                table: "proveedores_productos_equivalencias",
                columns: new[] { "producto_presentacion_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_proveedores_productos_equivalencias_tercero_id",
                schema: "s_compras",
                table: "proveedores_productos_equivalencias",
                column: "tercero_id");

            migrationBuilder.CreateIndex(
                name: "ux_proveedor_producto_equivalencia_codigo",
                schema: "s_compras",
                table: "proveedores_productos_equivalencias",
                columns: new[] { "empresa_id", "tercero_id", "tipo_codigo", "codigo_proveedor_normalizado" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compras_pagos",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "compras_recepciones_detalles_lotes",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "compras_recepciones_detalles_series",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "proveedores_productos_equivalencias",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "compras_recepciones_detalles",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "compras_recepciones",
                schema: "s_compras");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_productos_presentaciones_id_empresa_id",
                schema: "s_inventario",
                table: "productos_presentaciones");

            migrationBuilder.DropIndex(
                name: "ux_documentos_recibidos_sri_archivo_sha256",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_recibidos_sri_archivo",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_recibidos_sri_validacion",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropIndex(
                name: "ux_compras_detalles_compra_orden",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_detalles_orden",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_detalles_producto",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_detalles_reconocimiento",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropIndex(
                name: "ux_compras_empresa_proveedor_tipo_numero",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_estado",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.DropColumn(
                name: "ambiente",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "archivo_ruta_relativa",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "archivo_sha256",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "archivo_tamano",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "direccion_establecimiento",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "direccion_matriz",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "establecimiento_codigo",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "estado_validacion",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "firma_presente",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "moneda",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "nombre_comercial_emisor",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "propina",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "punto_emision_codigo",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "razon_social_emisor",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "razon_social_receptor",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "ruc_emisor",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "secuencial",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "tipo_emision",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.DropColumn(
                name: "codigo_auxiliar_proveedor",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropColumn(
                name: "codigo_principal_proveedor",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropColumn(
                name: "es_inventariable",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropColumn(
                name: "estado_reconocimiento",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropColumn(
                name: "orden",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropColumn(
                name: "precio_total_sin_impuesto",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropColumn(
                name: "fecha_vencimiento",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.AlterColumn<DateTime>(
                name: "fecha_autorizacion",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "bodega_id",
                schema: "s_compras",
                table: "compras_detalles",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_bodega_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_compra_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "compra_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_detalles_producto",
                schema: "s_compras",
                table: "compras_detalles",
                sql: "(producto_presentacion_id IS NULL OR producto_id IS NOT NULL) AND (bodega_id IS NULL OR producto_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_compras_empresa_numero",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "empresa_id", "numero_documento" },
                filter: "numero_documento IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_compras_detalles_bodegas_bodega_id",
                schema: "s_compras",
                table: "compras_detalles",
                column: "bodega_id",
                principalSchema: "s_inventario",
                principalTable: "bodegas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
