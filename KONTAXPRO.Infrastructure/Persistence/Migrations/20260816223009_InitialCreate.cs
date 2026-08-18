using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "s_compras");

            migrationBuilder.EnsureSchema(
                name: "s_inventario");

            migrationBuilder.EnsureSchema(
                name: "s_contabilidad");

            migrationBuilder.EnsureSchema(
                name: "s_seguridad");

            migrationBuilder.EnsureSchema(
                name: "s_tesoreria");

            migrationBuilder.EnsureSchema(
                name: "s_catalogos");

            migrationBuilder.EnsureSchema(
                name: "s_cartera");

            migrationBuilder.EnsureSchema(
                name: "s_facturacion_electronica");

            migrationBuilder.EnsureSchema(
                name: "s_bancos");

            migrationBuilder.EnsureSchema(
                name: "s_ventas");

            migrationBuilder.EnsureSchema(
                name: "s_configuracion");

            migrationBuilder.EnsureSchema(
                name: "s_comercial");

            migrationBuilder.EnsureSchema(
                name: "s_tributacion");

            migrationBuilder.CreateTable(
                name: "estados_comprobante_electronico",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estados_comprobante_electronico", x => x.id);
                    table.CheckConstraint("ck_estados_comprobante_electronico_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "estados_serie",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estados_serie", x => x.id);
                    table.CheckConstraint("ck_estados_serie_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "formas_pago",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formas_pago", x => x.id);
                    table.CheckConstraint("ck_formas_pago_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "impuestos",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_impuestos", x => x.id);
                    table.CheckConstraint("ck_impuestos_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "marcas",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marcas", x => x.id);
                    table.CheckConstraint("ck_marcas_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "permisos",
                schema: "s_seguridad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    modulo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permisos", x => x.id);
                    table.CheckConstraint("ck_permisos_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "regimenes_tributarios",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regimenes_tributarios", x => x.id);
                    table.CheckConstraint("ck_regimenes_tributarios_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "s_seguridad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    es_sistema = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.CheckConstraint("ck_roles_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_ambiente",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_ambiente", x => x.id);
                    table.CheckConstraint("ck_tipos_ambiente_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_comprobante",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_comprobante", x => x.id);
                    table.CheckConstraint("ck_tipos_comprobante_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_configuracion_contable",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_configuracion_contable", x => x.id);
                    table.CheckConstraint("ck_tipos_configuracion_contable_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_documento_interno",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    prefijo_default = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_documento_interno", x => x.id);
                    table.CheckConstraint("ck_tipos_documento_interno_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_emision",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_emision", x => x.id);
                    table.CheckConstraint("ck_tipos_emision_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_identificacion",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_sri = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    codigo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    longitud_minima = table.Column<int>(type: "integer", nullable: false),
                    longitud_maxima = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_identificacion", x => x.id);
                    table.CheckConstraint("ck_tipos_identificacion_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_tipos_identificacion_longitudes", "longitud_minima > 0 AND longitud_maxima >= longitud_minima");
                });

            migrationBuilder.CreateTable(
                name: "tipos_movimiento_bancario",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    naturaleza = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_movimiento_bancario", x => x.id);
                    table.CheckConstraint("ck_tipos_movimiento_bancario_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_tipos_movimiento_bancario_naturaleza", "naturaleza IN ('ENTRADA', 'SALIDA')");
                });

            migrationBuilder.CreateTable(
                name: "tipos_movimiento_caja",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    naturaleza = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_movimiento_caja", x => x.id);
                    table.CheckConstraint("ck_tipos_movimiento_caja_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_tipos_movimiento_caja_naturaleza", "naturaleza IN ('ENTRADA', 'SALIDA')");
                });

            migrationBuilder.CreateTable(
                name: "tipos_movimiento_cartera",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    naturaleza = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_movimiento_cartera", x => x.id);
                    table.CheckConstraint("ck_tipos_movimiento_cartera_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_tipos_movimiento_cartera_naturaleza", "naturaleza IN ('DEBITO', 'CREDITO')");
                });

            migrationBuilder.CreateTable(
                name: "tipos_movimiento_cuentas_por_pagar",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    naturaleza = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_movimiento_cuentas_por_pagar", x => x.id);
                    table.CheckConstraint("ck_tipos_movimiento_cuentas_por_pagar_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_tipos_movimiento_cuentas_por_pagar_naturaleza", "naturaleza IN ('DEBITO', 'CREDITO')");
                });

            migrationBuilder.CreateTable(
                name: "tipos_movimiento_inventario",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    naturaleza = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_movimiento_inventario", x => x.id);
                    table.CheckConstraint("ck_tipos_movimiento_inventario_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_tipos_movimiento_inventario_naturaleza", "naturaleza IN ('ENTRADA', 'SALIDA')");
                });

            migrationBuilder.CreateTable(
                name: "tipos_origen_asiento",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_origen_asiento", x => x.id);
                    table.CheckConstraint("ck_tipos_origen_asiento_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_origen_comprobante_electronico",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_origen_comprobante_electronico", x => x.id);
                    table.CheckConstraint("ck_tipos_origen_comprobante_electronico_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_origen_devolucion_compra",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_origen_devolucion_compra", x => x.id);
                    table.CheckConstraint("ck_tipos_origen_devolucion_compra_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_origen_devolucion_venta",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_origen_devolucion_venta", x => x.id);
                    table.CheckConstraint("ck_tipos_origen_devolucion_venta_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_origen_guia_remision",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_origen_guia_remision", x => x.id);
                    table.CheckConstraint("ck_tipos_origen_guia_remision_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_origen_movimiento_inventario",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_origen_movimiento_inventario", x => x.id);
                    table.CheckConstraint("ck_tipos_origen_movimiento_inventario_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "tipos_origen_retencion_emitida",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_origen_retencion_emitida", x => x.id);
                    table.CheckConstraint("ck_tipos_origen_retencion_emitida_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "unidades_medida",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    abreviatura = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidades_medida", x => x.id);
                    table.CheckConstraint("ck_unidades_medida_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "s_seguridad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero_identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre_completo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    requiere_cambio_clave = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ultimo_acceso_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                    table.CheckConstraint("ck_usuarios_estado", "estado IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "medios_pago",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    forma_pago_sri_id = table.Column<long>(type: "bigint", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medios_pago", x => x.id);
                    table.CheckConstraint("ck_medios_pago_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_medios_pago_formas_pago_forma_pago_sri_id",
                        column: x => x.forma_pago_sri_id,
                        principalSchema: "s_catalogos",
                        principalTable: "formas_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conceptos_retencion",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    impuesto_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    vigente_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    vigente_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conceptos_retencion", x => x.id);
                    table.CheckConstraint("ck_conceptos_retencion_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_conceptos_retencion_porcentaje", "porcentaje BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_conceptos_retencion_vigencia", "vigente_hasta IS NULL OR vigente_hasta >= vigente_desde");
                    table.ForeignKey(
                        name: "FK_conceptos_retencion_impuestos_impuesto_id",
                        column: x => x.impuesto_id,
                        principalSchema: "s_catalogos",
                        principalTable: "impuestos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tarifas_impuesto",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    impuesto_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_calculo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    valor_especifico = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    vigente_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    vigente_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tarifas_impuesto", x => x.id);
                    table.CheckConstraint("ck_tarifas_impuesto_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_tarifas_impuesto_porcentaje", "porcentaje IS NULL OR porcentaje BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_tarifas_impuesto_tipo_calculo", "tipo_calculo IN ('NINGUNO', 'PORCENTAJE', 'ESPECIFICO', 'MIXTO')");
                    table.CheckConstraint("ck_tarifas_impuesto_valor_especifico", "valor_especifico IS NULL OR valor_especifico >= 0");
                    table.CheckConstraint("ck_tarifas_impuesto_valores", "(tipo_calculo = 'NINGUNO' AND porcentaje IS NULL AND valor_especifico IS NULL) OR (tipo_calculo = 'PORCENTAJE' AND porcentaje IS NOT NULL AND valor_especifico IS NULL) OR (tipo_calculo = 'ESPECIFICO' AND porcentaje IS NULL AND valor_especifico IS NOT NULL) OR (tipo_calculo = 'MIXTO' AND porcentaje IS NOT NULL AND valor_especifico IS NOT NULL)");
                    table.CheckConstraint("ck_tarifas_impuesto_vigencia", "vigente_hasta IS NULL OR vigente_hasta >= vigente_desde");
                    table.ForeignKey(
                        name: "FK_tarifas_impuesto_impuestos_impuesto_id",
                        column: x => x.impuesto_id,
                        principalSchema: "s_catalogos",
                        principalTable: "impuestos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "empresas",
                schema: "s_configuracion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    regimen_tributario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    razon_social = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    nombre_comercial = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    obligado_contabilidad = table.Column<bool>(type: "boolean", nullable: false),
                    contribuyente_especial_numero = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    telefono = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas", x => x.id);
                    table.CheckConstraint("ck_empresas_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_empresas_regimenes_tributarios_regimen_tributario_id",
                        column: x => x.regimen_tributario_id,
                        principalSchema: "s_catalogos",
                        principalTable: "regimenes_tributarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roles_permisos",
                schema: "s_seguridad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rol_id = table.Column<long>(type: "bigint", nullable: false),
                    permiso_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles_permisos", x => x.id);
                    table.ForeignKey(
                        name: "FK_roles_permisos_permisos_permiso_id",
                        column: x => x.permiso_id,
                        principalSchema: "s_seguridad",
                        principalTable: "permisos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_roles_permisos_roles_rol_id",
                        column: x => x.rol_id,
                        principalSchema: "s_seguridad",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "terceros",
                schema: "s_comercial",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_identificacion_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    clave_identidad = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    razon_social = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    nombre_comercial = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    telefono = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    origen_registro = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    estado_verificacion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    fuente_verificacion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    verificado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    es_cliente = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    estado_cliente = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    es_proveedor = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    estado_proveedor = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terceros", x => x.id);
                    table.CheckConstraint("ck_terceros_consumidor_final_protegido", "numero_identificacion <> '9999999999999' OR (razon_social = 'CONSUMIDOR FINAL' AND es_cliente AND estado_cliente = 1 AND estado = 1)");
                    table.CheckConstraint("ck_terceros_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_terceros_estado_cliente", "estado_cliente IN (0, 1)");
                    table.CheckConstraint("ck_terceros_estado_proveedor", "estado_proveedor IN (0, 1)");
                    table.CheckConstraint("ck_terceros_estado_verificacion", "estado_verificacion IN ('PENDIENTE', 'VERIFICADO')");
                    table.CheckConstraint("ck_terceros_origen_registro", "origen_registro IN ('OFICIAL', 'OFFLINE')");
                    table.ForeignKey(
                        name: "FK_terceros_tipos_identificacion_tipo_identificacion_id",
                        column: x => x.tipo_identificacion_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_identificacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categorias_productos",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    categoria_padre_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_productos", x => x.id);
                    table.UniqueConstraint("ak_categorias_productos_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_categorias_productos_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_categorias_productos_categorias_productos_categoria_padre_id",
                        column: x => x.categoria_padre_id,
                        principalSchema: "s_catalogos",
                        principalTable: "categorias_productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_categorias_productos_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "establecimientos",
                schema: "s_configuracion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    prefijo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nombre_comercial = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    es_matriz = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_establecimientos", x => x.id);
                    table.UniqueConstraint("ak_establecimientos_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_establecimientos_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_establecimientos_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "facturacion_electronica",
                schema: "s_configuracion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_ambiente_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_emision_id = table.Column<long>(type: "bigint", nullable: false),
                    certificado_nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    certificado_referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    certificado_titular = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    certificado_emisor = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    certificado_numero_serie = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    certificado_fecha_inicio = table.Column<DateOnly>(type: "date", nullable: true),
                    certificado_fecha_caducidad = table.Column<DateOnly>(type: "date", nullable: true),
                    habilitada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facturacion_electronica", x => x.id);
                    table.ForeignKey(
                        name: "FK_facturacion_electronica_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturacion_electronica_tipos_ambiente_tipo_ambiente_id",
                        column: x => x.tipo_ambiente_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_ambiente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturacion_electronica_tipos_emision_tipo_emision_id",
                        column: x => x.tipo_emision_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_emision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "listas_precio",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    es_lista_base = table.Column<bool>(type: "boolean", nullable: false),
                    porcentaje_descuento_predeterminado = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listas_precio", x => x.id);
                    table.UniqueConstraint("ak_listas_precio_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_listas_precio_descuento", "porcentaje_descuento_predeterminado IS NULL OR porcentaje_descuento_predeterminado BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_listas_precio_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_listas_precio_orden", "orden >= 0");
                    table.ForeignKey(
                        name: "FK_listas_precio_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "motivos_operacion_inventario",
                schema: "s_catalogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    nombre = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tipo_operacion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    es_sistema = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motivos_operacion_inventario", x => x.id);
                    table.CheckConstraint("ck_motivos_operacion_inventario_estado", "estado IN (0,1)");
                    table.CheckConstraint("ck_motivos_operacion_inventario_sistema", "(es_sistema AND empresa_id IS NULL) OR (NOT es_sistema AND empresa_id IS NOT NULL)");
                    table.CheckConstraint("ck_motivos_operacion_inventario_tipo", "tipo_operacion IN ('INVENTARIO_INICIAL_ADICIONAL','AJUSTE_ENTRADA','AJUSTE_SALIDA','CONVERSION_CONTROL','CORRECCION_LOTE_SERIE')");
                    table.ForeignKey(
                        name: "FK_motivos_operacion_inventario_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "periodos_contables",
                schema: "s_contabilidad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    mes = table.Column<int>(type: "integer", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    cerrado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    cerrado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periodos_contables", x => x.id);
                    table.UniqueConstraint("ak_periodos_contables_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_periodos_contables_cierre", "(estado = 'ABIERTO' AND cerrado_por_usuario_id IS NULL AND cerrado_at IS NULL) OR (estado = 'CERRADO' AND cerrado_por_usuario_id IS NOT NULL AND cerrado_at IS NOT NULL)");
                    table.CheckConstraint("ck_periodos_contables_estado", "estado IN ('ABIERTO', 'CERRADO')");
                    table.CheckConstraint("ck_periodos_contables_fechas", "fecha_fin >= fecha_inicio");
                    table.CheckConstraint("ck_periodos_contables_mes", "mes BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_periodos_contables_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_periodos_contables_usuarios_cerrado_por_usuario_id",
                        column: x => x.cerrado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plan_cuentas",
                schema: "s_contabilidad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    cuenta_padre_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    naturaleza = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    acepta_movimientos = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_cuentas", x => x.id);
                    table.UniqueConstraint("ak_plan_cuentas_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_plan_cuentas_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_plan_cuentas_naturaleza", "naturaleza IN ('DEUDORA', 'ACREEDORA')");
                    table.CheckConstraint("ck_plan_cuentas_padre", "cuenta_padre_id IS NULL OR cuenta_padre_id <> id");
                    table.ForeignKey(
                        name: "FK_plan_cuentas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plan_cuentas_plan_cuentas_cuenta_padre_id_empresa_id",
                        columns: x => new { x.cuenta_padre_id, x.empresa_id },
                        principalSchema: "s_contabilidad",
                        principalTable: "plan_cuentas",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "secuenciales_asientos",
                schema: "s_contabilidad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    ultimo_secuencial = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_secuenciales_asientos", x => x.id);
                    table.CheckConstraint("ck_secuenciales_asientos_valor", "ultimo_secuencial >= 0");
                    table.ForeignKey(
                        name: "FK_secuenciales_asientos_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios_empresas",
                schema: "s_seguridad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_empresas", x => x.id);
                    table.CheckConstraint("ck_usuarios_empresas_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_usuarios_empresas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_empresas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documentos_recibidos_sri",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    tercero_id = table.Column<long>(type: "bigint", nullable: true),
                    tipo_comprobante_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_documento = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    clave_acceso = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: false),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_autorizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ambiente = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tipo_emision = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ruc_emisor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    razon_social_emisor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    nombre_comercial_emisor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    direccion_matriz = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    direccion_establecimiento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    establecimiento_codigo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    punto_emision_codigo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    secuencial = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    identificacion_receptor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    razon_social_receptor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    valor_sin_impuestos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    iva = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    propina = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    importe_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    firma_presente = table.Column<bool>(type: "boolean", nullable: false),
                    estado_validacion = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    archivo_ruta_relativa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    archivo_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    archivo_tamano = table.Column<long>(type: "bigint", nullable: false),
                    numero_documento_modificado = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    clasificacion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    estado_procesamiento = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    xml_obtenido_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos_recibidos_sri", x => x.id);
                    table.UniqueConstraint("ak_documentos_recibidos_sri_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_documentos_recibidos_sri_archivo", "archivo_tamano > 0 AND length(archivo_sha256) = 64");
                    table.CheckConstraint("ck_documentos_recibidos_sri_clasificacion", "clasificacion IS NULL OR clasificacion IN ('INVENTARIO', 'GASTO', 'ACTIVO', 'OTRO')");
                    table.CheckConstraint("ck_documentos_recibidos_sri_estado", "estado_procesamiento IN ('PENDIENTE', 'PROCESADO', 'NO_APLICA')");
                    table.CheckConstraint("ck_documentos_recibidos_sri_validacion", "estado_validacion IN ('AUTORIZADO_SRI', 'VALIDADO_LOCALMENTE', 'ADVERTENCIA', 'RECHAZADO')");
                    table.ForeignKey(
                        name: "FK_documentos_recibidos_sri_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documentos_recibidos_sri_terceros_tercero_id",
                        column: x => x.tercero_id,
                        principalSchema: "s_comercial",
                        principalTable: "terceros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documentos_recibidos_sri_tipos_comprobante_tipo_comprobante~",
                        column: x => x.tipo_comprobante_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_comprobante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "terceros_identificaciones",
                schema: "s_comercial",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_identificacion_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    numero_normalizado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    es_principal = table.Column<bool>(type: "boolean", nullable: false),
                    estado_verificacion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    fuente_verificacion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    verificado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terceros_identificaciones", x => x.id);
                    table.CheckConstraint("ck_terceros_identificaciones_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_terceros_identificaciones_verificacion", "estado_verificacion IN ('PENDIENTE', 'VERIFICADO')");
                    table.ForeignKey(
                        name: "FK_terceros_identificaciones_terceros_tercero_id",
                        column: x => x.tercero_id,
                        principalSchema: "s_comercial",
                        principalTable: "terceros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_terceros_identificaciones_tipos_identificacion_tipo_identif~",
                        column: x => x.tipo_identificacion_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_identificacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    categoria_producto_id = table.Column<long>(type: "bigint", nullable: true),
                    marca_id = table.Column<long>(type: "bigint", nullable: true),
                    unidad_medida_base_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    modelo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    tipo_producto = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    maneja_inventario = table.Column<bool>(type: "boolean", nullable: false),
                    maneja_lotes = table.Column<bool>(type: "boolean", nullable: false),
                    maneja_series = table.Column<bool>(type: "boolean", nullable: false),
                    maneja_fecha_caducidad = table.Column<bool>(type: "boolean", nullable: false),
                    alerta_caducidad = table.Column<bool>(type: "boolean", nullable: false),
                    dias_alerta_caducidad = table.Column<int>(type: "integer", nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos", x => x.id);
                    table.UniqueConstraint("ak_productos_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_productos_alerta_caducidad", "dias_alerta_caducidad IS NULL OR dias_alerta_caducidad >= 0");
                    table.CheckConstraint("ck_productos_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_productos_tipo", "tipo_producto IN ('PRODUCTO', 'SERVICIO')");
                    table.ForeignKey(
                        name: "FK_productos_categorias_productos_categoria_producto_id_empres~",
                        columns: x => new { x.categoria_producto_id, x.empresa_id },
                        principalSchema: "s_catalogos",
                        principalTable: "categorias_productos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_marcas_marca_id",
                        column: x => x.marca_id,
                        principalSchema: "s_catalogos",
                        principalTable: "marcas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_unidades_medida_unidad_medida_base_id",
                        column: x => x.unidad_medida_base_id,
                        principalSchema: "s_catalogos",
                        principalTable: "unidades_medida",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auditoria",
                schema: "s_seguridad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    empresa_id = table.Column<long>(type: "bigint", nullable: true),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: true),
                    instalacion_uuid = table.Column<Guid>(type: "uuid", nullable: true),
                    nombre_equipo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ip_equipo = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    accion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    entidad = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    entidad_id = table.Column<long>(type: "bigint", nullable: true),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria", x => x.id);
                    table.ForeignKey(
                        name: "FK_auditoria_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_auditoria_establecimientos_establecimiento_id",
                        column: x => x.establecimiento_id,
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_auditoria_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bodegas",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    permite_transferencias_internas = table.Column<bool>(type: "boolean", nullable: false),
                    permite_venta_facturada = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bodegas", x => x.id);
                    table.UniqueConstraint("ak_bodegas_id_establecimiento", x => new { x.id, x.establecimiento_id });
                    table.CheckConstraint("ck_bodegas_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_bodegas_establecimientos_establecimiento_id",
                        column: x => x.establecimiento_id,
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "puntos_emision",
                schema: "s_configuracion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_puntos_emision", x => x.id);
                    table.UniqueConstraint("ak_puntos_emision_id_establecimiento", x => new { x.id, x.establecimiento_id });
                    table.CheckConstraint("ck_puntos_emision_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_puntos_emision_establecimientos_establecimiento_id",
                        column: x => x.establecimiento_id,
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "secuenciales_internos",
                schema: "s_configuracion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_documento_interno_id = table.Column<long>(type: "bigint", nullable: false),
                    ultimo_secuencial = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_secuenciales_internos", x => x.id);
                    table.CheckConstraint("ck_secuenciales_internos_no_negativo", "ultimo_secuencial >= 0");
                    table.ForeignKey(
                        name: "FK_secuenciales_internos_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_secuenciales_internos_establecimientos_establecimiento_id_e~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_secuenciales_internos_tipos_documento_interno_tipo_document~",
                        column: x => x.tipo_documento_interno_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_documento_interno",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "empresas_terceros",
                schema: "s_comercial",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    lista_precio_id = table.Column<long>(type: "bigint", nullable: true),
                    credito_habilitado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cupo_credito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    dias_credito = table.Column<int>(type: "integer", nullable: true),
                    motivo_bloqueo_credito = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas_terceros", x => x.id);
                    table.UniqueConstraint("ak_empresas_terceros_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_empresas_terceros_cupo_credito", "cupo_credito IS NULL OR cupo_credito >= 0");
                    table.CheckConstraint("ck_empresas_terceros_dias_credito", "dias_credito IS NULL OR dias_credito >= 0");
                    table.CheckConstraint("ck_empresas_terceros_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_empresas_terceros_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_empresas_terceros_listas_precio_lista_precio_id_empresa_id",
                        columns: x => new { x.lista_precio_id, x.empresa_id },
                        principalSchema: "s_inventario",
                        principalTable: "listas_precio",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_empresas_terceros_terceros_tercero_id",
                        column: x => x.tercero_id,
                        principalSchema: "s_comercial",
                        principalTable: "terceros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asientos",
                schema: "s_contabilidad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    periodo_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_asiento = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo_asiento = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tipo_origen_asiento_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_id = table.Column<long>(type: "bigint", nullable: true),
                    asiento_origen_reversado_id = table.Column<long>(type: "bigint", nullable: true),
                    concepto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    estado = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asientos", x => x.id);
                    table.UniqueConstraint("ak_asientos_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_asientos_estado", "estado IN ('CONTABILIZADO', 'ANULADO')");
                    table.CheckConstraint("ck_asientos_numero", "numero_asiento ~ '^ASI-[0-9]{4}-[0-9]{6,}$'");
                    table.CheckConstraint("ck_asientos_tipo", "tipo_asiento IN ('AUTOMATICO', 'MANUAL')");
                    table.ForeignKey(
                        name: "FK_asientos_asientos_asiento_origen_reversado_id",
                        column: x => x.asiento_origen_reversado_id,
                        principalSchema: "s_contabilidad",
                        principalTable: "asientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asientos_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asientos_periodos_contables_periodo_id_empresa_id",
                        columns: x => new { x.periodo_id, x.empresa_id },
                        principalSchema: "s_contabilidad",
                        principalTable: "periodos_contables",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asientos_tipos_origen_asiento_tipo_origen_asiento_id",
                        column: x => x.tipo_origen_asiento_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_origen_asiento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asientos_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asientos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cajas",
                schema: "s_tesoreria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cuenta_contable_id = table.Column<long>(type: "bigint", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cajas", x => x.id);
                    table.UniqueConstraint("ak_cajas_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_cajas_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_cajas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cajas_establecimientos_establecimiento_id_empresa_id",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cajas_plan_cuentas_cuenta_contable_id_empresa_id",
                        columns: x => new { x.cuenta_contable_id, x.empresa_id },
                        principalSchema: "s_contabilidad",
                        principalTable: "plan_cuentas",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "configuracion_cuentas",
                schema: "s_contabilidad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_configuracion_contable_id = table.Column<long>(type: "bigint", nullable: false),
                    cuenta_contable_id = table.Column<long>(type: "bigint", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_cuentas", x => x.id);
                    table.CheckConstraint("ck_configuracion_cuentas_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_configuracion_cuentas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_configuracion_cuentas_plan_cuentas_cuenta_contable_id_empre~",
                        columns: x => new { x.cuenta_contable_id, x.empresa_id },
                        principalSchema: "s_contabilidad",
                        principalTable: "plan_cuentas",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_configuracion_cuentas_tipos_configuracion_contable_tipo_con~",
                        column: x => x.tipo_configuracion_contable_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_configuracion_contable",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_bancarias",
                schema: "s_bancos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    banco = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_cuenta = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    numero_cuenta = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cuenta_contable_id = table.Column<long>(type: "bigint", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_bancarias", x => x.id);
                    table.UniqueConstraint("ak_cuentas_bancarias_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_cuentas_bancarias_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_cuentas_bancarias_tipo", "tipo_cuenta IN ('CORRIENTE', 'AHORROS', 'OTRA')");
                    table.ForeignKey(
                        name: "FK_cuentas_bancarias_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_bancarias_plan_cuentas_cuenta_contable_id_empresa_id",
                        columns: x => new { x.cuenta_contable_id, x.empresa_id },
                        principalSchema: "s_contabilidad",
                        principalTable: "plan_cuentas",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios_empresas_establecimientos",
                schema: "s_seguridad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_empresas_establecimientos", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_empresas_establecimientos_establecimientos_estable~",
                        column: x => x.establecimiento_id,
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_empresas_establecimientos_usuarios_empresas_usuari~",
                        column: x => x.usuario_empresa_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios_empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios_empresas_roles",
                schema: "s_seguridad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    rol_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_empresas_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_empresas_roles_roles_rol_id",
                        column: x => x.rol_id,
                        principalSchema: "s_seguridad",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_empresas_roles_usuarios_empresas_usuario_empresa_id",
                        column: x => x.usuario_empresa_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios_empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documentos_recibidos_sri_pagos",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    documento_recibido_sri_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_forma_pago_sri = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    plazo = table.Column<int>(type: "integer", nullable: true),
                    unidad_tiempo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos_recibidos_sri_pagos", x => x.id);
                    table.CheckConstraint("ck_documentos_sri_pagos_plazo", "plazo IS NULL OR plazo >= 0");
                    table.CheckConstraint("ck_documentos_sri_pagos_valor", "valor > 0");
                    table.ForeignKey(
                        name: "FK_documentos_recibidos_sri_pagos_documentos_recibidos_sri_doc~",
                        column: x => x.documento_recibido_sri_id,
                        principalSchema: "s_compras",
                        principalTable: "documentos_recibidos_sri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversiones_control_inventario",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_control_anterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tipo_control_nuevo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_conversion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo_operacion_inventario_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversiones_control_inventario", x => x.id);
                    table.CheckConstraint("ck_conversion_control_motivo", "length(btrim(motivo)) > 0");
                    table.CheckConstraint("ck_conversion_control_tipos", "tipo_control_anterior IN ('NORMAL','LOTE','SERIE','LOTE_Y_SERIE') AND tipo_control_nuevo IN ('NORMAL','LOTE','SERIE','LOTE_Y_SERIE') AND tipo_control_anterior <> tipo_control_nuevo");
                    table.ForeignKey(
                        name: "FK_conversiones_control_inventario_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversiones_control_inventario_motivos_operacion_inventari~",
                        column: x => x.motivo_operacion_inventario_id,
                        principalSchema: "s_catalogos",
                        principalTable: "motivos_operacion_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversiones_control_inventario_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversiones_control_inventario_usuarios_anulado_por_usuari~",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversiones_control_inventario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "correcciones_datos_inventario",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_entidad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    entidad_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo_operacion_inventario_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    fecha_correccion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor_anterior = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    valor_nuevo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_correcciones_datos_inventario", x => x.id);
                    table.CheckConstraint("ck_correcciones_datos_inventario_tipo", "tipo_entidad IN ('LOTE','SERIE')");
                    table.ForeignKey(
                        name: "FK_correcciones_datos_inventario_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_correcciones_datos_inventario_motivos_operacion_inventario_~",
                        column: x => x.motivo_operacion_inventario_id,
                        principalSchema: "s_catalogos",
                        principalTable: "motivos_operacion_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_correcciones_datos_inventario_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_correcciones_datos_inventario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos_costos",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    ultimo_precio_compra = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    ultimo_costo_efectivo = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_promedio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos_costos", x => x.id);
                    table.CheckConstraint("ck_productos_costos_no_negativos", "ultimo_precio_compra >= 0 AND ultimo_costo_efectivo >= 0 AND costo_promedio >= 0");
                    table.ForeignKey(
                        name: "FK_productos_costos_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos_impuestos",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    tarifa_impuesto_id = table.Column<long>(type: "bigint", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos_impuestos", x => x.id);
                    table.CheckConstraint("ck_productos_impuestos_estado", "estado IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_productos_impuestos_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_impuestos_tarifas_impuesto_tarifa_impuesto_id",
                        column: x => x.tarifa_impuesto_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tarifas_impuesto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos_lotes",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_lote = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    fecha_elaboracion = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_caducidad = table.Column<DateOnly>(type: "date", nullable: true),
                    observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos_lotes", x => x.id);
                    table.UniqueConstraint("ak_productos_lotes_id_producto", x => new { x.id, x.producto_id });
                    table.CheckConstraint("ck_productos_lotes_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_productos_lotes_fechas", "fecha_elaboracion IS NULL OR fecha_caducidad IS NULL OR fecha_caducidad >= fecha_elaboracion");
                    table.ForeignKey(
                        name: "FK_productos_lotes_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos_presentaciones",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    codigo_barras = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    es_presentacion_base = table.Column<bool>(type: "boolean", nullable: false),
                    permite_compra = table.Column<bool>(type: "boolean", nullable: false),
                    permite_venta = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos_presentaciones", x => x.id);
                    table.UniqueConstraint("AK_productos_presentaciones_id_empresa_id", x => new { x.id, x.empresa_id });
                    table.UniqueConstraint("ak_productos_presentaciones_id_producto_empresa", x => new { x.id, x.producto_id, x.empresa_id });
                    table.CheckConstraint("ck_productos_presentaciones_base_factor", "NOT es_presentacion_base OR factor_conversion = 1");
                    table.CheckConstraint("ck_productos_presentaciones_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_productos_presentaciones_factor", "factor_conversion > 0");
                    table.ForeignKey(
                        name: "FK_productos_presentaciones_productos_producto_id_empresa_id",
                        columns: x => new { x.producto_id, x.empresa_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ajustes_inventario",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_ajuste = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tipo_ajuste = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    fecha_ajuste = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    motivo_operacion_inventario_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ajustes_inventario", x => x.id);
                    table.CheckConstraint("ck_ajustes_inventario_tipo", "tipo_ajuste IN ('ENTRADA', 'SALIDA')");
                    table.ForeignKey(
                        name: "FK_ajustes_inventario_bodegas_bodega_id_establecimiento_id",
                        columns: x => new { x.bodega_id, x.establecimiento_id },
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_inventario_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_inventario_establecimientos_establecimiento_id_empr~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_inventario_motivos_operacion_inventario_motivo_oper~",
                        column: x => x.motivo_operacion_inventario_id,
                        principalSchema: "s_catalogos",
                        principalTable: "motivos_operacion_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_inventario_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_inventario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_inventario",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_movimiento = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tipo_movimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    fecha_movimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_tipo_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_documento = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    movimiento_reverso_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_inventario", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_movimientos_inventario_movimiento_re~",
                        column: x => x.movimiento_reverso_id,
                        principalSchema: "s_inventario",
                        principalTable: "movimientos_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_tipos_movimiento_inventario_tipo_mov~",
                        column: x => x.tipo_movimiento_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_movimiento_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_tipos_origen_movimiento_inventario_o~",
                        column: x => x.origen_tipo_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_origen_movimiento_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos_existencias",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    stock_actual = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    stock_reservado = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    stock_minimo = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    ubicacion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos_existencias", x => x.id);
                    table.CheckConstraint("ck_productos_existencias_minimo", "stock_minimo >= 0");
                    table.CheckConstraint("ck_productos_existencias_reservado", "stock_reservado >= 0");
                    table.ForeignKey(
                        name: "FK_productos_existencias_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_existencias_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transferencias_inventario",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_origen_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_destino_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_origen_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_destino_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_transferencia = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_transferencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencias_inventario", x => x.id);
                    table.CheckConstraint("ck_transferencias_inventario_bodegas", "bodega_origen_id <> bodega_destino_id");
                    table.ForeignKey(
                        name: "FK_transferencias_inventario_bodegas_bodega_destino_id_estable~",
                        columns: x => new { x.bodega_destino_id, x.establecimiento_destino_id },
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_inventario_bodegas_bodega_origen_id_establec~",
                        columns: x => new { x.bodega_origen_id, x.establecimiento_origen_id },
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_inventario_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_inventario_establecimientos_establecimiento_~",
                        columns: x => new { x.establecimiento_destino_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_inventario_establecimientos_establecimiento~1",
                        columns: x => new { x.establecimiento_origen_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_inventario_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_inventario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comprobantes_electronicos",
                schema: "s_facturacion_electronica",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_comprobante_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_origen_comprobante_electronico_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_ambiente_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_emision_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: true),
                    punto_emision_id = table.Column<long>(type: "bigint", nullable: true),
                    secuencial = table.Column<int>(type: "integer", nullable: true),
                    version_xml = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "2.1.0"),
                    clave_acceso = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: false),
                    estado_comprobante_electronico_id = table.Column<long>(type: "bigint", nullable: false),
                    procesamiento_iniciado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    procesado_por_instalacion_uuid = table.Column<Guid>(type: "uuid", nullable: true),
                    xml_generado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xml_firmado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_envio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_ultima_consulta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    intentos_envio = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    intentos_autorizacion = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    estado_recepcion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    estado_autorizacion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    xml_generado_referencia = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    xml_firmado_referencia = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    xml_autorizado_referencia = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    fecha_autorizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    numero_autorizacion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    xml_autorizado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ride_generado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comprobantes_electronicos", x => x.id);
                    table.CheckConstraint("ck_comprobantes_electronicos_intentos", "intentos_envio >= 0 AND intentos_autorizacion >= 0");
                    table.CheckConstraint("ck_comprobantes_electronicos_secuencial", "secuencial IS NULL OR secuencial BETWEEN 1 AND 999999999");
                    table.ForeignKey(
                        name: "FK_comprobantes_electronicos_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_electronicos_establecimientos_establecimiento_~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_electronicos_estados_comprobante_electronico_e~",
                        column: x => x.estado_comprobante_electronico_id,
                        principalSchema: "s_catalogos",
                        principalTable: "estados_comprobante_electronico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_electronicos_puntos_emision_punto_emision_id_e~",
                        columns: x => new { x.punto_emision_id, x.establecimiento_id },
                        principalSchema: "s_configuracion",
                        principalTable: "puntos_emision",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_electronicos_tipos_ambiente_tipo_ambiente_id",
                        column: x => x.tipo_ambiente_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_ambiente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_electronicos_tipos_comprobante_tipo_comprobant~",
                        column: x => x.tipo_comprobante_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_comprobante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_electronicos_tipos_emision_tipo_emision_id",
                        column: x => x.tipo_emision_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_emision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_electronicos_tipos_origen_comprobante_electron~",
                        column: x => x.tipo_origen_comprobante_electronico_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_origen_comprobante_electronico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "secuenciales_comprobantes",
                schema: "s_configuracion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    punto_emision_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_comprobante_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_ambiente_id = table.Column<long>(type: "bigint", nullable: false),
                    ultimo_secuencial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_secuenciales_comprobantes", x => x.id);
                    table.CheckConstraint("ck_secuenciales_comprobantes_rango", "ultimo_secuencial BETWEEN 0 AND 999999999");
                    table.ForeignKey(
                        name: "FK_secuenciales_comprobantes_puntos_emision_punto_emision_id",
                        column: x => x.punto_emision_id,
                        principalSchema: "s_configuracion",
                        principalTable: "puntos_emision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_secuenciales_comprobantes_tipos_ambiente_tipo_ambiente_id",
                        column: x => x.tipo_ambiente_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_ambiente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_secuenciales_comprobantes_tipos_comprobante_tipo_comprobant~",
                        column: x => x.tipo_comprobante_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_comprobante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios_configuracion_empresa",
                schema: "s_configuracion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    punto_emision_id = table.Column<long>(type: "bigint", nullable: true),
                    bodega_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_configuracion_empresa", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_configuracion_empresa_bodegas_bodega_id_establecim~",
                        columns: x => new { x.bodega_id, x.establecimiento_id },
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_configuracion_empresa_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_configuracion_empresa_establecimientos_establecimi~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_configuracion_empresa_puntos_emision_punto_emision~",
                        columns: x => new { x.punto_emision_id, x.establecimiento_id },
                        principalSchema: "s_configuracion",
                        principalTable: "puntos_emision",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_configuracion_empresa_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cobros",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_cobro = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_cobro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobros", x => x.id);
                    table.CheckConstraint("ck_cobros_valor", "valor_total > 0");
                    table.ForeignKey(
                        name: "FK_cobros_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cobros_empresas_terceros_empresa_tercero_id_empresa_id",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cobros_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cobros_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    documento_recibido_sri_id = table.Column<long>(type: "bigint", nullable: true),
                    compra_sustituida_id = table.Column<long>(type: "bigint", nullable: true),
                    tipo_compra = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tipo_comprobante_id = table.Column<long>(type: "bigint", nullable: true),
                    numero_documento = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    proveedor_identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    proveedor_razon_social = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_ingreso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    subtotal_sin_impuestos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuesto_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    es_credito = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras", x => x.id);
                    table.UniqueConstraint("ak_compras_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_compras_documento", "(tipo_compra = 'FACTURADA' AND tipo_comprobante_id IS NOT NULL AND numero_documento IS NOT NULL)");
                    table.CheckConstraint("ck_compras_estado", "estado IN ('BORRADOR', 'PENDIENTE_RECEPCION', 'PARCIALMENTE_RECIBIDA', 'RECIBIDA', 'ANULADA')");
                    table.CheckConstraint("ck_compras_tipo", "tipo_compra = 'FACTURADA'");
                    table.ForeignKey(
                        name: "FK_compras_compras_compra_sustituida_id_empresa_id",
                        columns: x => new { x.compra_sustituida_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "compras",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_documentos_recibidos_sri_documento_recibido_sri_id_~",
                        columns: x => new { x.documento_recibido_sri_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "documentos_recibidos_sri",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_empresas_terceros_empresa_tercero_id_empresa_id",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_establecimientos_establecimiento_id_empresa_id",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_tipos_comprobante_tipo_comprobante_id",
                        column: x => x.tipo_comprobante_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_comprobante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_por_cobrar",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_tipo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    origen_id = table.Column<long>(type: "bigint", nullable: false),
                    fecha_origen = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    valor_original = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_actual = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_por_cobrar", x => x.id);
                    table.CheckConstraint("ck_cuentas_por_cobrar_estado", "estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA', 'ANULADA')");
                    table.CheckConstraint("ck_cuentas_por_cobrar_saldo", "saldo_actual >= 0");
                    table.CheckConstraint("ck_cuentas_por_cobrar_valor", "valor_original > 0");
                    table.ForeignKey(
                        name: "FK_cuentas_por_cobrar_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_por_cobrar_empresas_terceros_empresa_tercero_id_emp~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_por_pagar",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_tipo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    origen_id = table.Column<long>(type: "bigint", nullable: false),
                    fecha_origen = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    valor_original = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_actual = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_por_pagar", x => x.id);
                    table.CheckConstraint("ck_cuentas_por_pagar_estado", "estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA', 'ANULADA')");
                    table.CheckConstraint("ck_cuentas_por_pagar_saldo", "saldo_actual >= 0");
                    table.CheckConstraint("ck_cuentas_por_pagar_valor", "valor_original > 0");
                    table.ForeignKey(
                        name: "FK_cuentas_por_pagar_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_por_pagar_empresas_terceros_empresa_tercero_id_empr~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "devoluciones_compras",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_origen_devolucion_compra_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_devolucion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_devolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devoluciones_compras", x => x.id);
                    table.UniqueConstraint("ak_devoluciones_compras_id_empresa", x => new { x.id, x.empresa_id });
                    table.ForeignKey(
                        name: "FK_devoluciones_compras_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_compras_empresas_terceros_empresa_tercero_id_e~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_compras_establecimientos_establecimiento_id_em~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_compras_tipos_origen_devolucion_compra_tipo_or~",
                        column: x => x.tipo_origen_devolucion_compra_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_origen_devolucion_compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_compras_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_compras_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "devoluciones_ventas",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_origen_devolucion_venta_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_devolucion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_devolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devoluciones_ventas", x => x.id);
                    table.ForeignKey(
                        name: "FK_devoluciones_ventas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_ventas_empresas_terceros_empresa_tercero_id_em~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_ventas_establecimientos_establecimiento_id_emp~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_ventas_tipos_origen_devolucion_venta_tipo_orig~",
                        column: x => x.tipo_origen_devolucion_venta_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_origen_devolucion_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_ventas_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_ventas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guias_remision",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    punto_emision_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: true),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_comprobante_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_documento = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    secuencial = table.Column<int>(type: "integer", nullable: false),
                    tipo_origen_guia_remision_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha_emision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_inicio_traslado = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin_traslado = table.Column<DateOnly>(type: "date", nullable: false),
                    motivo_traslado = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    direccion_partida = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    direccion_destino = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    transportista_tipo_identificacion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    transportista_numero_identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transportista_razon_social = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    placa = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guias_remision", x => x.id);
                    table.CheckConstraint("ck_guias_remision_fechas", "fecha_fin_traslado >= fecha_inicio_traslado");
                    table.ForeignKey(
                        name: "FK_guias_remision_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guias_remision_empresas_terceros_empresa_tercero_id_empresa~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guias_remision_establecimientos_establecimiento_id_empresa_~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guias_remision_puntos_emision_punto_emision_id_establecimie~",
                        columns: x => new { x.punto_emision_id, x.establecimiento_id },
                        principalSchema: "s_configuracion",
                        principalTable: "puntos_emision",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guias_remision_tipos_comprobante_tipo_comprobante_id",
                        column: x => x.tipo_comprobante_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_comprobante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guias_remision_tipos_origen_guia_remision_tipo_origen_guia_~",
                        column: x => x.tipo_origen_guia_remision_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_origen_guia_remision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guias_remision_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guias_remision_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "liquidaciones_compra",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    punto_emision_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_comprobante_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_documento = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    secuencial = table.Column<int>(type: "integer", nullable: false),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    subtotal_sin_impuestos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuesto_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_liquidaciones_compra", x => x.id);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_empresas_terceros_empresa_tercero_id_e~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_establecimientos_establecimiento_id_em~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_puntos_emision_punto_emision_id_establ~",
                        columns: x => new { x.punto_emision_id, x.establecimiento_id },
                        principalSchema: "s_configuracion",
                        principalTable: "puntos_emision",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_tipos_comprobante_tipo_comprobante_id",
                        column: x => x.tipo_comprobante_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_comprobante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_pago = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_pago = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos", x => x.id);
                    table.CheckConstraint("ck_pagos_valor", "valor_total > 0");
                    table.ForeignKey(
                        name: "FK_pagos_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_empresas_terceros_empresa_tercero_id_empresa_id",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proformas",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_proforma = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_emision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_vigencia = table.Column<DateOnly>(type: "date", nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuesto_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proformas", x => x.id);
                    table.CheckConstraint("ck_proformas_estado", "estado IN ('ABIERTA', 'CONVERTIDA', 'ANULADA')");
                    table.ForeignKey(
                        name: "FK_proformas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proformas_empresas_terceros_empresa_tercero_id_empresa_id",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proformas_establecimientos_establecimiento_id_empresa_id",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proformas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retenciones_emitidas",
                schema: "s_tributacion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    punto_emision_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_comprobante_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_origen_retencion_emitida_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_documento = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    secuencial = table.Column<int>(type: "integer", nullable: false),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    periodo_fiscal = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    total_retenido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retenciones_emitidas", x => x.id);
                    table.CheckConstraint("ck_retenciones_emitidas_periodo", "periodo_fiscal ~ '^(0[1-9]|1[0-2])/[0-9]{4}$'");
                    table.CheckConstraint("ck_retenciones_emitidas_total", "total_retenido > 0");
                    table.ForeignKey(
                        name: "FK_retenciones_emitidas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_emitidas_empresas_terceros_empresa_tercero_id_e~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_emitidas_establecimientos_establecimiento_id_em~",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_emitidas_puntos_emision_punto_emision_id_establ~",
                        columns: x => new { x.punto_emision_id, x.establecimiento_id },
                        principalSchema: "s_configuracion",
                        principalTable: "puntos_emision",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_emitidas_tipos_comprobante_tipo_comprobante_id",
                        column: x => x.tipo_comprobante_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_comprobante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_emitidas_tipos_origen_retencion_emitida_tipo_or~",
                        column: x => x.tipo_origen_retencion_emitida_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_origen_retencion_emitida",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_emitidas_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_emitidas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retenciones_recibidas",
                schema: "s_tributacion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    documento_recibido_sri_id = table.Column<long>(type: "bigint", nullable: true),
                    numero_documento = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    clave_acceso = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: true),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    periodo_fiscal = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    total_retenido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retenciones_recibidas", x => x.id);
                    table.CheckConstraint("ck_retenciones_recibidas_periodo", "periodo_fiscal ~ '^(0[1-9]|1[0-2])/[0-9]{4}$'");
                    table.CheckConstraint("ck_retenciones_recibidas_total", "total_retenido > 0");
                    table.ForeignKey(
                        name: "FK_retenciones_recibidas_documentos_recibidos_sri_documento_re~",
                        columns: x => new { x.documento_recibido_sri_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "documentos_recibidos_sri",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_recibidas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_recibidas_empresas_terceros_empresa_tercero_id_~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ventas_xf",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero_xf = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_venta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_xf", x => x.id);
                    table.ForeignKey(
                        name: "FK_ventas_xf_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_xf_empresas_terceros_empresa_tercero_id_empresa_id",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_xf_establecimientos_establecimiento_id_empresa_id",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_xf_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_xf_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asientos_detalles",
                schema: "s_contabilidad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asiento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    cuenta_contable_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    debe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    haber = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asientos_detalles", x => x.id);
                    table.CheckConstraint("ck_asientos_detalles_debe_haber", "(debe > 0 AND haber = 0) OR (haber > 0 AND debe = 0)");
                    table.CheckConstraint("ck_asientos_detalles_orden", "orden > 0");
                    table.ForeignKey(
                        name: "FK_asientos_detalles_asientos_asiento_id_empresa_id",
                        columns: x => new { x.asiento_id, x.empresa_id },
                        principalSchema: "s_contabilidad",
                        principalTable: "asientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asientos_detalles_empresas_terceros_empresa_tercero_id_empr~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asientos_detalles_plan_cuentas_cuenta_contable_id_empresa_id",
                        columns: x => new { x.cuenta_contable_id, x.empresa_id },
                        principalSchema: "s_contabilidad",
                        principalTable: "plan_cuentas",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cajas_sesiones",
                schema: "s_tesoreria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    caja_id = table.Column<long>(type: "bigint", nullable: false),
                    abierta_por_usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    fecha_apertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    saldo_inicial = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cerrada_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha_cierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    saldo_sistema = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    saldo_contado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    diferencia = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    estado = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cajas_sesiones", x => x.id);
                    table.CheckConstraint("ck_cajas_sesiones_cierre", "(estado = 'ABIERTA' AND cerrada_por_usuario_id IS NULL AND fecha_cierre IS NULL) OR (estado = 'CERRADA' AND cerrada_por_usuario_id IS NOT NULL AND fecha_cierre IS NOT NULL)");
                    table.CheckConstraint("ck_cajas_sesiones_estado", "estado IN ('ABIERTA', 'CERRADA')");
                    table.CheckConstraint("ck_cajas_sesiones_saldo_inicial", "saldo_inicial >= 0");
                    table.ForeignKey(
                        name: "FK_cajas_sesiones_cajas_caja_id",
                        column: x => x.caja_id,
                        principalSchema: "s_tesoreria",
                        principalTable: "cajas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cajas_sesiones_usuarios_abierta_por_usuario_id",
                        column: x => x.abierta_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cajas_sesiones_usuarios_cerrada_por_usuario_id",
                        column: x => x.cerrada_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transferencias_bancarias",
                schema: "s_bancos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    cuenta_bancaria_origen_id = table.Column<long>(type: "bigint", nullable: false),
                    cuenta_bancaria_destino_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_transferencia = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_transferencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencias_bancarias", x => x.id);
                    table.CheckConstraint("ck_transferencias_bancarias_cuentas", "cuenta_bancaria_origen_id <> cuenta_bancaria_destino_id");
                    table.CheckConstraint("ck_transferencias_bancarias_valor", "valor > 0");
                    table.ForeignKey(
                        name: "FK_transferencias_bancarias_cuentas_bancarias_cuenta_bancaria_~",
                        columns: x => new { x.cuenta_bancaria_destino_id, x.empresa_id },
                        principalSchema: "s_bancos",
                        principalTable: "cuentas_bancarias",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_bancarias_cuentas_bancarias_cuenta_bancaria~1",
                        columns: x => new { x.cuenta_bancaria_origen_id, x.empresa_id },
                        principalSchema: "s_bancos",
                        principalTable: "cuentas_bancarias",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_bancarias_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_bancarias_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_bancarias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversiones_control_inventario_detalles",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conversion_control_inventario_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_lote_id = table.Column<long>(type: "bigint", nullable: true),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversiones_control_inventario_detalles", x => x.id);
                    table.CheckConstraint("ck_conversion_control_detalle_cantidad", "cantidad_base >= 0");
                    table.ForeignKey(
                        name: "FK_conversiones_control_inventario_detalles_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversiones_control_inventario_detalles_conversiones_contr~",
                        column: x => x.conversion_control_inventario_id,
                        principalSchema: "s_inventario",
                        principalTable: "conversiones_control_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversiones_control_inventario_detalles_productos_lotes_pr~",
                        column: x => x.producto_lote_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos_lotes_existencias",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lote_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    stock_actual = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    stock_reservado = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    ubicacion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos_lotes_existencias", x => x.id);
                    table.CheckConstraint("ck_productos_lotes_existencias_reservado", "stock_reservado >= 0");
                    table.ForeignKey(
                        name: "FK_productos_lotes_existencias_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_lotes_existencias_productos_lotes_lote_id",
                        column: x => x.lote_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos_series",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_lote_id = table.Column<long>(type: "bigint", nullable: true),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_serie = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    estado_serie_id = table.Column<long>(type: "bigint", nullable: false),
                    ubicacion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos_series", x => x.id);
                    table.UniqueConstraint("ak_productos_series_id_producto_bodega", x => new { x.id, x.producto_id, x.bodega_id });
                    table.ForeignKey(
                        name: "FK_productos_series_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_series_estados_serie_estado_serie_id",
                        column: x => x.estado_serie_id,
                        principalSchema: "s_catalogos",
                        principalTable: "estados_serie",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_series_productos_lotes_producto_lote_id_producto_~",
                        columns: x => new { x.producto_lote_id, x.producto_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos_lotes",
                        principalColumns: new[] { "id", "producto_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_series_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos_presentaciones_precios",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    lista_precio_id = table.Column<long>(type: "bigint", nullable: false),
                    metodo_calculo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    precio = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos_presentaciones_precios", x => x.id);
                    table.CheckConstraint("ck_productos_presentaciones_precios_estado", "estado IN (0, 1)");
                    table.CheckConstraint("ck_productos_presentaciones_precios_metodo", "metodo_calculo IN ('PORCENTAJE_COSTO', 'PRECIO_FIJO', 'DESCUENTO_PORCENTAJE')");
                    table.CheckConstraint("ck_productos_presentaciones_precios_no_negativo", "(porcentaje IS NULL OR porcentaje >= 0) AND (precio IS NULL OR precio >= 0)");
                    table.CheckConstraint("ck_productos_presentaciones_precios_valores", "(metodo_calculo IN ('PORCENTAJE_COSTO', 'DESCUENTO_PORCENTAJE') AND porcentaje IS NOT NULL AND precio IS NULL) OR (metodo_calculo = 'PRECIO_FIJO' AND precio IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_productos_presentaciones_precios_listas_precio_lista_precio~",
                        column: x => x.lista_precio_id,
                        principalSchema: "s_inventario",
                        principalTable: "listas_precio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_presentaciones_precios_productos_presentaciones_p~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
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
                name: "ajustes_inventario_detalles",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ajuste_inventario_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ajustes_inventario_detalles", x => x.id);
                    table.CheckConstraint("ck_ajustes_inventario_detalles_cantidad", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.ForeignKey(
                        name: "FK_ajustes_inventario_detalles_ajustes_inventario_ajuste_inven~",
                        column: x => x.ajuste_inventario_id,
                        principalSchema: "s_inventario",
                        principalTable: "ajustes_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_inventario_detalles_productos_presentaciones_produc~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_inventario_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_inventario_detalles",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    movimiento_inventario_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    stock_anterior = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    stock_nuevo = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_promedio_anterior = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_promedio_nuevo = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    es_bonificacion = table.Column<bool>(type: "boolean", nullable: false),
                    observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_inventario_detalles", x => x.id);
                    table.CheckConstraint("ck_movimientos_inventario_detalles_cantidad", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.CheckConstraint("ck_movimientos_inventario_detalles_costos", "costo_unitario_base >= 0 AND costo_total >= 0");
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_detalles_movimientos_inventario_movi~",
                        column: x => x.movimiento_inventario_id,
                        principalSchema: "s_inventario",
                        principalTable: "movimientos_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_detalles_productos_presentaciones_pr~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transferencias_inventario_detalles",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transferencia_inventario_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencias_inventario_detalles", x => x.id);
                    table.CheckConstraint("ck_transferencias_inventario_detalles_cantidad", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.ForeignKey(
                        name: "FK_transferencias_inventario_detalles_productos_presentaciones~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_inventario_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_inventario_detalles_transferencias_inventari~",
                        column: x => x.transferencia_inventario_id,
                        principalSchema: "s_inventario",
                        principalTable: "transferencias_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comprobantes_electronicos_eventos",
                schema: "s_facturacion_electronica",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comprobante_electronico_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_evento = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    mensaje = table.Column<string>(type: "text", nullable: true),
                    informacion_adicional = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comprobantes_electronicos_eventos", x => x.id);
                    table.CheckConstraint("ck_comprobantes_electronicos_eventos_tipo", "tipo_evento IN ('GENERACION', 'FIRMA', 'ENVIO', 'RECEPCION', 'AUTORIZACION', 'REINTENTO', 'ERROR')");
                    table.ForeignKey(
                        name: "FK_comprobantes_electronicos_eventos_comprobantes_electronicos~",
                        column: x => x.comprobante_electronico_id,
                        principalSchema: "s_facturacion_electronica",
                        principalTable: "comprobantes_electronicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cobros_medios",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cobro_id = table.Column<long>(type: "bigint", nullable: false),
                    medio_pago_id = table.Column<long>(type: "bigint", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobros_medios", x => x.id);
                    table.CheckConstraint("ck_cobros_medios_valor", "valor > 0");
                    table.ForeignKey(
                        name: "FK_cobros_medios_cobros_cobro_id",
                        column: x => x.cobro_id,
                        principalSchema: "s_cartera",
                        principalTable: "cobros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cobros_medios_medios_pago_medio_pago_id",
                        column: x => x.medio_pago_id,
                        principalSchema: "s_catalogos",
                        principalTable: "medios_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras_detalles",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    compra_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    codigo_principal_proveedor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    codigo_auxiliar_proveedor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    estado_reconocimiento = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    es_inventariable = table.Column<bool>(type: "boolean", nullable: false),
                    clasificacion_contable = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    cuenta_contable_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: true),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_unitario_compra = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descuento_porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descuento_valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_total_sin_impuesto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_total_linea = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    es_bonificacion = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_detalles", x => x.id);
                    table.UniqueConstraint("ak_compras_detalles_id_compra_empresa", x => new { x.id, x.compra_id, x.empresa_id });
                    table.UniqueConstraint("ak_compras_detalles_id_empresa", x => new { x.id, x.empresa_id });
                    table.CheckConstraint("ck_compras_detalles_cantidades", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.CheckConstraint("ck_compras_detalles_clasificacion_contable", "clasificacion_contable IN ('INVENTARIO', 'GASTO', 'ACTIVO', 'OTRO')");
                    table.CheckConstraint("ck_compras_detalles_clasificacion_producto", "(es_inventariable AND clasificacion_contable = 'INVENTARIO') OR (NOT es_inventariable AND clasificacion_contable <> 'INVENTARIO')");
                    table.CheckConstraint("ck_compras_detalles_orden", "orden > 0");
                    table.CheckConstraint("ck_compras_detalles_producto", "(es_inventariable AND producto_id IS NOT NULL AND producto_presentacion_id IS NOT NULL AND estado_reconocimiento = 'RECONOCIDA') OR (NOT es_inventariable AND producto_id IS NULL AND producto_presentacion_id IS NULL AND estado_reconocimiento = 'NO_INVENTARIABLE') OR (estado_reconocimiento IN ('SUGERIDA', 'NO_RECONOCIDA'))");
                    table.CheckConstraint("ck_compras_detalles_reconocimiento", "estado_reconocimiento IN ('RECONOCIDA', 'SUGERIDA', 'NO_RECONOCIDA', 'NO_INVENTARIABLE')");
                    table.ForeignKey(
                        name: "FK_compras_detalles_compras_compra_id_empresa_id",
                        columns: x => new { x.compra_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "compras",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_detalles_plan_cuentas_cuenta_contable_id_empresa_id",
                        columns: x => new { x.cuenta_contable_id, x.empresa_id },
                        principalSchema: "s_contabilidad",
                        principalTable: "plan_cuentas",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_detalles_productos_presentaciones_producto_presenta~",
                        columns: x => new { x.producto_presentacion_id, x.producto_id, x.empresa_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumns: new[] { "id", "producto_id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_detalles_productos_producto_id_empresa_id",
                        columns: x => new { x.producto_id, x.empresa_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumns: new[] { "id", "empresa_id" },
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
                    anulada_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_recepciones", x => x.id);
                    table.UniqueConstraint("ak_compras_recepciones_id_compra_bodega_empresa", x => new { x.id, x.compra_id, x.bodega_id, x.empresa_id });
                    table.UniqueConstraint("ak_compras_recepciones_id_compra_empresa", x => new { x.id, x.compra_id, x.empresa_id });
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
                        name: "FK_compras_recepciones_usuarios_anulada_por_usuario_id",
                        column: x => x.anulada_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
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
                name: "cobros_aplicaciones",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cobro_id = table.Column<long>(type: "bigint", nullable: false),
                    cuenta_por_cobrar_id = table.Column<long>(type: "bigint", nullable: false),
                    valor_aplicado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_anterior = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_nuevo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobros_aplicaciones", x => x.id);
                    table.CheckConstraint("ck_cobros_aplicaciones_saldos", "saldo_anterior >= 0 AND saldo_nuevo >= 0 AND saldo_nuevo = saldo_anterior - valor_aplicado");
                    table.CheckConstraint("ck_cobros_aplicaciones_valor", "valor_aplicado > 0");
                    table.ForeignKey(
                        name: "FK_cobros_aplicaciones_cobros_cobro_id",
                        column: x => x.cobro_id,
                        principalSchema: "s_cartera",
                        principalTable: "cobros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cobros_aplicaciones_cuentas_por_cobrar_cuenta_por_cobrar_id",
                        column: x => x.cuenta_por_cobrar_id,
                        principalSchema: "s_cartera",
                        principalTable: "cuentas_por_cobrar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ajustes_compras",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    compra_id = table.Column<long>(type: "bigint", nullable: false),
                    documento_recibido_sri_id = table.Column<long>(type: "bigint", nullable: true),
                    devolucion_compra_id = table.Column<long>(type: "bigint", nullable: true),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_ajuste = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    valor_sin_impuestos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuesto_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ajustes_compras", x => x.id);
                    table.CheckConstraint("ck_ajustes_compras_tipo", "tipo_ajuste IN ('DEVOLUCION_MERCADERIA', 'DESCUENTO_POSTERIOR', 'CORRECCION_PRECIO', 'OTRO')");
                    table.ForeignKey(
                        name: "FK_ajustes_compras_compras_compra_id_empresa_id",
                        columns: x => new { x.compra_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "compras",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_compras_devoluciones_compras_devolucion_compra_id_e~",
                        columns: x => new { x.devolucion_compra_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "devoluciones_compras",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_compras_documentos_recibidos_sri_documento_recibido~",
                        columns: x => new { x.documento_recibido_sri_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "documentos_recibidos_sri",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_compras_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_compras_empresas_terceros_empresa_tercero_id_empres~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_compras_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "devoluciones_compras_detalles",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    devolucion_compra_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devoluciones_compras_detalles", x => x.id);
                    table.CheckConstraint("ck_devoluciones_compras_detalles_cantidad", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.ForeignKey(
                        name: "FK_devoluciones_compras_detalles_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_compras_detalles_devoluciones_compras_devoluci~",
                        column: x => x.devolucion_compra_id,
                        principalSchema: "s_compras",
                        principalTable: "devoluciones_compras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_compras_detalles_productos_presentaciones_prod~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_compras_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "devoluciones_ventas_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    devolucion_venta_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devoluciones_ventas_detalles", x => x.id);
                    table.CheckConstraint("ck_devoluciones_ventas_detalles_cantidad", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.ForeignKey(
                        name: "FK_devoluciones_ventas_detalles_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_ventas_detalles_devoluciones_ventas_devolucion~",
                        column: x => x.devolucion_venta_id,
                        principalSchema: "s_ventas",
                        principalTable: "devoluciones_ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_ventas_detalles_productos_presentaciones_produ~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_devoluciones_ventas_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guias_remision_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guia_remision_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guias_remision_detalles", x => x.id);
                    table.CheckConstraint("ck_guias_remision_detalles_cantidad", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.ForeignKey(
                        name: "FK_guias_remision_detalles_guias_remision_guia_remision_id",
                        column: x => x.guia_remision_id,
                        principalSchema: "s_ventas",
                        principalTable: "guias_remision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guias_remision_detalles_productos_presentaciones_producto_p~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guias_remision_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "liquidaciones_compra_detalles",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    liquidacion_compra_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: true),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: true),
                    bodega_id = table.Column<long>(type: "bigint", nullable: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descuento_porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descuento_valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_total_linea = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_liquidaciones_compra_detalles", x => x.id);
                    table.CheckConstraint("ck_liquidaciones_compra_detalles_cantidades", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.CheckConstraint("ck_liquidaciones_compra_detalles_producto", "(producto_presentacion_id IS NULL OR producto_id IS NOT NULL) AND (bodega_id IS NULL OR producto_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_detalles_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_detalles_liquidaciones_compra_liquidac~",
                        column: x => x.liquidacion_compra_id,
                        principalSchema: "s_compras",
                        principalTable: "liquidaciones_compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_detalles_productos_presentaciones_prod~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos_aplicaciones",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pago_id = table.Column<long>(type: "bigint", nullable: false),
                    cuenta_por_pagar_id = table.Column<long>(type: "bigint", nullable: false),
                    valor_aplicado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_anterior = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_nuevo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_aplicaciones", x => x.id);
                    table.CheckConstraint("ck_pagos_aplicaciones_saldos", "saldo_anterior >= 0 AND saldo_nuevo >= 0 AND saldo_nuevo = saldo_anterior - valor_aplicado");
                    table.CheckConstraint("ck_pagos_aplicaciones_valor", "valor_aplicado > 0");
                    table.ForeignKey(
                        name: "FK_pagos_aplicaciones_cuentas_por_pagar_cuenta_por_pagar_id",
                        column: x => x.cuenta_por_pagar_id,
                        principalSchema: "s_cartera",
                        principalTable: "cuentas_por_pagar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_aplicaciones_pagos_pago_id",
                        column: x => x.pago_id,
                        principalSchema: "s_cartera",
                        principalTable: "pagos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos_medios",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pago_id = table.Column<long>(type: "bigint", nullable: false),
                    medio_pago_id = table.Column<long>(type: "bigint", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_medios", x => x.id);
                    table.CheckConstraint("ck_pagos_medios_valor", "valor > 0");
                    table.ForeignKey(
                        name: "FK_pagos_medios_medios_pago_medio_pago_id",
                        column: x => x.medio_pago_id,
                        principalSchema: "s_catalogos",
                        principalTable: "medios_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_medios_pagos_pago_id",
                        column: x => x.pago_id,
                        principalSchema: "s_cartera",
                        principalTable: "pagos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "facturas",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    punto_emision_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_comprobante_id = table.Column<long>(type: "bigint", nullable: false),
                    proforma_id = table.Column<long>(type: "bigint", nullable: true),
                    numero_documento = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    secuencial = table.Column<int>(type: "integer", nullable: false),
                    fecha_emision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    origen_facturacion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subtotal_sin_impuestos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuesto_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    es_credito = table.Column<bool>(type: "boolean", nullable: false),
                    receptor_distinto_autorizado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    motivo_receptor_distinto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facturas", x => x.id);
                    table.CheckConstraint("ck_facturas_origen", "origen_facturacion IN ('DIRECTA', 'DESDE_NOTA_ENTREGA', 'DESDE_XF')");
                    table.ForeignKey(
                        name: "FK_facturas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_empresas_terceros_empresa_tercero_id_empresa_id",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_establecimientos_establecimiento_id_empresa_id",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_proformas_proforma_id",
                        column: x => x.proforma_id,
                        principalSchema: "s_ventas",
                        principalTable: "proformas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_puntos_emision_punto_emision_id_establecimiento_id",
                        columns: x => new { x.punto_emision_id, x.establecimiento_id },
                        principalSchema: "s_configuracion",
                        principalTable: "puntos_emision",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_tipos_comprobante_tipo_comprobante_id",
                        column: x => x.tipo_comprobante_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_comprobante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_usuarios_receptor_distinto_autorizado_por_usuario_~",
                        column: x => x.receptor_distinto_autorizado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notas_entrega",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proforma_id = table.Column<long>(type: "bigint", nullable: true),
                    numero_nota = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_emision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    es_credito = table.Column<bool>(type: "boolean", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_entrega", x => x.id);
                    table.ForeignKey(
                        name: "FK_notas_entrega_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_entrega_empresas_terceros_empresa_tercero_id_empresa_~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_entrega_establecimientos_establecimiento_id_empresa_id",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_entrega_proformas_proforma_id",
                        column: x => x.proforma_id,
                        principalSchema: "s_ventas",
                        principalTable: "proformas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_entrega_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_entrega_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proformas_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proforma_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_referencia = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descuento_valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_final_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_modificado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proformas_detalles", x => x.id);
                    table.CheckConstraint("ck_proformas_detalles_cantidades", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.ForeignKey(
                        name: "FK_proformas_detalles_productos_presentaciones_producto_presen~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proformas_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proformas_detalles_proformas_proforma_id",
                        column: x => x.proforma_id,
                        principalSchema: "s_ventas",
                        principalTable: "proformas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proformas_detalles_usuarios_precio_modificado_por_usuario_id",
                        column: x => x.precio_modificado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retenciones_emitidas_detalles",
                schema: "s_tributacion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    retencion_emitida_id = table.Column<long>(type: "bigint", nullable: false),
                    concepto_retencion_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_impuesto_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo_retencion_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    porcentaje_retencion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    valor_retenido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retenciones_emitidas_detalles", x => x.id);
                    table.CheckConstraint("ck_retenciones_emitidas_detalles_base", "base_imponible > 0");
                    table.CheckConstraint("ck_retenciones_emitidas_detalles_valores", "porcentaje_retencion >= 0 AND valor_retenido > 0");
                    table.ForeignKey(
                        name: "FK_retenciones_emitidas_detalles_conceptos_retencion_concepto_~",
                        column: x => x.concepto_retencion_id,
                        principalSchema: "s_catalogos",
                        principalTable: "conceptos_retencion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_emitidas_detalles_retenciones_emitidas_retencio~",
                        column: x => x.retencion_emitida_id,
                        principalSchema: "s_tributacion",
                        principalTable: "retenciones_emitidas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retenciones_recibidas_detalles",
                schema: "s_tributacion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    retencion_recibida_id = table.Column<long>(type: "bigint", nullable: false),
                    concepto_retencion_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo_impuesto_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo_retencion_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    porcentaje_retencion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    valor_retenido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retenciones_recibidas_detalles", x => x.id);
                    table.CheckConstraint("ck_retenciones_recibidas_detalles_base", "base_imponible > 0");
                    table.CheckConstraint("ck_retenciones_recibidas_detalles_valores", "porcentaje_retencion >= 0 AND valor_retenido > 0");
                    table.ForeignKey(
                        name: "FK_retenciones_recibidas_detalles_conceptos_retencion_concepto~",
                        column: x => x.concepto_retencion_id,
                        principalSchema: "s_catalogos",
                        principalTable: "conceptos_retencion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_recibidas_detalles_retenciones_recibidas_retenc~",
                        column: x => x.retencion_recibida_id,
                        principalSchema: "s_tributacion",
                        principalTable: "retenciones_recibidas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ventas_xf_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    venta_xf_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_facturable = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_referencia = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descuento_valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_final_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_modificado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_xf_detalles", x => x.id);
                    table.CheckConstraint("ck_ventas_xf_detalles_cantidades", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.ForeignKey(
                        name: "FK_ventas_xf_detalles_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_xf_detalles_productos_presentaciones_producto_presen~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_xf_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_xf_detalles_usuarios_precio_modificado_por_usuario_id",
                        column: x => x.precio_modificado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_xf_detalles_ventas_xf_venta_xf_id",
                        column: x => x.venta_xf_id,
                        principalSchema: "s_ventas",
                        principalTable: "ventas_xf",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "depositos_caja_banco",
                schema: "s_tesoreria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    caja_sesion_id = table.Column<long>(type: "bigint", nullable: false),
                    cuenta_bancaria_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_deposito = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_deposito = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_depositos_caja_banco", x => x.id);
                    table.CheckConstraint("ck_depositos_caja_banco_valor", "valor > 0");
                    table.ForeignKey(
                        name: "FK_depositos_caja_banco_cajas_sesiones_caja_sesion_id",
                        column: x => x.caja_sesion_id,
                        principalSchema: "s_tesoreria",
                        principalTable: "cajas_sesiones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_depositos_caja_banco_cuentas_bancarias_cuenta_bancaria_id_e~",
                        columns: x => new { x.cuenta_bancaria_id, x.empresa_id },
                        principalSchema: "s_bancos",
                        principalTable: "cuentas_bancarias",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_depositos_caja_banco_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_depositos_caja_banco_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_depositos_caja_banco_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversiones_control_inventario_series",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conversion_control_inventario_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_serie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversiones_control_inventario_series", x => x.id);
                    table.ForeignKey(
                        name: "FK_conversiones_control_inventario_series_conversiones_control~",
                        column: x => x.conversion_control_inventario_id,
                        principalSchema: "s_inventario",
                        principalTable: "conversiones_control_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversiones_control_inventario_series_productos_series_pro~",
                        column: x => x.producto_serie_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_inventario_detalles_lotes",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    movimiento_inventario_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_lote_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    stock_lote_anterior = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    stock_lote_nuevo = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_inventario_detalles_lotes", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_detalles_lotes_movimientos_inventari~",
                        column: x => x.movimiento_inventario_detalle_id,
                        principalSchema: "s_inventario",
                        principalTable: "movimientos_inventario_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_detalles_lotes_productos_lotes_produ~",
                        column: x => x.producto_lote_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_inventario_detalles_series",
                schema: "s_inventario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    movimiento_inventario_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_serie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_inventario_detalles_series", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_detalles_series_movimientos_inventar~",
                        column: x => x.movimiento_inventario_detalle_id,
                        principalSchema: "s_inventario",
                        principalTable: "movimientos_inventario_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_detalles_series_productos_series_pro~",
                        column: x => x.producto_serie_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras_detalles_impuestos",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    compra_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    tarifa_impuesto_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo_impuesto_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo_porcentaje_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre_impuesto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_calculo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    valor_especifico = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valor_impuesto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_detalles_impuestos", x => x.id);
                    table.ForeignKey(
                        name: "FK_compras_detalles_impuestos_compras_detalles_compra_detalle_~",
                        column: x => x.compra_detalle_id,
                        principalSchema: "s_compras",
                        principalTable: "compras_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_detalles_impuestos_tarifas_impuesto_tarifa_impuesto~",
                        column: x => x.tarifa_impuesto_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tarifas_impuesto",
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
                    compra_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    compra_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    movimiento_inventario_detalle_id = table.Column<long>(type: "bigint", nullable: true),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    es_bonificacion = table.Column<bool>(type: "boolean", nullable: false),
                    ultimo_precio_compra_anterior = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    ultimo_costo_efectivo_anterior = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_recepciones_detalles", x => x.id);
                    table.UniqueConstraint("ak_compras_recepciones_detalles_id_empresa", x => new { x.id, x.empresa_id });
                    table.UniqueConstraint("ak_compras_recepciones_detalles_id_producto_bodega_empresa", x => new { x.id, x.producto_id, x.bodega_id, x.empresa_id });
                    table.UniqueConstraint("ak_compras_recepciones_detalles_id_producto_empresa", x => new { x.id, x.producto_id, x.empresa_id });
                    table.CheckConstraint("ck_compras_recepciones_detalles_cantidad", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0 AND costo_unitario_base >= 0 AND costo_total >= 0");
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_compras_detalles_compra_detall~",
                        columns: x => new { x.compra_detalle_id, x.compra_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "compras_detalles",
                        principalColumns: new[] { "id", "compra_id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                        columns: x => new { x.compra_recepcion_id, x.compra_id, x.bodega_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "compras_recepciones",
                        principalColumns: new[] { "id", "compra_id", "bodega_id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_movimientos_inventario_detalle~",
                        column: x => x.movimiento_inventario_detalle_id,
                        principalSchema: "s_inventario",
                        principalTable: "movimientos_inventario_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_productos_presentaciones_produ~",
                        columns: x => new { x.producto_presentacion_id, x.producto_id, x.empresa_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumns: new[] { "id", "producto_id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_productos_producto_id_empresa_~",
                        columns: x => new { x.producto_id, x.empresa_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cobros_aplicaciones_reversos",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cobro_aplicacion_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    valor_reversado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_anterior = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_nuevo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobros_aplicaciones_reversos", x => x.id);
                    table.CheckConstraint("ck_cobros_aplicaciones_reversos_saldos", "saldo_anterior >= 0 AND saldo_nuevo >= 0 AND saldo_nuevo = saldo_anterior + valor_reversado");
                    table.CheckConstraint("ck_cobros_aplicaciones_reversos_valor", "valor_reversado > 0");
                    table.ForeignKey(
                        name: "FK_cobros_aplicaciones_reversos_cobros_aplicaciones_cobro_apli~",
                        column: x => x.cobro_aplicacion_id,
                        principalSchema: "s_cartera",
                        principalTable: "cobros_aplicaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cobros_aplicaciones_reversos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ajustes_compras_detalles",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ajuste_compra_id = table.Column<long>(type: "bigint", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    valor_sin_impuestos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuesto_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ajustes_compras_detalles", x => x.id);
                    table.ForeignKey(
                        name: "FK_ajustes_compras_detalles_ajustes_compras_ajuste_compra_id",
                        column: x => x.ajuste_compra_id,
                        principalSchema: "s_compras",
                        principalTable: "ajustes_compras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "liquidaciones_compra_detalles_impuestos",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    liquidacion_compra_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    tarifa_impuesto_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo_impuesto_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo_porcentaje_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre_impuesto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_calculo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    valor_especifico = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valor_impuesto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_liquidaciones_compra_detalles_impuestos", x => x.id);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_detalles_impuestos_liquidaciones_compr~",
                        column: x => x.liquidacion_compra_detalle_id,
                        principalSchema: "s_compras",
                        principalTable: "liquidaciones_compra_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_compra_detalles_impuestos_tarifas_impuesto_ta~",
                        column: x => x.tarifa_impuesto_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tarifas_impuesto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos_aplicaciones_reversos",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pago_aplicacion_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    valor_reversado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_anterior = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_nuevo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_aplicaciones_reversos", x => x.id);
                    table.CheckConstraint("ck_pagos_aplicaciones_reversos_saldos", "saldo_anterior >= 0 AND saldo_nuevo >= 0 AND saldo_nuevo = saldo_anterior + valor_reversado");
                    table.CheckConstraint("ck_pagos_aplicaciones_reversos_valor", "valor_reversado > 0");
                    table.ForeignKey(
                        name: "FK_pagos_aplicaciones_reversos_pagos_aplicaciones_pago_aplicac~",
                        column: x => x.pago_aplicacion_id,
                        principalSchema: "s_cartera",
                        principalTable: "pagos_aplicaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_aplicaciones_reversos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_bancarios",
                schema: "s_bancos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cuenta_bancaria_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_movimiento_bancario_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_tipo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    origen_id = table.Column<long>(type: "bigint", nullable: true),
                    cobro_medio_id = table.Column<long>(type: "bigint", nullable: true),
                    pago_medio_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha_movimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    concepto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    movimiento_reverso_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_bancarios", x => x.id);
                    table.CheckConstraint("ck_movimientos_bancarios_medio", "NOT (cobro_medio_id IS NOT NULL AND pago_medio_id IS NOT NULL)");
                    table.CheckConstraint("ck_movimientos_bancarios_valor", "valor > 0");
                    table.ForeignKey(
                        name: "FK_movimientos_bancarios_cobros_medios_cobro_medio_id",
                        column: x => x.cobro_medio_id,
                        principalSchema: "s_cartera",
                        principalTable: "cobros_medios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_bancarios_cuentas_bancarias_cuenta_bancaria_id",
                        column: x => x.cuenta_bancaria_id,
                        principalSchema: "s_bancos",
                        principalTable: "cuentas_bancarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_bancarios_movimientos_bancarios_movimiento_reve~",
                        column: x => x.movimiento_reverso_id,
                        principalSchema: "s_bancos",
                        principalTable: "movimientos_bancarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_bancarios_pagos_medios_pago_medio_id",
                        column: x => x.pago_medio_id,
                        principalSchema: "s_cartera",
                        principalTable: "pagos_medios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_bancarios_tipos_movimiento_bancario_tipo_movimi~",
                        column: x => x.tipo_movimiento_bancario_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_movimiento_bancario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_bancarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_caja",
                schema: "s_tesoreria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    caja_sesion_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_movimiento_caja_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_tipo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    origen_id = table.Column<long>(type: "bigint", nullable: true),
                    cobro_medio_id = table.Column<long>(type: "bigint", nullable: true),
                    pago_medio_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha_movimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    concepto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    movimiento_reverso_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_caja", x => x.id);
                    table.CheckConstraint("ck_movimientos_caja_medio", "NOT (cobro_medio_id IS NOT NULL AND pago_medio_id IS NOT NULL)");
                    table.CheckConstraint("ck_movimientos_caja_valor", "valor > 0");
                    table.ForeignKey(
                        name: "FK_movimientos_caja_cajas_sesiones_caja_sesion_id",
                        column: x => x.caja_sesion_id,
                        principalSchema: "s_tesoreria",
                        principalTable: "cajas_sesiones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_cobros_medios_cobro_medio_id",
                        column: x => x.cobro_medio_id,
                        principalSchema: "s_cartera",
                        principalTable: "cobros_medios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_movimientos_caja_movimiento_reverso_id",
                        column: x => x.movimiento_reverso_id,
                        principalSchema: "s_tesoreria",
                        principalTable: "movimientos_caja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_pagos_medios_pago_medio_id",
                        column: x => x.pago_medio_id,
                        principalSchema: "s_cartera",
                        principalTable: "pagos_medios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_tipos_movimiento_caja_tipo_movimiento_caja~",
                        column: x => x.tipo_movimiento_caja_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_movimiento_caja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "facturas_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    factura_id = table.Column<long>(type: "bigint", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_facturable = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_referencia = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descuento_valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_final_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_modificado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facturas_detalles", x => x.id);
                    table.CheckConstraint("ck_facturas_detalles_cantidades", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.ForeignKey(
                        name: "FK_facturas_detalles_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_detalles_facturas_factura_id",
                        column: x => x.factura_id,
                        principalSchema: "s_ventas",
                        principalTable: "facturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_detalles_productos_presentaciones_producto_present~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_detalles_usuarios_precio_modificado_por_usuario_id",
                        column: x => x.precio_modificado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "facturas_formas_pago",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    factura_id = table.Column<long>(type: "bigint", nullable: false),
                    medio_pago_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_forma_pago_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    plazo = table.Column<int>(type: "integer", nullable: true),
                    unidad_tiempo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facturas_formas_pago", x => x.id);
                    table.CheckConstraint("ck_facturas_formas_pago_plazo", "plazo IS NULL OR plazo >= 0");
                    table.CheckConstraint("ck_facturas_formas_pago_valor", "valor > 0");
                    table.ForeignKey(
                        name: "FK_facturas_formas_pago_facturas_factura_id",
                        column: x => x.factura_id,
                        principalSchema: "s_ventas",
                        principalTable: "facturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_formas_pago_medios_pago_medio_pago_id",
                        column: x => x.medio_pago_id,
                        principalSchema: "s_catalogos",
                        principalTable: "medios_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notas_credito",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    punto_emision_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_comprobante_id = table.Column<long>(type: "bigint", nullable: false),
                    factura_id = table.Column<long>(type: "bigint", nullable: false),
                    devolucion_venta_id = table.Column<long>(type: "bigint", nullable: true),
                    numero_documento = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    secuencial = table.Column<int>(type: "integer", nullable: false),
                    fecha_emision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subtotal_sin_impuestos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuesto_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_credito", x => x.id);
                    table.ForeignKey(
                        name: "FK_notas_credito_devoluciones_ventas_devolucion_venta_id",
                        column: x => x.devolucion_venta_id,
                        principalSchema: "s_ventas",
                        principalTable: "devoluciones_ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_credito_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_credito_empresas_terceros_empresa_tercero_id_empresa_~",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_credito_establecimientos_establecimiento_id_empresa_id",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_credito_facturas_factura_id",
                        column: x => x.factura_id,
                        principalSchema: "s_ventas",
                        principalTable: "facturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_credito_puntos_emision_punto_emision_id_establecimien~",
                        columns: x => new { x.punto_emision_id, x.establecimiento_id },
                        principalSchema: "s_configuracion",
                        principalTable: "puntos_emision",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_credito_tipos_comprobante_tipo_comprobante_id",
                        column: x => x.tipo_comprobante_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_comprobante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_credito_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_credito_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notas_debito",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    punto_emision_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_comprobante_id = table.Column<long>(type: "bigint", nullable: false),
                    factura_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_documento = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    secuencial = table.Column<int>(type: "integer", nullable: false),
                    fecha_emision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subtotal_sin_impuestos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuesto_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    establecimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_tercero_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    anulado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    anulada_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_debito", x => x.id);
                    table.ForeignKey(
                        name: "FK_notas_debito_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "s_configuracion",
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_debito_empresas_terceros_empresa_tercero_id_empresa_id",
                        columns: x => new { x.empresa_tercero_id, x.empresa_id },
                        principalSchema: "s_comercial",
                        principalTable: "empresas_terceros",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_debito_establecimientos_establecimiento_id_empresa_id",
                        columns: x => new { x.establecimiento_id, x.empresa_id },
                        principalSchema: "s_configuracion",
                        principalTable: "establecimientos",
                        principalColumns: new[] { "id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_debito_facturas_factura_id",
                        column: x => x.factura_id,
                        principalSchema: "s_ventas",
                        principalTable: "facturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_debito_puntos_emision_punto_emision_id_establecimient~",
                        columns: x => new { x.punto_emision_id, x.establecimiento_id },
                        principalSchema: "s_configuracion",
                        principalTable: "puntos_emision",
                        principalColumns: new[] { "id", "establecimiento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_debito_tipos_comprobante_tipo_comprobante_id",
                        column: x => x.tipo_comprobante_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_comprobante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_debito_usuarios_anulado_por_usuario_id",
                        column: x => x.anulado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_debito_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retenciones_recibidas_documentos",
                schema: "s_tributacion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    retencion_recibida_id = table.Column<long>(type: "bigint", nullable: false),
                    factura_id = table.Column<long>(type: "bigint", nullable: false),
                    valor_retenido_aplicado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retenciones_recibidas_documentos", x => x.id);
                    table.CheckConstraint("ck_retenciones_recibidas_documentos_valor", "valor_retenido_aplicado > 0");
                    table.ForeignKey(
                        name: "FK_retenciones_recibidas_documentos_facturas_factura_id",
                        column: x => x.factura_id,
                        principalSchema: "s_ventas",
                        principalTable: "facturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_retenciones_recibidas_documentos_retenciones_recibidas_rete~",
                        column: x => x.retencion_recibida_id,
                        principalSchema: "s_tributacion",
                        principalTable: "retenciones_recibidas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notas_entrega_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nota_entrega_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_presentacion_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_facturable = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_referencia = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descuento_valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_final_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_unitario_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_modificado_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_entrega_detalles", x => x.id);
                    table.CheckConstraint("ck_notas_entrega_detalles_cantidades", "cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0");
                    table.ForeignKey(
                        name: "FK_notas_entrega_detalles_bodegas_bodega_id",
                        column: x => x.bodega_id,
                        principalSchema: "s_inventario",
                        principalTable: "bodegas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_entrega_detalles_notas_entrega_nota_entrega_id",
                        column: x => x.nota_entrega_id,
                        principalSchema: "s_ventas",
                        principalTable: "notas_entrega",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_entrega_detalles_productos_presentaciones_producto_pr~",
                        column: x => x.producto_presentacion_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos_presentaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_entrega_detalles_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "s_inventario",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_entrega_detalles_usuarios_precio_modificado_por_usuar~",
                        column: x => x.precio_modificado_por_usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proformas_detalles_impuestos",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proforma_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    tarifa_impuesto_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo_impuesto_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo_porcentaje_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre_impuesto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_calculo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    valor_especifico = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valor_impuesto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proformas_detalles_impuestos", x => x.id);
                    table.ForeignKey(
                        name: "FK_proformas_detalles_impuestos_proformas_detalles_proforma_de~",
                        column: x => x.proforma_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "proformas_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proformas_detalles_impuestos_tarifas_impuesto_tarifa_impues~",
                        column: x => x.tarifa_impuesto_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tarifas_impuesto",
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
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
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
                        columns: x => new { x.compra_recepcion_detalle_id, x.producto_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "compras_recepciones_detalles",
                        principalColumns: new[] { "id", "producto_id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_lotes_productos_lotes_producto~",
                        columns: x => new { x.producto_lote_id, x.producto_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos_lotes",
                        principalColumns: new[] { "id", "producto_id" },
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
                    empresa_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_id = table.Column<long>(type: "bigint", nullable: false),
                    bodega_id = table.Column<long>(type: "bigint", nullable: false),
                    producto_serie_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_serie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_recepciones_detalles_series", x => x.id);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_series_compras_recepciones_det~",
                        columns: x => new { x.compra_recepcion_detalle_id, x.producto_id, x.bodega_id, x.empresa_id },
                        principalSchema: "s_compras",
                        principalTable: "compras_recepciones_detalles",
                        principalColumns: new[] { "id", "producto_id", "bodega_id", "empresa_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_recepciones_detalles_series_productos_series_produc~",
                        columns: x => new { x.producto_serie_id, x.producto_id, x.bodega_id },
                        principalSchema: "s_inventario",
                        principalTable: "productos_series",
                        principalColumns: new[] { "id", "producto_id", "bodega_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_por_cobrar_movimientos",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cuenta_por_cobrar_id = table.Column<long>(type: "bigint", nullable: false),
                    secuencia = table.Column<int>(type: "integer", nullable: false),
                    tipo_movimiento_cartera_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_tipo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    origen_id = table.Column<long>(type: "bigint", nullable: true),
                    cobro_aplicacion_id = table.Column<long>(type: "bigint", nullable: true),
                    reverso_aplicacion_id = table.Column<long>(type: "bigint", nullable: true),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_anterior = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_nuevo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_por_cobrar_movimientos", x => x.id);
                    table.CheckConstraint("ck_cuentas_por_cobrar_movimientos_aplicacion", "NOT (cobro_aplicacion_id IS NOT NULL AND reverso_aplicacion_id IS NOT NULL)");
                    table.CheckConstraint("ck_cuentas_por_cobrar_movimientos_secuencia", "secuencia > 0");
                    table.CheckConstraint("ck_cuentas_por_cobrar_movimientos_valor", "valor > 0 AND saldo_anterior >= 0 AND saldo_nuevo >= 0");
                    table.ForeignKey(
                        name: "FK_cuentas_por_cobrar_movimientos_cobros_aplicaciones_cobro_ap~",
                        column: x => x.cobro_aplicacion_id,
                        principalSchema: "s_cartera",
                        principalTable: "cobros_aplicaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_por_cobrar_movimientos_cobros_aplicaciones_reversos~",
                        column: x => x.reverso_aplicacion_id,
                        principalSchema: "s_cartera",
                        principalTable: "cobros_aplicaciones_reversos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_por_cobrar_movimientos_cuentas_por_cobrar_cuenta_po~",
                        column: x => x.cuenta_por_cobrar_id,
                        principalSchema: "s_cartera",
                        principalTable: "cuentas_por_cobrar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_por_cobrar_movimientos_tipos_movimiento_cartera_tip~",
                        column: x => x.tipo_movimiento_cartera_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_movimiento_cartera",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_por_cobrar_movimientos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ajustes_compras_detalles_impuestos",
                schema: "s_compras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ajuste_compra_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    tarifa_impuesto_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo_impuesto_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo_porcentaje_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre_impuesto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_calculo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    valor_especifico = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valor_impuesto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ajustes_compras_detalles_impuestos", x => x.id);
                    table.ForeignKey(
                        name: "FK_ajustes_compras_detalles_impuestos_ajustes_compras_detalles~",
                        column: x => x.ajuste_compra_detalle_id,
                        principalSchema: "s_compras",
                        principalTable: "ajustes_compras_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_compras_detalles_impuestos_tarifas_impuesto_tarifa_~",
                        column: x => x.tarifa_impuesto_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tarifas_impuesto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_por_pagar_movimientos",
                schema: "s_cartera",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cuenta_por_pagar_id = table.Column<long>(type: "bigint", nullable: false),
                    secuencia = table.Column<int>(type: "integer", nullable: false),
                    tipo_movimiento_cuenta_por_pagar_id = table.Column<long>(type: "bigint", nullable: false),
                    origen_tipo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    origen_id = table.Column<long>(type: "bigint", nullable: true),
                    pago_aplicacion_id = table.Column<long>(type: "bigint", nullable: true),
                    reverso_aplicacion_id = table.Column<long>(type: "bigint", nullable: true),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_anterior = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_nuevo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_por_pagar_movimientos", x => x.id);
                    table.CheckConstraint("ck_cuentas_por_pagar_movimientos_aplicacion", "NOT (pago_aplicacion_id IS NOT NULL AND reverso_aplicacion_id IS NOT NULL)");
                    table.CheckConstraint("ck_cuentas_por_pagar_movimientos_secuencia", "secuencia > 0");
                    table.CheckConstraint("ck_cuentas_por_pagar_movimientos_valor", "valor > 0 AND saldo_anterior >= 0 AND saldo_nuevo >= 0");
                    table.ForeignKey(
                        name: "FK_cuentas_por_pagar_movimientos_cuentas_por_pagar_cuenta_por_~",
                        column: x => x.cuenta_por_pagar_id,
                        principalSchema: "s_cartera",
                        principalTable: "cuentas_por_pagar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_por_pagar_movimientos_pagos_aplicaciones_pago_aplic~",
                        column: x => x.pago_aplicacion_id,
                        principalSchema: "s_cartera",
                        principalTable: "pagos_aplicaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_por_pagar_movimientos_pagos_aplicaciones_reversos_r~",
                        column: x => x.reverso_aplicacion_id,
                        principalSchema: "s_cartera",
                        principalTable: "pagos_aplicaciones_reversos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_por_pagar_movimientos_tipos_movimiento_cuentas_por_~",
                        column: x => x.tipo_movimiento_cuenta_por_pagar_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tipos_movimiento_cuentas_por_pagar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_por_pagar_movimientos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "s_seguridad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    evidencia_nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    evidencia_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    evidencia_tamano = table.Column<long>(type: "bigint", nullable: true),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    es_deducible = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    movimiento_caja_id = table.Column<long>(type: "bigint", nullable: true),
                    movimiento_bancario_id = table.Column<long>(type: "bigint", nullable: true),
                    movimiento_inventario_id = table.Column<long>(type: "bigint", nullable: true),
                    asiento_id = table.Column<long>(type: "bigint", nullable: true),
                    operacion_sustituida_id = table.Column<long>(type: "bigint", nullable: true),
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
                        name: "FK_operaciones_sin_sustento_operaciones_sin_sustento_operacion~",
                        column: x => x.operacion_sustituida_id,
                        principalSchema: "s_tesoreria",
                        principalTable: "operaciones_sin_sustento",
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
                name: "facturas_detalles_impuestos",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    factura_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    tarifa_impuesto_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo_impuesto_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo_porcentaje_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre_impuesto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_calculo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    valor_especifico = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valor_impuesto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facturas_detalles_impuestos", x => x.id);
                    table.ForeignKey(
                        name: "FK_facturas_detalles_impuestos_facturas_detalles_factura_detal~",
                        column: x => x.factura_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "facturas_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_detalles_impuestos_tarifas_impuesto_tarifa_impuest~",
                        column: x => x.tarifa_impuesto_id,
                        principalSchema: "s_catalogos",
                        principalTable: "tarifas_impuesto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "facturas_xf_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    factura_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    venta_xf_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facturas_xf_detalles", x => x.id);
                    table.ForeignKey(
                        name: "FK_facturas_xf_detalles_facturas_detalles_factura_detalle_id",
                        column: x => x.factura_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "facturas_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_xf_detalles_ventas_xf_detalles_venta_xf_detalle_id",
                        column: x => x.venta_xf_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "ventas_xf_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notas_credito_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nota_credito_id = table.Column<long>(type: "bigint", nullable: false),
                    factura_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cantidad_presentacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    factor_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_credito_detalles", x => x.id);
                    table.ForeignKey(
                        name: "FK_notas_credito_detalles_facturas_detalles_factura_detalle_id",
                        column: x => x.factura_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "facturas_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_credito_detalles_notas_credito_nota_credito_id",
                        column: x => x.nota_credito_id,
                        principalSchema: "s_ventas",
                        principalTable: "notas_credito",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notas_debito_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nota_debito_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_debito_detalles", x => x.id);
                    table.CheckConstraint("ck_notas_debito_detalles_valor", "valor > 0");
                    table.ForeignKey(
                        name: "FK_notas_debito_detalles_notas_debito_nota_debito_id",
                        column: x => x.nota_debito_id,
                        principalSchema: "s_ventas",
                        principalTable: "notas_debito",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "facturas_notas_entrega_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    factura_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    nota_entrega_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facturas_notas_entrega_detalles", x => x.id);
                    table.ForeignKey(
                        name: "FK_facturas_notas_entrega_detalles_facturas_detalles_factura_d~",
                        column: x => x.factura_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "facturas_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturas_notas_entrega_detalles_notas_entrega_detalles_nota~",
                        column: x => x.nota_entrega_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "notas_entrega_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notas_entrega_xf_detalles",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nota_entrega_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    venta_xf_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_entrega_xf_detalles", x => x.id);
                    table.ForeignKey(
                        name: "FK_notas_entrega_xf_detalles_notas_entrega_detalles_nota_entre~",
                        column: x => x.nota_entrega_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "notas_entrega_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_entrega_xf_detalles_ventas_xf_detalles_venta_xf_detal~",
                        column: x => x.venta_xf_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "ventas_xf_detalles",
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

            migrationBuilder.CreateTable(
                name: "notas_credito_detalles_impuestos",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nota_credito_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_impuesto_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo_porcentaje_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre_impuesto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_calculo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    valor_especifico = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valor_impuesto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_credito_detalles_impuestos", x => x.id);
                    table.ForeignKey(
                        name: "FK_notas_credito_detalles_impuestos_notas_credito_detalles_not~",
                        column: x => x.nota_credito_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "notas_credito_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notas_debito_detalles_impuestos",
                schema: "s_ventas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nota_debito_detalle_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_impuesto_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    codigo_porcentaje_sri = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre_impuesto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_calculo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    valor_especifico = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valor_impuesto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_debito_detalles_impuestos", x => x.id);
                    table.ForeignKey(
                        name: "FK_notas_debito_detalles_impuestos_notas_debito_detalles_nota_~",
                        column: x => x.nota_debito_detalle_id,
                        principalSchema: "s_ventas",
                        principalTable: "notas_debito_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_compras_compra_id_empresa_id",
                schema: "s_compras",
                table: "ajustes_compras",
                columns: new[] { "compra_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_compras_devolucion_compra_id_empresa_id",
                schema: "s_compras",
                table: "ajustes_compras",
                columns: new[] { "devolucion_compra_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_compras_documento_recibido_sri_id_empresa_id",
                schema: "s_compras",
                table: "ajustes_compras",
                columns: new[] { "documento_recibido_sri_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_compras_empresa_id",
                schema: "s_compras",
                table: "ajustes_compras",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_compras_empresa_tercero_id_empresa_id",
                schema: "s_compras",
                table: "ajustes_compras",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_compras_usuario_id",
                schema: "s_compras",
                table: "ajustes_compras",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_compras_detalles_ajuste_compra_id",
                schema: "s_compras",
                table: "ajustes_compras_detalles",
                column: "ajuste_compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_compras_detalles_impuestos_ajuste_compra_detalle_id",
                schema: "s_compras",
                table: "ajustes_compras_detalles_impuestos",
                column: "ajuste_compra_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_compras_detalles_impuestos_tarifa_impuesto_id",
                schema: "s_compras",
                table: "ajustes_compras_detalles_impuestos",
                column: "tarifa_impuesto_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_inventario_anulado_por_usuario_id",
                schema: "s_inventario",
                table: "ajustes_inventario",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_inventario_bodega_id_establecimiento_id",
                schema: "s_inventario",
                table: "ajustes_inventario",
                columns: new[] { "bodega_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_inventario_establecimiento_id_empresa_id",
                schema: "s_inventario",
                table: "ajustes_inventario",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_inventario_motivo_operacion_inventario_id",
                schema: "s_inventario",
                table: "ajustes_inventario",
                column: "motivo_operacion_inventario_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_inventario_usuario_id",
                schema: "s_inventario",
                table: "ajustes_inventario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_ajustes_inventario_empresa_numero",
                schema: "s_inventario",
                table: "ajustes_inventario",
                columns: new[] { "empresa_id", "numero_ajuste" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_inventario_detalles_ajuste_inventario_id",
                schema: "s_inventario",
                table: "ajustes_inventario_detalles",
                column: "ajuste_inventario_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_inventario_detalles_producto_id",
                schema: "s_inventario",
                table: "ajustes_inventario_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_inventario_detalles_producto_presentacion_id",
                schema: "s_inventario",
                table: "ajustes_inventario_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_asientos_anulado_por_usuario_id",
                schema: "s_contabilidad",
                table: "asientos",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_asientos_periodo_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos",
                columns: new[] { "periodo_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_asientos_tipo_origen_asiento_id",
                schema: "s_contabilidad",
                table: "asientos",
                column: "tipo_origen_asiento_id");

            migrationBuilder.CreateIndex(
                name: "IX_asientos_usuario_id",
                schema: "s_contabilidad",
                table: "asientos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_asientos_empresa_numero",
                schema: "s_contabilidad",
                table: "asientos",
                columns: new[] { "empresa_id", "numero_asiento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_asientos_origen",
                schema: "s_contabilidad",
                table: "asientos",
                columns: new[] { "empresa_id", "tipo_origen_asiento_id", "origen_id" },
                unique: true,
                filter: "origen_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_asientos_origen_reversado",
                schema: "s_contabilidad",
                table: "asientos",
                column: "asiento_origen_reversado_id",
                unique: true,
                filter: "asiento_origen_reversado_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_asientos_detalles_asiento_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                columns: new[] { "asiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_asientos_detalles_cuenta_contable_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                columns: new[] { "cuenta_contable_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_asientos_detalles_empresa_tercero_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ux_asientos_detalles_asiento_orden",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                columns: new[] { "asiento_id", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_created_at",
                schema: "s_seguridad",
                table: "auditoria",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_empresa_created_at",
                schema: "s_seguridad",
                table: "auditoria",
                columns: new[] { "empresa_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_establecimiento_id",
                schema: "s_seguridad",
                table: "auditoria",
                column: "establecimiento_id");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_usuario_id",
                schema: "s_seguridad",
                table: "auditoria",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_bodegas_establecimiento_codigo",
                schema: "s_inventario",
                table: "bodegas",
                columns: new[] { "establecimiento_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cajas_cuenta_contable_id_empresa_id",
                schema: "s_tesoreria",
                table: "cajas",
                columns: new[] { "cuenta_contable_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cajas_establecimiento_id_empresa_id",
                schema: "s_tesoreria",
                table: "cajas",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ux_cajas_empresa_codigo",
                schema: "s_tesoreria",
                table: "cajas",
                columns: new[] { "empresa_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cajas_sesiones_abierta_por_usuario_id",
                schema: "s_tesoreria",
                table: "cajas_sesiones",
                column: "abierta_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_cajas_sesiones_cerrada_por_usuario_id",
                schema: "s_tesoreria",
                table: "cajas_sesiones",
                column: "cerrada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_cajas_sesiones_caja_abierta",
                schema: "s_tesoreria",
                table: "cajas_sesiones",
                column: "caja_id",
                unique: true,
                filter: "estado = 'ABIERTA'");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_productos_categoria_padre_id",
                schema: "s_catalogos",
                table: "categorias_productos",
                column: "categoria_padre_id");

            migrationBuilder.CreateIndex(
                name: "ux_categorias_productos_empresa_codigo",
                schema: "s_catalogos",
                table: "categorias_productos",
                columns: new[] { "empresa_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_categorias_productos_uuid",
                schema: "s_catalogos",
                table: "categorias_productos",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cobros_anulado_por_usuario_id",
                schema: "s_cartera",
                table: "cobros",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_cobros_empresa_tercero_id_empresa_id",
                schema: "s_cartera",
                table: "cobros",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cobros_usuario_id",
                schema: "s_cartera",
                table: "cobros",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_cobros_empresa_numero",
                schema: "s_cartera",
                table: "cobros",
                columns: new[] { "empresa_id", "numero_cobro" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cobros_aplicaciones_cuenta_por_cobrar_id",
                schema: "s_cartera",
                table: "cobros_aplicaciones",
                column: "cuenta_por_cobrar_id");

            migrationBuilder.CreateIndex(
                name: "ux_cobros_aplicaciones_cobro_cuenta",
                schema: "s_cartera",
                table: "cobros_aplicaciones",
                columns: new[] { "cobro_id", "cuenta_por_cobrar_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cobros_aplicaciones_reversos_usuario_id",
                schema: "s_cartera",
                table: "cobros_aplicaciones_reversos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_cobros_aplicaciones_reversos_aplicacion",
                schema: "s_cartera",
                table: "cobros_aplicaciones_reversos",
                column: "cobro_aplicacion_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cobros_medios_cobro_id",
                schema: "s_cartera",
                table: "cobros_medios",
                column: "cobro_id");

            migrationBuilder.CreateIndex(
                name: "IX_cobros_medios_medio_pago_id",
                schema: "s_cartera",
                table: "cobros_medios",
                column: "medio_pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_anulado_por_usuario_id",
                schema: "s_compras",
                table: "compras",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_compra_sustituida_id_empresa_id",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "compra_sustituida_id", "empresa_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compras_documento_recibido_sri_id_empresa_id",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "documento_recibido_sri_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_empresa_tercero_id_empresa_id",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_establecimiento_id_empresa_id",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_tipo_comprobante_id",
                schema: "s_compras",
                table: "compras",
                column: "tipo_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_usuario_id",
                schema: "s_compras",
                table: "compras",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_compras_compra_sustituida",
                schema: "s_compras",
                table: "compras",
                column: "compra_sustituida_id",
                unique: true,
                filter: "compra_sustituida_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_compras_documento_recibido_sri",
                schema: "s_compras",
                table: "compras",
                column: "documento_recibido_sri_id",
                unique: true,
                filter: "documento_recibido_sri_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_compras_empresa_proveedor_tipo_numero",
                schema: "s_compras",
                table: "compras",
                columns: new[] { "empresa_id", "empresa_tercero_id", "tipo_comprobante_id", "numero_documento" },
                unique: true,
                filter: "numero_documento IS NOT NULL AND estado <> 'ANULADA'");

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

            migrationBuilder.CreateIndex(
                name: "ux_compras_detalles_compra_orden",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "compra_id", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_impuestos_compra_detalle_id",
                schema: "s_compras",
                table: "compras_detalles_impuestos",
                column: "compra_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_impuestos_tarifa_impuesto_id",
                schema: "s_compras",
                table: "compras_detalles_impuestos",
                column: "tarifa_impuesto_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_anulada_por_usuario_id",
                schema: "s_compras",
                table: "compras_recepciones",
                column: "anulada_por_usuario_id");

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
                name: "IX_compras_recepciones_detalles_compra_detalle_id_compra_id_em~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_detalle_id", "compra_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_compra_recepcion_id_compra_id_~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "compra_id", "bodega_id", "empresa_id" });

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
                name: "ux_compras_recepciones_detalles_compra_detalle",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "compra_detalle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_compras_recepciones_detalles_movimiento",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                column: "movimiento_inventario_detalle_id",
                unique: true,
                filter: "movimiento_inventario_detalle_id IS NOT NULL");

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
                name: "ux_compras_recepciones_detalles_lotes_lote",
                schema: "s_compras",
                table: "compras_recepciones_detalles_lotes",
                columns: new[] { "compra_recepcion_detalle_id", "producto_lote_id" },
                unique: true);

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
                name: "ux_compras_recepciones_detalles_series_serie",
                schema: "s_compras",
                table: "compras_recepciones_detalles_series",
                columns: new[] { "compra_recepcion_detalle_id", "producto_serie_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_electronicos_empresa_id",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_electronicos_establecimiento_id_empresa_id",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ix_comprobantes_electronicos_procesamiento",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos",
                columns: new[] { "estado_comprobante_electronico_id", "procesamiento_iniciado_at" });

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_electronicos_punto_emision_id_establecimiento_~",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos",
                columns: new[] { "punto_emision_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_electronicos_tipo_ambiente_id",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos",
                column: "tipo_ambiente_id");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_electronicos_tipo_comprobante_id",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos",
                column: "tipo_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_electronicos_tipo_emision_id",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos",
                column: "tipo_emision_id");

            migrationBuilder.CreateIndex(
                name: "ux_comprobantes_electronicos_clave_acceso",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos",
                column: "clave_acceso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_comprobantes_electronicos_emision",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos",
                columns: new[] { "punto_emision_id", "tipo_comprobante_id", "tipo_ambiente_id", "secuencial" },
                unique: true,
                filter: "punto_emision_id IS NOT NULL AND secuencial IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_comprobantes_electronicos_origen",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos",
                columns: new[] { "tipo_origen_comprobante_electronico_id", "origen_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_electronicos_eventos_comprobante_electronico_id",
                schema: "s_facturacion_electronica",
                table: "comprobantes_electronicos_eventos",
                column: "comprobante_electronico_id");

            migrationBuilder.CreateIndex(
                name: "ux_conceptos_retencion_impuesto_codigo_vigencia",
                schema: "s_catalogos",
                table: "conceptos_retencion",
                columns: new[] { "impuesto_id", "codigo_sri", "vigente_desde" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_cuentas_cuenta_contable_id_empresa_id",
                schema: "s_contabilidad",
                table: "configuracion_cuentas",
                columns: new[] { "cuenta_contable_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_cuentas_tipo_configuracion_contable_id",
                schema: "s_contabilidad",
                table: "configuracion_cuentas",
                column: "tipo_configuracion_contable_id");

            migrationBuilder.CreateIndex(
                name: "ux_configuracion_cuentas_empresa_tipo",
                schema: "s_contabilidad",
                table: "configuracion_cuentas",
                columns: new[] { "empresa_id", "tipo_configuracion_contable_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversiones_control_inventario_anulado_por_usuario_id",
                schema: "s_inventario",
                table: "conversiones_control_inventario",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversiones_control_inventario_empresa_id_producto_id_fech~",
                schema: "s_inventario",
                table: "conversiones_control_inventario",
                columns: new[] { "empresa_id", "producto_id", "fecha_conversion" });

            migrationBuilder.CreateIndex(
                name: "IX_conversiones_control_inventario_motivo_operacion_inventario~",
                schema: "s_inventario",
                table: "conversiones_control_inventario",
                column: "motivo_operacion_inventario_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversiones_control_inventario_producto_id",
                schema: "s_inventario",
                table: "conversiones_control_inventario",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversiones_control_inventario_usuario_id",
                schema: "s_inventario",
                table: "conversiones_control_inventario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversiones_control_inventario_detalles_bodega_id",
                schema: "s_inventario",
                table: "conversiones_control_inventario_detalles",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversiones_control_inventario_detalles_conversion_control~",
                schema: "s_inventario",
                table: "conversiones_control_inventario_detalles",
                column: "conversion_control_inventario_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversiones_control_inventario_detalles_producto_lote_id",
                schema: "s_inventario",
                table: "conversiones_control_inventario_detalles",
                column: "producto_lote_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversiones_control_inventario_series_conversion_control_i~",
                schema: "s_inventario",
                table: "conversiones_control_inventario_series",
                columns: new[] { "conversion_control_inventario_id", "producto_serie_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversiones_control_inventario_series_producto_serie_id",
                schema: "s_inventario",
                table: "conversiones_control_inventario_series",
                column: "producto_serie_id");

            migrationBuilder.CreateIndex(
                name: "IX_correcciones_datos_inventario_empresa_id_producto_id_fecha_~",
                schema: "s_inventario",
                table: "correcciones_datos_inventario",
                columns: new[] { "empresa_id", "producto_id", "fecha_correccion" });

            migrationBuilder.CreateIndex(
                name: "IX_correcciones_datos_inventario_motivo_operacion_inventario_id",
                schema: "s_inventario",
                table: "correcciones_datos_inventario",
                column: "motivo_operacion_inventario_id");

            migrationBuilder.CreateIndex(
                name: "IX_correcciones_datos_inventario_producto_id",
                schema: "s_inventario",
                table: "correcciones_datos_inventario",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_correcciones_datos_inventario_usuario_id",
                schema: "s_inventario",
                table: "correcciones_datos_inventario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_bancarias_cuenta_contable_id_empresa_id",
                schema: "s_bancos",
                table: "cuentas_bancarias",
                columns: new[] { "cuenta_contable_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ux_cuentas_bancarias_empresa_banco_numero",
                schema: "s_bancos",
                table: "cuentas_bancarias",
                columns: new[] { "empresa_id", "banco", "numero_cuenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_por_cobrar_empresa_tercero_id_empresa_id",
                schema: "s_cartera",
                table: "cuentas_por_cobrar",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ux_cuentas_por_cobrar_origen",
                schema: "s_cartera",
                table: "cuentas_por_cobrar",
                columns: new[] { "empresa_id", "origen_tipo", "origen_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_por_cobrar_movimientos_cobro_aplicacion_id",
                schema: "s_cartera",
                table: "cuentas_por_cobrar_movimientos",
                column: "cobro_aplicacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_por_cobrar_movimientos_reverso_aplicacion_id",
                schema: "s_cartera",
                table: "cuentas_por_cobrar_movimientos",
                column: "reverso_aplicacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_por_cobrar_movimientos_tipo_movimiento_cartera_id",
                schema: "s_cartera",
                table: "cuentas_por_cobrar_movimientos",
                column: "tipo_movimiento_cartera_id");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_por_cobrar_movimientos_usuario_id",
                schema: "s_cartera",
                table: "cuentas_por_cobrar_movimientos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_cuentas_por_cobrar_movimientos_secuencia",
                schema: "s_cartera",
                table: "cuentas_por_cobrar_movimientos",
                columns: new[] { "cuenta_por_cobrar_id", "secuencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_por_pagar_empresa_tercero_id_empresa_id",
                schema: "s_cartera",
                table: "cuentas_por_pagar",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ux_cuentas_por_pagar_origen",
                schema: "s_cartera",
                table: "cuentas_por_pagar",
                columns: new[] { "empresa_id", "origen_tipo", "origen_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_por_pagar_movimientos_pago_aplicacion_id",
                schema: "s_cartera",
                table: "cuentas_por_pagar_movimientos",
                column: "pago_aplicacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_por_pagar_movimientos_reverso_aplicacion_id",
                schema: "s_cartera",
                table: "cuentas_por_pagar_movimientos",
                column: "reverso_aplicacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_por_pagar_movimientos_tipo_movimiento_cuenta_por_pa~",
                schema: "s_cartera",
                table: "cuentas_por_pagar_movimientos",
                column: "tipo_movimiento_cuenta_por_pagar_id");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_por_pagar_movimientos_usuario_id",
                schema: "s_cartera",
                table: "cuentas_por_pagar_movimientos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_cuentas_por_pagar_movimientos_secuencia",
                schema: "s_cartera",
                table: "cuentas_por_pagar_movimientos",
                columns: new[] { "cuenta_por_pagar_id", "secuencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_depositos_caja_banco_anulado_por_usuario_id",
                schema: "s_tesoreria",
                table: "depositos_caja_banco",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_depositos_caja_banco_caja_sesion_id",
                schema: "s_tesoreria",
                table: "depositos_caja_banco",
                column: "caja_sesion_id");

            migrationBuilder.CreateIndex(
                name: "IX_depositos_caja_banco_cuenta_bancaria_id_empresa_id",
                schema: "s_tesoreria",
                table: "depositos_caja_banco",
                columns: new[] { "cuenta_bancaria_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_depositos_caja_banco_usuario_id",
                schema: "s_tesoreria",
                table: "depositos_caja_banco",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_depositos_caja_banco_empresa_numero",
                schema: "s_tesoreria",
                table: "depositos_caja_banco",
                columns: new[] { "empresa_id", "numero_deposito" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_compras_anulado_por_usuario_id",
                schema: "s_compras",
                table: "devoluciones_compras",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_compras_empresa_tercero_id_empresa_id",
                schema: "s_compras",
                table: "devoluciones_compras",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_compras_establecimiento_id_empresa_id",
                schema: "s_compras",
                table: "devoluciones_compras",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ix_devoluciones_compras_origen",
                schema: "s_compras",
                table: "devoluciones_compras",
                columns: new[] { "empresa_id", "tipo_origen_devolucion_compra_id", "origen_id" });

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_compras_tipo_origen_devolucion_compra_id",
                schema: "s_compras",
                table: "devoluciones_compras",
                column: "tipo_origen_devolucion_compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_compras_usuario_id",
                schema: "s_compras",
                table: "devoluciones_compras",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_devoluciones_compras_empresa_numero",
                schema: "s_compras",
                table: "devoluciones_compras",
                columns: new[] { "empresa_id", "numero_devolucion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_compras_detalles_bodega_id",
                schema: "s_compras",
                table: "devoluciones_compras_detalles",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_compras_detalles_devolucion_compra_id",
                schema: "s_compras",
                table: "devoluciones_compras_detalles",
                column: "devolucion_compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_compras_detalles_producto_id",
                schema: "s_compras",
                table: "devoluciones_compras_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_compras_detalles_producto_presentacion_id",
                schema: "s_compras",
                table: "devoluciones_compras_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_ventas_anulado_por_usuario_id",
                schema: "s_ventas",
                table: "devoluciones_ventas",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_ventas_empresa_tercero_id_empresa_id",
                schema: "s_ventas",
                table: "devoluciones_ventas",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_ventas_establecimiento_id_empresa_id",
                schema: "s_ventas",
                table: "devoluciones_ventas",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ix_devoluciones_ventas_origen",
                schema: "s_ventas",
                table: "devoluciones_ventas",
                columns: new[] { "empresa_id", "tipo_origen_devolucion_venta_id", "origen_id" });

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_ventas_tipo_origen_devolucion_venta_id",
                schema: "s_ventas",
                table: "devoluciones_ventas",
                column: "tipo_origen_devolucion_venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_ventas_usuario_id",
                schema: "s_ventas",
                table: "devoluciones_ventas",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_devoluciones_ventas_empresa_numero",
                schema: "s_ventas",
                table: "devoluciones_ventas",
                columns: new[] { "empresa_id", "numero_devolucion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_ventas_detalles_bodega_id",
                schema: "s_ventas",
                table: "devoluciones_ventas_detalles",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_ventas_detalles_devolucion_venta_id",
                schema: "s_ventas",
                table: "devoluciones_ventas_detalles",
                column: "devolucion_venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_ventas_detalles_producto_id",
                schema: "s_ventas",
                table: "devoluciones_ventas_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_devoluciones_ventas_detalles_producto_presentacion_id",
                schema: "s_ventas",
                table: "devoluciones_ventas_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_recibidos_sri_empresa_id",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_recibidos_sri_tercero_id",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                column: "tercero_id");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_recibidos_sri_tipo_comprobante_id",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                column: "tipo_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "ux_documentos_recibidos_sri_archivo_sha256",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                column: "archivo_sha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_documentos_recibidos_sri_clave_acceso",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                column: "clave_acceso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documentos_recibidos_sri_pagos_documento_recibido_sri_id",
                schema: "s_compras",
                table: "documentos_recibidos_sri_pagos",
                column: "documento_recibido_sri_id");

            migrationBuilder.CreateIndex(
                name: "IX_empresas_regimen_tributario_id",
                schema: "s_configuracion",
                table: "empresas",
                column: "regimen_tributario_id");

            migrationBuilder.CreateIndex(
                name: "ux_empresas_numero_identificacion",
                schema: "s_configuracion",
                table: "empresas",
                column: "numero_identificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_empresas_terceros_lista_precio_id_empresa_id",
                schema: "s_comercial",
                table: "empresas_terceros",
                columns: new[] { "lista_precio_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_empresas_terceros_tercero_id",
                schema: "s_comercial",
                table: "empresas_terceros",
                column: "tercero_id");

            migrationBuilder.CreateIndex(
                name: "ux_empresas_terceros_empresa_tercero",
                schema: "s_comercial",
                table: "empresas_terceros",
                columns: new[] { "empresa_id", "tercero_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_establecimientos_empresa_codigo",
                schema: "s_configuracion",
                table: "establecimientos",
                columns: new[] { "empresa_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_establecimientos_empresa_matriz",
                schema: "s_configuracion",
                table: "establecimientos",
                column: "empresa_id",
                unique: true,
                filter: "es_matriz");

            migrationBuilder.CreateIndex(
                name: "ux_establecimientos_empresa_prefijo",
                schema: "s_configuracion",
                table: "establecimientos",
                columns: new[] { "empresa_id", "prefijo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_estados_comprobante_electronico_codigo",
                schema: "s_catalogos",
                table: "estados_comprobante_electronico",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_estados_serie_codigo",
                schema: "s_catalogos",
                table: "estados_serie",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_facturacion_electronica_tipo_ambiente_id",
                schema: "s_configuracion",
                table: "facturacion_electronica",
                column: "tipo_ambiente_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturacion_electronica_tipo_emision_id",
                schema: "s_configuracion",
                table: "facturacion_electronica",
                column: "tipo_emision_id");

            migrationBuilder.CreateIndex(
                name: "ux_facturacion_electronica_empresa",
                schema: "s_configuracion",
                table: "facturacion_electronica",
                column: "empresa_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_facturas_anulado_por_usuario_id",
                schema: "s_ventas",
                table: "facturas",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_empresa_tercero_id_empresa_id",
                schema: "s_ventas",
                table: "facturas",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_facturas_establecimiento_id_empresa_id",
                schema: "s_ventas",
                table: "facturas",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_facturas_proforma_id",
                schema: "s_ventas",
                table: "facturas",
                column: "proforma_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_punto_emision_id_establecimiento_id",
                schema: "s_ventas",
                table: "facturas",
                columns: new[] { "punto_emision_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_facturas_receptor_distinto_autorizado_por_usuario_id",
                schema: "s_ventas",
                table: "facturas",
                column: "receptor_distinto_autorizado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_tipo_comprobante_id",
                schema: "s_ventas",
                table: "facturas",
                column: "tipo_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_usuario_id",
                schema: "s_ventas",
                table: "facturas",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_facturas_emision_secuencial",
                schema: "s_ventas",
                table: "facturas",
                columns: new[] { "punto_emision_id", "tipo_comprobante_id", "secuencial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_facturas_empresa_numero",
                schema: "s_ventas",
                table: "facturas",
                columns: new[] { "empresa_id", "numero_documento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_facturas_detalles_bodega_id",
                schema: "s_ventas",
                table: "facturas_detalles",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_detalles_factura_id",
                schema: "s_ventas",
                table: "facturas_detalles",
                column: "factura_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_detalles_precio_modificado_por_usuario_id",
                schema: "s_ventas",
                table: "facturas_detalles",
                column: "precio_modificado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_detalles_producto_id",
                schema: "s_ventas",
                table: "facturas_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_detalles_producto_presentacion_id",
                schema: "s_ventas",
                table: "facturas_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_detalles_impuestos_factura_detalle_id",
                schema: "s_ventas",
                table: "facturas_detalles_impuestos",
                column: "factura_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_detalles_impuestos_tarifa_impuesto_id",
                schema: "s_ventas",
                table: "facturas_detalles_impuestos",
                column: "tarifa_impuesto_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_formas_pago_factura_id",
                schema: "s_ventas",
                table: "facturas_formas_pago",
                column: "factura_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_formas_pago_medio_pago_id",
                schema: "s_ventas",
                table: "facturas_formas_pago",
                column: "medio_pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_notas_entrega_detalles_nota_entrega_detalle_id",
                schema: "s_ventas",
                table: "facturas_notas_entrega_detalles",
                column: "nota_entrega_detalle_id");

            migrationBuilder.CreateIndex(
                name: "ux_facturas_notas_entrega_detalles_par",
                schema: "s_ventas",
                table: "facturas_notas_entrega_detalles",
                columns: new[] { "factura_detalle_id", "nota_entrega_detalle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_facturas_xf_detalles_venta_xf_detalle_id",
                schema: "s_ventas",
                table: "facturas_xf_detalles",
                column: "venta_xf_detalle_id");

            migrationBuilder.CreateIndex(
                name: "ux_facturas_xf_detalles_par",
                schema: "s_ventas",
                table: "facturas_xf_detalles",
                columns: new[] { "factura_detalle_id", "venta_xf_detalle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_formas_pago_codigo",
                schema: "s_catalogos",
                table: "formas_pago",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_formas_pago_codigo_sri",
                schema: "s_catalogos",
                table: "formas_pago",
                column: "codigo_sri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guias_remision_anulado_por_usuario_id",
                schema: "s_ventas",
                table: "guias_remision",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_guias_remision_empresa_tercero_id_empresa_id",
                schema: "s_ventas",
                table: "guias_remision",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_guias_remision_establecimiento_id_empresa_id",
                schema: "s_ventas",
                table: "guias_remision",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_guias_remision_punto_emision_id_establecimiento_id",
                schema: "s_ventas",
                table: "guias_remision",
                columns: new[] { "punto_emision_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_guias_remision_tipo_comprobante_id",
                schema: "s_ventas",
                table: "guias_remision",
                column: "tipo_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "IX_guias_remision_tipo_origen_guia_remision_id",
                schema: "s_ventas",
                table: "guias_remision",
                column: "tipo_origen_guia_remision_id");

            migrationBuilder.CreateIndex(
                name: "IX_guias_remision_usuario_id",
                schema: "s_ventas",
                table: "guias_remision",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_guias_remision_emision_secuencial",
                schema: "s_ventas",
                table: "guias_remision",
                columns: new[] { "punto_emision_id", "tipo_comprobante_id", "secuencial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_guias_remision_empresa_numero",
                schema: "s_ventas",
                table: "guias_remision",
                columns: new[] { "empresa_id", "numero_documento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guias_remision_detalles_guia_remision_id",
                schema: "s_ventas",
                table: "guias_remision_detalles",
                column: "guia_remision_id");

            migrationBuilder.CreateIndex(
                name: "IX_guias_remision_detalles_producto_id",
                schema: "s_ventas",
                table: "guias_remision_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_guias_remision_detalles_producto_presentacion_id",
                schema: "s_ventas",
                table: "guias_remision_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "ux_impuestos_codigo",
                schema: "s_catalogos",
                table: "impuestos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_impuestos_codigo_sri",
                schema: "s_catalogos",
                table: "impuestos",
                column: "codigo_sri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_anulado_por_usuario_id",
                schema: "s_compras",
                table: "liquidaciones_compra",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_empresa_tercero_id_empresa_id",
                schema: "s_compras",
                table: "liquidaciones_compra",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_establecimiento_id_empresa_id",
                schema: "s_compras",
                table: "liquidaciones_compra",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_punto_emision_id_establecimiento_id",
                schema: "s_compras",
                table: "liquidaciones_compra",
                columns: new[] { "punto_emision_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_tipo_comprobante_id",
                schema: "s_compras",
                table: "liquidaciones_compra",
                column: "tipo_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_usuario_id",
                schema: "s_compras",
                table: "liquidaciones_compra",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_liquidaciones_compra_emision_secuencial",
                schema: "s_compras",
                table: "liquidaciones_compra",
                columns: new[] { "punto_emision_id", "tipo_comprobante_id", "secuencial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_liquidaciones_compra_empresa_numero",
                schema: "s_compras",
                table: "liquidaciones_compra",
                columns: new[] { "empresa_id", "numero_documento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_detalles_bodega_id",
                schema: "s_compras",
                table: "liquidaciones_compra_detalles",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_detalles_liquidacion_compra_id",
                schema: "s_compras",
                table: "liquidaciones_compra_detalles",
                column: "liquidacion_compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_detalles_producto_id",
                schema: "s_compras",
                table: "liquidaciones_compra_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_detalles_producto_presentacion_id",
                schema: "s_compras",
                table: "liquidaciones_compra_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_detalles_impuestos_liquidacion_compra_~",
                schema: "s_compras",
                table: "liquidaciones_compra_detalles_impuestos",
                column: "liquidacion_compra_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_compra_detalles_impuestos_tarifa_impuesto_id",
                schema: "s_compras",
                table: "liquidaciones_compra_detalles_impuestos",
                column: "tarifa_impuesto_id");

            migrationBuilder.CreateIndex(
                name: "ux_listas_precio_empresa_base",
                schema: "s_inventario",
                table: "listas_precio",
                column: "empresa_id",
                unique: true,
                filter: "es_lista_base");

            migrationBuilder.CreateIndex(
                name: "ux_listas_precio_empresa_codigo",
                schema: "s_inventario",
                table: "listas_precio",
                columns: new[] { "empresa_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_marcas_codigo",
                schema: "s_catalogos",
                table: "marcas",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_marcas_uuid",
                schema: "s_catalogos",
                table: "marcas",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medios_pago_forma_pago_sri_id",
                schema: "s_catalogos",
                table: "medios_pago",
                column: "forma_pago_sri_id");

            migrationBuilder.CreateIndex(
                name: "ux_medios_pago_codigo",
                schema: "s_catalogos",
                table: "medios_pago",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_motivos_operacion_inventario_codigo",
                schema: "s_catalogos",
                table: "motivos_operacion_inventario",
                column: "codigo",
                unique: true,
                filter: "empresa_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_motivos_operacion_inventario_empresa_id_codigo",
                schema: "s_catalogos",
                table: "motivos_operacion_inventario",
                columns: new[] { "empresa_id", "codigo" },
                unique: true,
                filter: "empresa_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_motivos_operacion_inventario_empresa_id_tipo_operacion_nomb~",
                schema: "s_catalogos",
                table: "motivos_operacion_inventario",
                columns: new[] { "empresa_id", "tipo_operacion", "nombre" },
                unique: true,
                filter: "empresa_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_motivos_operacion_inventario_tipo_operacion_nombre",
                schema: "s_catalogos",
                table: "motivos_operacion_inventario",
                columns: new[] { "tipo_operacion", "nombre" },
                unique: true,
                filter: "empresa_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_bancarios_cobro_medio_id",
                schema: "s_bancos",
                table: "movimientos_bancarios",
                column: "cobro_medio_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_bancarios_cuenta_bancaria_id",
                schema: "s_bancos",
                table: "movimientos_bancarios",
                column: "cuenta_bancaria_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_bancarios_pago_medio_id",
                schema: "s_bancos",
                table: "movimientos_bancarios",
                column: "pago_medio_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_bancarios_tipo_movimiento_bancario_id",
                schema: "s_bancos",
                table: "movimientos_bancarios",
                column: "tipo_movimiento_bancario_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_bancarios_usuario_id",
                schema: "s_bancos",
                table: "movimientos_bancarios",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_movimientos_bancarios_reverso",
                schema: "s_bancos",
                table: "movimientos_bancarios",
                column: "movimiento_reverso_id",
                unique: true,
                filter: "movimiento_reverso_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_caja_sesion_id",
                schema: "s_tesoreria",
                table: "movimientos_caja",
                column: "caja_sesion_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_cobro_medio_id",
                schema: "s_tesoreria",
                table: "movimientos_caja",
                column: "cobro_medio_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_pago_medio_id",
                schema: "s_tesoreria",
                table: "movimientos_caja",
                column: "pago_medio_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_tipo_movimiento_caja_id",
                schema: "s_tesoreria",
                table: "movimientos_caja",
                column: "tipo_movimiento_caja_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_usuario_id",
                schema: "s_tesoreria",
                table: "movimientos_caja",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_movimientos_caja_reverso",
                schema: "s_tesoreria",
                table: "movimientos_caja",
                column: "movimiento_reverso_id",
                unique: true,
                filter: "movimiento_reverso_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_anulado_por_usuario_id",
                schema: "s_inventario",
                table: "movimientos_inventario",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_bodega_id",
                schema: "s_inventario",
                table: "movimientos_inventario",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_inventario_empresa_bodega_fecha",
                schema: "s_inventario",
                table: "movimientos_inventario",
                columns: new[] { "empresa_id", "bodega_id", "fecha_movimiento" });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_origen_tipo_id",
                schema: "s_inventario",
                table: "movimientos_inventario",
                column: "origen_tipo_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_tipo_movimiento_id",
                schema: "s_inventario",
                table: "movimientos_inventario",
                column: "tipo_movimiento_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_usuario_id",
                schema: "s_inventario",
                table: "movimientos_inventario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_movimientos_inventario_empresa_numero",
                schema: "s_inventario",
                table: "movimientos_inventario",
                columns: new[] { "empresa_id", "numero_movimiento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_movimientos_inventario_origen_bodega_tipo",
                schema: "s_inventario",
                table: "movimientos_inventario",
                columns: new[] { "empresa_id", "origen_tipo_id", "origen_id", "bodega_id", "tipo_movimiento_id" },
                unique: true,
                filter: "origen_id > 0");

            migrationBuilder.CreateIndex(
                name: "ux_movimientos_inventario_reverso",
                schema: "s_inventario",
                table: "movimientos_inventario",
                column: "movimiento_reverso_id",
                unique: true,
                filter: "movimiento_reverso_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_detalles_movimiento_inventario_id",
                schema: "s_inventario",
                table: "movimientos_inventario_detalles",
                column: "movimiento_inventario_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_detalles_producto_id",
                schema: "s_inventario",
                table: "movimientos_inventario_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_detalles_producto_presentacion_id",
                schema: "s_inventario",
                table: "movimientos_inventario_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_detalles_lotes_movimiento_inventario~",
                schema: "s_inventario",
                table: "movimientos_inventario_detalles_lotes",
                column: "movimiento_inventario_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_detalles_lotes_producto_lote_id",
                schema: "s_inventario",
                table: "movimientos_inventario_detalles_lotes",
                column: "producto_lote_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_detalles_series_producto_serie_id",
                schema: "s_inventario",
                table: "movimientos_inventario_detalles_series",
                column: "producto_serie_id");

            migrationBuilder.CreateIndex(
                name: "ux_movimientos_inventario_detalles_series_par",
                schema: "s_inventario",
                table: "movimientos_inventario_detalles_series",
                columns: new[] { "movimiento_inventario_detalle_id", "producto_serie_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_anulado_por_usuario_id",
                schema: "s_ventas",
                table: "notas_credito",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_devolucion_venta_id",
                schema: "s_ventas",
                table: "notas_credito",
                column: "devolucion_venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_empresa_tercero_id_empresa_id",
                schema: "s_ventas",
                table: "notas_credito",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_establecimiento_id_empresa_id",
                schema: "s_ventas",
                table: "notas_credito",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_factura_id",
                schema: "s_ventas",
                table: "notas_credito",
                column: "factura_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_punto_emision_id_establecimiento_id",
                schema: "s_ventas",
                table: "notas_credito",
                columns: new[] { "punto_emision_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_tipo_comprobante_id",
                schema: "s_ventas",
                table: "notas_credito",
                column: "tipo_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_usuario_id",
                schema: "s_ventas",
                table: "notas_credito",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_notas_credito_emision_secuencial",
                schema: "s_ventas",
                table: "notas_credito",
                columns: new[] { "punto_emision_id", "tipo_comprobante_id", "secuencial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_notas_credito_empresa_numero",
                schema: "s_ventas",
                table: "notas_credito",
                columns: new[] { "empresa_id", "numero_documento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_detalles_factura_detalle_id",
                schema: "s_ventas",
                table: "notas_credito_detalles",
                column: "factura_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_detalles_nota_credito_id",
                schema: "s_ventas",
                table: "notas_credito_detalles",
                column: "nota_credito_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_credito_detalles_impuestos_nota_credito_detalle_id",
                schema: "s_ventas",
                table: "notas_credito_detalles_impuestos",
                column: "nota_credito_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_debito_anulado_por_usuario_id",
                schema: "s_ventas",
                table: "notas_debito",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_debito_empresa_tercero_id_empresa_id",
                schema: "s_ventas",
                table: "notas_debito",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_debito_establecimiento_id_empresa_id",
                schema: "s_ventas",
                table: "notas_debito",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_debito_factura_id",
                schema: "s_ventas",
                table: "notas_debito",
                column: "factura_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_debito_punto_emision_id_establecimiento_id",
                schema: "s_ventas",
                table: "notas_debito",
                columns: new[] { "punto_emision_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_debito_tipo_comprobante_id",
                schema: "s_ventas",
                table: "notas_debito",
                column: "tipo_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_debito_usuario_id",
                schema: "s_ventas",
                table: "notas_debito",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_notas_debito_emision_secuencial",
                schema: "s_ventas",
                table: "notas_debito",
                columns: new[] { "punto_emision_id", "tipo_comprobante_id", "secuencial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_notas_debito_empresa_numero",
                schema: "s_ventas",
                table: "notas_debito",
                columns: new[] { "empresa_id", "numero_documento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notas_debito_detalles_nota_debito_id",
                schema: "s_ventas",
                table: "notas_debito_detalles",
                column: "nota_debito_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_debito_detalles_impuestos_nota_debito_detalle_id",
                schema: "s_ventas",
                table: "notas_debito_detalles_impuestos",
                column: "nota_debito_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_anulado_por_usuario_id",
                schema: "s_ventas",
                table: "notas_entrega",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_empresa_tercero_id_empresa_id",
                schema: "s_ventas",
                table: "notas_entrega",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_establecimiento_id_empresa_id",
                schema: "s_ventas",
                table: "notas_entrega",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_proforma_id",
                schema: "s_ventas",
                table: "notas_entrega",
                column: "proforma_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_usuario_id",
                schema: "s_ventas",
                table: "notas_entrega",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_notas_entrega_empresa_numero",
                schema: "s_ventas",
                table: "notas_entrega",
                columns: new[] { "empresa_id", "numero_nota" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_detalles_bodega_id",
                schema: "s_ventas",
                table: "notas_entrega_detalles",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_detalles_nota_entrega_id",
                schema: "s_ventas",
                table: "notas_entrega_detalles",
                column: "nota_entrega_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_detalles_precio_modificado_por_usuario_id",
                schema: "s_ventas",
                table: "notas_entrega_detalles",
                column: "precio_modificado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_detalles_producto_id",
                schema: "s_ventas",
                table: "notas_entrega_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_detalles_producto_presentacion_id",
                schema: "s_ventas",
                table: "notas_entrega_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_entrega_xf_detalles_venta_xf_detalle_id",
                schema: "s_ventas",
                table: "notas_entrega_xf_detalles",
                column: "venta_xf_detalle_id");

            migrationBuilder.CreateIndex(
                name: "ux_notas_entrega_xf_detalles_par",
                schema: "s_ventas",
                table: "notas_entrega_xf_detalles",
                columns: new[] { "nota_entrega_detalle_id", "venta_xf_detalle_id" },
                unique: true);

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
                name: "IX_operaciones_sin_sustento_operacion_sustituida_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "operacion_sustituida_id",
                unique: true,
                filter: "operacion_sustituida_id IS NOT NULL");

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

            migrationBuilder.CreateIndex(
                name: "IX_pagos_anulado_por_usuario_id",
                schema: "s_cartera",
                table: "pagos",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_empresa_tercero_id_empresa_id",
                schema: "s_cartera",
                table: "pagos",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_usuario_id",
                schema: "s_cartera",
                table: "pagos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_pagos_empresa_numero",
                schema: "s_cartera",
                table: "pagos",
                columns: new[] { "empresa_id", "numero_pago" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_aplicaciones_cuenta_por_pagar_id",
                schema: "s_cartera",
                table: "pagos_aplicaciones",
                column: "cuenta_por_pagar_id");

            migrationBuilder.CreateIndex(
                name: "ux_pagos_aplicaciones_pago_cuenta",
                schema: "s_cartera",
                table: "pagos_aplicaciones",
                columns: new[] { "pago_id", "cuenta_por_pagar_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_aplicaciones_reversos_usuario_id",
                schema: "s_cartera",
                table: "pagos_aplicaciones_reversos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_pagos_aplicaciones_reversos_aplicacion",
                schema: "s_cartera",
                table: "pagos_aplicaciones_reversos",
                column: "pago_aplicacion_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_medios_medio_pago_id",
                schema: "s_cartera",
                table: "pagos_medios",
                column: "medio_pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_medios_pago_id",
                schema: "s_cartera",
                table: "pagos_medios",
                column: "pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_periodos_contables_cerrado_por_usuario_id",
                schema: "s_contabilidad",
                table: "periodos_contables",
                column: "cerrado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_periodos_contables_empresa_anio_mes",
                schema: "s_contabilidad",
                table: "periodos_contables",
                columns: new[] { "empresa_id", "anio", "mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_permisos_codigo",
                schema: "s_seguridad",
                table: "permisos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_cuentas_cuenta_padre_id_empresa_id",
                schema: "s_contabilidad",
                table: "plan_cuentas",
                columns: new[] { "cuenta_padre_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ux_plan_cuentas_empresa_codigo",
                schema: "s_contabilidad",
                table: "plan_cuentas",
                columns: new[] { "empresa_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_categoria_producto_id_empresa_id",
                schema: "s_inventario",
                table: "productos",
                columns: new[] { "categoria_producto_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_productos_marca_id",
                schema: "s_inventario",
                table: "productos",
                column: "marca_id");

            migrationBuilder.CreateIndex(
                name: "IX_productos_unidad_medida_base_id",
                schema: "s_inventario",
                table: "productos",
                column: "unidad_medida_base_id");

            migrationBuilder.CreateIndex(
                name: "ux_productos_empresa_codigo",
                schema: "s_inventario",
                table: "productos",
                columns: new[] { "empresa_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_productos_uuid",
                schema: "s_inventario",
                table: "productos",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_productos_costos_producto",
                schema: "s_inventario",
                table: "productos_costos",
                column: "producto_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_existencias_bodega_id",
                schema: "s_inventario",
                table: "productos_existencias",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "ux_productos_existencias_producto_bodega",
                schema: "s_inventario",
                table: "productos_existencias",
                columns: new[] { "producto_id", "bodega_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_impuestos_tarifa_impuesto_id",
                schema: "s_inventario",
                table: "productos_impuestos",
                column: "tarifa_impuesto_id");

            migrationBuilder.CreateIndex(
                name: "ux_productos_impuestos_producto_tarifa",
                schema: "s_inventario",
                table: "productos_impuestos",
                columns: new[] { "producto_id", "tarifa_impuesto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_productos_lotes_producto_numero",
                schema: "s_inventario",
                table: "productos_lotes",
                columns: new[] { "producto_id", "numero_lote" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_lotes_existencias_bodega_id",
                schema: "s_inventario",
                table: "productos_lotes_existencias",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "ux_productos_lotes_existencias_lote_bodega",
                schema: "s_inventario",
                table: "productos_lotes_existencias",
                columns: new[] { "lote_id", "bodega_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_presentaciones_producto_id_empresa_id",
                schema: "s_inventario",
                table: "productos_presentaciones",
                columns: new[] { "producto_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ux_productos_presentaciones_base",
                schema: "s_inventario",
                table: "productos_presentaciones",
                column: "producto_id",
                unique: true,
                filter: "es_presentacion_base");

            migrationBuilder.CreateIndex(
                name: "ux_productos_presentaciones_empresa_barcode",
                schema: "s_inventario",
                table: "productos_presentaciones",
                columns: new[] { "empresa_id", "codigo_barras" },
                unique: true,
                filter: "codigo_barras IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_productos_presentaciones_producto_codigo",
                schema: "s_inventario",
                table: "productos_presentaciones",
                columns: new[] { "producto_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_productos_presentaciones_uuid",
                schema: "s_inventario",
                table: "productos_presentaciones",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_presentaciones_precios_lista_precio_id",
                schema: "s_inventario",
                table: "productos_presentaciones_precios",
                column: "lista_precio_id");

            migrationBuilder.CreateIndex(
                name: "ux_productos_presentaciones_precios_par",
                schema: "s_inventario",
                table: "productos_presentaciones_precios",
                columns: new[] { "producto_presentacion_id", "lista_precio_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_series_bodega_id",
                schema: "s_inventario",
                table: "productos_series",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "IX_productos_series_estado_serie_id",
                schema: "s_inventario",
                table: "productos_series",
                column: "estado_serie_id");

            migrationBuilder.CreateIndex(
                name: "IX_productos_series_producto_lote_id_producto_id",
                schema: "s_inventario",
                table: "productos_series",
                columns: new[] { "producto_lote_id", "producto_id" });

            migrationBuilder.CreateIndex(
                name: "ux_productos_series_producto_numero",
                schema: "s_inventario",
                table: "productos_series",
                columns: new[] { "producto_id", "numero_serie" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proformas_empresa_tercero_id_empresa_id",
                schema: "s_ventas",
                table: "proformas",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_proformas_establecimiento_id_empresa_id",
                schema: "s_ventas",
                table: "proformas",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_proformas_usuario_id",
                schema: "s_ventas",
                table: "proformas",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_proformas_empresa_numero",
                schema: "s_ventas",
                table: "proformas",
                columns: new[] { "empresa_id", "numero_proforma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proformas_detalles_precio_modificado_por_usuario_id",
                schema: "s_ventas",
                table: "proformas_detalles",
                column: "precio_modificado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_proformas_detalles_producto_id",
                schema: "s_ventas",
                table: "proformas_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_proformas_detalles_producto_presentacion_id",
                schema: "s_ventas",
                table: "proformas_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_proformas_detalles_proforma_id",
                schema: "s_ventas",
                table: "proformas_detalles",
                column: "proforma_id");

            migrationBuilder.CreateIndex(
                name: "IX_proformas_detalles_impuestos_proforma_detalle_id",
                schema: "s_ventas",
                table: "proformas_detalles_impuestos",
                column: "proforma_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_proformas_detalles_impuestos_tarifa_impuesto_id",
                schema: "s_ventas",
                table: "proformas_detalles_impuestos",
                column: "tarifa_impuesto_id");

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

            migrationBuilder.CreateIndex(
                name: "ux_puntos_emision_establecimiento_codigo",
                schema: "s_configuracion",
                table: "puntos_emision",
                columns: new[] { "establecimiento_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_regimenes_tributarios_codigo",
                schema: "s_catalogos",
                table: "regimenes_tributarios",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_emitidas_anulado_por_usuario_id",
                schema: "s_tributacion",
                table: "retenciones_emitidas",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_emitidas_empresa_tercero_id_empresa_id",
                schema: "s_tributacion",
                table: "retenciones_emitidas",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_emitidas_establecimiento_id_empresa_id",
                schema: "s_tributacion",
                table: "retenciones_emitidas",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ix_retenciones_emitidas_origen",
                schema: "s_tributacion",
                table: "retenciones_emitidas",
                columns: new[] { "empresa_id", "tipo_origen_retencion_emitida_id", "origen_id" });

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_emitidas_punto_emision_id_establecimiento_id",
                schema: "s_tributacion",
                table: "retenciones_emitidas",
                columns: new[] { "punto_emision_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_emitidas_tipo_comprobante_id",
                schema: "s_tributacion",
                table: "retenciones_emitidas",
                column: "tipo_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_emitidas_tipo_origen_retencion_emitida_id",
                schema: "s_tributacion",
                table: "retenciones_emitidas",
                column: "tipo_origen_retencion_emitida_id");

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_emitidas_usuario_id",
                schema: "s_tributacion",
                table: "retenciones_emitidas",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_retenciones_emitidas_emision_secuencial",
                schema: "s_tributacion",
                table: "retenciones_emitidas",
                columns: new[] { "punto_emision_id", "tipo_comprobante_id", "secuencial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_retenciones_emitidas_empresa_numero",
                schema: "s_tributacion",
                table: "retenciones_emitidas",
                columns: new[] { "empresa_id", "numero_documento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_emitidas_detalles_concepto_retencion_id",
                schema: "s_tributacion",
                table: "retenciones_emitidas_detalles",
                column: "concepto_retencion_id");

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_emitidas_detalles_retencion_emitida_id",
                schema: "s_tributacion",
                table: "retenciones_emitidas_detalles",
                column: "retencion_emitida_id");

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_recibidas_documento_recibido_sri_id_empresa_id",
                schema: "s_tributacion",
                table: "retenciones_recibidas",
                columns: new[] { "documento_recibido_sri_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ix_retenciones_recibidas_empresa_numero",
                schema: "s_tributacion",
                table: "retenciones_recibidas",
                columns: new[] { "empresa_id", "numero_documento" });

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_recibidas_empresa_tercero_id_empresa_id",
                schema: "s_tributacion",
                table: "retenciones_recibidas",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ux_retenciones_recibidas_clave_acceso",
                schema: "s_tributacion",
                table: "retenciones_recibidas",
                column: "clave_acceso",
                unique: true,
                filter: "clave_acceso IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_retenciones_recibidas_documento_sri",
                schema: "s_tributacion",
                table: "retenciones_recibidas",
                column: "documento_recibido_sri_id",
                unique: true,
                filter: "documento_recibido_sri_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_recibidas_detalles_concepto_retencion_id",
                schema: "s_tributacion",
                table: "retenciones_recibidas_detalles",
                column: "concepto_retencion_id");

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_recibidas_detalles_retencion_recibida_id",
                schema: "s_tributacion",
                table: "retenciones_recibidas_detalles",
                column: "retencion_recibida_id");

            migrationBuilder.CreateIndex(
                name: "IX_retenciones_recibidas_documentos_factura_id",
                schema: "s_tributacion",
                table: "retenciones_recibidas_documentos",
                column: "factura_id");

            migrationBuilder.CreateIndex(
                name: "ux_retenciones_recibidas_documentos_par",
                schema: "s_tributacion",
                table: "retenciones_recibidas_documentos",
                columns: new[] { "retencion_recibida_id", "factura_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_roles_codigo",
                schema: "s_seguridad",
                table: "roles",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_permisos_permiso_id",
                schema: "s_seguridad",
                table: "roles_permisos",
                column: "permiso_id");

            migrationBuilder.CreateIndex(
                name: "ux_roles_permisos_rol_permiso",
                schema: "s_seguridad",
                table: "roles_permisos",
                columns: new[] { "rol_id", "permiso_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_secuenciales_asientos_empresa_anio",
                schema: "s_contabilidad",
                table: "secuenciales_asientos",
                columns: new[] { "empresa_id", "anio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_secuenciales_comprobantes_tipo_ambiente_id",
                schema: "s_configuracion",
                table: "secuenciales_comprobantes",
                column: "tipo_ambiente_id");

            migrationBuilder.CreateIndex(
                name: "IX_secuenciales_comprobantes_tipo_comprobante_id",
                schema: "s_configuracion",
                table: "secuenciales_comprobantes",
                column: "tipo_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "ux_secuenciales_comprobantes_punto_tipo_ambiente",
                schema: "s_configuracion",
                table: "secuenciales_comprobantes",
                columns: new[] { "punto_emision_id", "tipo_comprobante_id", "tipo_ambiente_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_secuenciales_internos_establecimiento_id_empresa_id",
                schema: "s_configuracion",
                table: "secuenciales_internos",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_secuenciales_internos_tipo_documento_interno_id",
                schema: "s_configuracion",
                table: "secuenciales_internos",
                column: "tipo_documento_interno_id");

            migrationBuilder.CreateIndex(
                name: "ux_secuenciales_internos_empresa_establecimiento_tipo",
                schema: "s_configuracion",
                table: "secuenciales_internos",
                columns: new[] { "empresa_id", "establecimiento_id", "tipo_documento_interno_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tarifas_impuesto_impuesto_codigo_vigencia",
                schema: "s_catalogos",
                table: "tarifas_impuesto",
                columns: new[] { "impuesto_id", "codigo_sri", "vigente_desde" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_terceros_clave_identidad",
                schema: "s_comercial",
                table: "terceros",
                column: "clave_identidad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_terceros_tipo_identificacion_numero",
                schema: "s_comercial",
                table: "terceros",
                columns: new[] { "tipo_identificacion_id", "numero_identificacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_terceros_identificaciones_tercero",
                schema: "s_comercial",
                table: "terceros_identificaciones",
                column: "tercero_id");

            migrationBuilder.CreateIndex(
                name: "ux_terceros_identificaciones_principal",
                schema: "s_comercial",
                table: "terceros_identificaciones",
                columns: new[] { "tercero_id", "es_principal" },
                unique: true,
                filter: "es_principal");

            migrationBuilder.CreateIndex(
                name: "ux_terceros_identificaciones_tipo_numero",
                schema: "s_comercial",
                table: "terceros_identificaciones",
                columns: new[] { "tipo_identificacion_id", "numero_normalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_ambiente_codigo",
                schema: "s_catalogos",
                table: "tipos_ambiente",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_comprobante_codigo",
                schema: "s_catalogos",
                table: "tipos_comprobante",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_comprobante_codigo_sri",
                schema: "s_catalogos",
                table: "tipos_comprobante",
                column: "codigo_sri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_configuracion_contable_codigo",
                schema: "s_catalogos",
                table: "tipos_configuracion_contable",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_documento_interno_codigo",
                schema: "s_catalogos",
                table: "tipos_documento_interno",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_emision_codigo",
                schema: "s_catalogos",
                table: "tipos_emision",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_identificacion_codigo",
                schema: "s_catalogos",
                table: "tipos_identificacion",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_identificacion_codigo_sri",
                schema: "s_catalogos",
                table: "tipos_identificacion",
                column: "codigo_sri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_movimiento_bancario_codigo",
                schema: "s_catalogos",
                table: "tipos_movimiento_bancario",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_movimiento_caja_codigo",
                schema: "s_catalogos",
                table: "tipos_movimiento_caja",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_movimiento_cartera_codigo",
                schema: "s_catalogos",
                table: "tipos_movimiento_cartera",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_movimiento_cuentas_por_pagar_codigo",
                schema: "s_catalogos",
                table: "tipos_movimiento_cuentas_por_pagar",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_movimiento_inventario_codigo",
                schema: "s_catalogos",
                table: "tipos_movimiento_inventario",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_origen_asiento_codigo",
                schema: "s_catalogos",
                table: "tipos_origen_asiento",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_origen_comprobante_electronico_codigo",
                schema: "s_catalogos",
                table: "tipos_origen_comprobante_electronico",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_origen_devolucion_compra_codigo",
                schema: "s_catalogos",
                table: "tipos_origen_devolucion_compra",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_origen_devolucion_venta_codigo",
                schema: "s_catalogos",
                table: "tipos_origen_devolucion_venta",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_origen_guia_remision_codigo",
                schema: "s_catalogos",
                table: "tipos_origen_guia_remision",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_origen_movimiento_inventario_codigo",
                schema: "s_catalogos",
                table: "tipos_origen_movimiento_inventario",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipos_origen_retencion_emitida_codigo",
                schema: "s_catalogos",
                table: "tipos_origen_retencion_emitida",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_bancarias_anulado_por_usuario_id",
                schema: "s_bancos",
                table: "transferencias_bancarias",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_bancarias_cuenta_bancaria_destino_id_empresa~",
                schema: "s_bancos",
                table: "transferencias_bancarias",
                columns: new[] { "cuenta_bancaria_destino_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_bancarias_cuenta_bancaria_origen_id_empresa_~",
                schema: "s_bancos",
                table: "transferencias_bancarias",
                columns: new[] { "cuenta_bancaria_origen_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_bancarias_usuario_id",
                schema: "s_bancos",
                table: "transferencias_bancarias",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_transferencias_bancarias_empresa_numero",
                schema: "s_bancos",
                table: "transferencias_bancarias",
                columns: new[] { "empresa_id", "numero_transferencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_inventario_anulado_por_usuario_id",
                schema: "s_inventario",
                table: "transferencias_inventario",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_inventario_bodega_destino_id_establecimiento~",
                schema: "s_inventario",
                table: "transferencias_inventario",
                columns: new[] { "bodega_destino_id", "establecimiento_destino_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_inventario_bodega_origen_id_establecimiento_~",
                schema: "s_inventario",
                table: "transferencias_inventario",
                columns: new[] { "bodega_origen_id", "establecimiento_origen_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_inventario_establecimiento_destino_id_empres~",
                schema: "s_inventario",
                table: "transferencias_inventario",
                columns: new[] { "establecimiento_destino_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_inventario_establecimiento_origen_id_empresa~",
                schema: "s_inventario",
                table: "transferencias_inventario",
                columns: new[] { "establecimiento_origen_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_inventario_usuario_id",
                schema: "s_inventario",
                table: "transferencias_inventario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_transferencias_inventario_empresa_numero",
                schema: "s_inventario",
                table: "transferencias_inventario",
                columns: new[] { "empresa_id", "numero_transferencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_inventario_detalles_producto_id",
                schema: "s_inventario",
                table: "transferencias_inventario_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_inventario_detalles_producto_presentacion_id",
                schema: "s_inventario",
                table: "transferencias_inventario_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_inventario_detalles_transferencia_inventario~",
                schema: "s_inventario",
                table: "transferencias_inventario_detalles",
                column: "transferencia_inventario_id");

            migrationBuilder.CreateIndex(
                name: "ux_unidades_medida_codigo",
                schema: "s_catalogos",
                table: "unidades_medida",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_numero_identificacion",
                schema: "s_seguridad",
                table: "usuarios",
                column: "numero_identificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_configuracion_empresa_bodega_id_establecimiento_id",
                schema: "s_configuracion",
                table: "usuarios_configuracion_empresa",
                columns: new[] { "bodega_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_configuracion_empresa_empresa_id",
                schema: "s_configuracion",
                table: "usuarios_configuracion_empresa",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_configuracion_empresa_establecimiento_id_empresa_id",
                schema: "s_configuracion",
                table: "usuarios_configuracion_empresa",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_configuracion_empresa_punto_emision_id_establecimi~",
                schema: "s_configuracion",
                table: "usuarios_configuracion_empresa",
                columns: new[] { "punto_emision_id", "establecimiento_id" });

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_configuracion_empresa_usuario_empresa",
                schema: "s_configuracion",
                table: "usuarios_configuracion_empresa",
                columns: new[] { "usuario_id", "empresa_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_empresas_empresa_id",
                schema: "s_seguridad",
                table: "usuarios_empresas",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_empresas_usuario_empresa",
                schema: "s_seguridad",
                table: "usuarios_empresas",
                columns: new[] { "usuario_id", "empresa_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_empresas_establecimientos_establecimiento_id",
                schema: "s_seguridad",
                table: "usuarios_empresas_establecimientos",
                column: "establecimiento_id");

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_empresas_establecimientos_usuario_establecimiento",
                schema: "s_seguridad",
                table: "usuarios_empresas_establecimientos",
                columns: new[] { "usuario_empresa_id", "establecimiento_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_empresas_roles_rol_id",
                schema: "s_seguridad",
                table: "usuarios_empresas_roles",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_empresas_roles_usuario_empresa_rol",
                schema: "s_seguridad",
                table: "usuarios_empresas_roles",
                columns: new[] { "usuario_empresa_id", "rol_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ventas_xf_anulado_por_usuario_id",
                schema: "s_ventas",
                table: "ventas_xf",
                column: "anulado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_xf_empresa_tercero_id_empresa_id",
                schema: "s_ventas",
                table: "ventas_xf",
                columns: new[] { "empresa_tercero_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_xf_establecimiento_id_empresa_id",
                schema: "s_ventas",
                table: "ventas_xf",
                columns: new[] { "establecimiento_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_xf_usuario_id",
                schema: "s_ventas",
                table: "ventas_xf",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_ventas_xf_empresa_numero",
                schema: "s_ventas",
                table: "ventas_xf",
                columns: new[] { "empresa_id", "numero_xf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ventas_xf_detalles_bodega_id",
                schema: "s_ventas",
                table: "ventas_xf_detalles",
                column: "bodega_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_xf_detalles_precio_modificado_por_usuario_id",
                schema: "s_ventas",
                table: "ventas_xf_detalles",
                column: "precio_modificado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_xf_detalles_producto_id",
                schema: "s_ventas",
                table: "ventas_xf_detalles",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_xf_detalles_producto_presentacion_id",
                schema: "s_ventas",
                table: "ventas_xf_detalles",
                column: "producto_presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_xf_detalles_venta_xf_id",
                schema: "s_ventas",
                table: "ventas_xf_detalles",
                column: "venta_xf_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ajustes_compras_detalles_impuestos",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "ajustes_inventario_detalles",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "asientos_detalles",
                schema: "s_contabilidad");

            migrationBuilder.DropTable(
                name: "auditoria",
                schema: "s_seguridad");

            migrationBuilder.DropTable(
                name: "compras_detalles_impuestos",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "compras_recepciones_detalles_lotes",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "compras_recepciones_detalles_series",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "comprobantes_electronicos_eventos",
                schema: "s_facturacion_electronica");

            migrationBuilder.DropTable(
                name: "configuracion_cuentas",
                schema: "s_contabilidad");

            migrationBuilder.DropTable(
                name: "conversiones_control_inventario_detalles",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "conversiones_control_inventario_series",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "correcciones_datos_inventario",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "cuentas_por_cobrar_movimientos",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "cuentas_por_pagar_movimientos",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "depositos_caja_banco",
                schema: "s_tesoreria");

            migrationBuilder.DropTable(
                name: "devoluciones_compras_detalles",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "devoluciones_ventas_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "documentos_recibidos_sri_pagos",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "facturacion_electronica",
                schema: "s_configuracion");

            migrationBuilder.DropTable(
                name: "facturas_detalles_impuestos",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "facturas_formas_pago",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "facturas_notas_entrega_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "facturas_xf_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "guias_remision_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "liquidaciones_compra_detalles_impuestos",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "movimientos_inventario_detalles_lotes",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "movimientos_inventario_detalles_series",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "notas_credito_detalles_impuestos",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "notas_debito_detalles_impuestos",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "notas_entrega_xf_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "operaciones_sin_sustento_detalles",
                schema: "s_tesoreria");

            migrationBuilder.DropTable(
                name: "productos_costos",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "productos_existencias",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "productos_impuestos",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "productos_lotes_existencias",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "productos_presentaciones_precios",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "proformas_detalles_impuestos",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "proveedores_productos_equivalencias",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "retenciones_emitidas_detalles",
                schema: "s_tributacion");

            migrationBuilder.DropTable(
                name: "retenciones_recibidas_detalles",
                schema: "s_tributacion");

            migrationBuilder.DropTable(
                name: "retenciones_recibidas_documentos",
                schema: "s_tributacion");

            migrationBuilder.DropTable(
                name: "roles_permisos",
                schema: "s_seguridad");

            migrationBuilder.DropTable(
                name: "secuenciales_asientos",
                schema: "s_contabilidad");

            migrationBuilder.DropTable(
                name: "secuenciales_comprobantes",
                schema: "s_configuracion");

            migrationBuilder.DropTable(
                name: "secuenciales_internos",
                schema: "s_configuracion");

            migrationBuilder.DropTable(
                name: "terceros_identificaciones",
                schema: "s_comercial");

            migrationBuilder.DropTable(
                name: "transferencias_bancarias",
                schema: "s_bancos");

            migrationBuilder.DropTable(
                name: "transferencias_inventario_detalles",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "usuarios_configuracion_empresa",
                schema: "s_configuracion");

            migrationBuilder.DropTable(
                name: "usuarios_empresas_establecimientos",
                schema: "s_seguridad");

            migrationBuilder.DropTable(
                name: "usuarios_empresas_roles",
                schema: "s_seguridad");

            migrationBuilder.DropTable(
                name: "ajustes_compras_detalles",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "ajustes_inventario",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "compras_recepciones_detalles",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "comprobantes_electronicos",
                schema: "s_facturacion_electronica");

            migrationBuilder.DropTable(
                name: "tipos_configuracion_contable",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "conversiones_control_inventario",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "cobros_aplicaciones_reversos",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "tipos_movimiento_cartera",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "pagos_aplicaciones_reversos",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "tipos_movimiento_cuentas_por_pagar",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "guias_remision",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "liquidaciones_compra_detalles",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "productos_series",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "notas_credito_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "notas_debito_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "notas_entrega_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "ventas_xf_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "operaciones_sin_sustento",
                schema: "s_tesoreria");

            migrationBuilder.DropTable(
                name: "proformas_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "tarifas_impuesto",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "retenciones_emitidas",
                schema: "s_tributacion");

            migrationBuilder.DropTable(
                name: "conceptos_retencion",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "retenciones_recibidas",
                schema: "s_tributacion");

            migrationBuilder.DropTable(
                name: "permisos",
                schema: "s_seguridad");

            migrationBuilder.DropTable(
                name: "tipos_documento_interno",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "transferencias_inventario",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "s_seguridad");

            migrationBuilder.DropTable(
                name: "usuarios_empresas",
                schema: "s_seguridad");

            migrationBuilder.DropTable(
                name: "ajustes_compras",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "compras_detalles",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "compras_recepciones",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "movimientos_inventario_detalles",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "estados_comprobante_electronico",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "tipos_ambiente",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "tipos_emision",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "tipos_origen_comprobante_electronico",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "motivos_operacion_inventario",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "cobros_aplicaciones",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "pagos_aplicaciones",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "tipos_origen_guia_remision",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "liquidaciones_compra",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "estados_serie",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "productos_lotes",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "facturas_detalles",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "notas_credito",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "notas_debito",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "notas_entrega",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "ventas_xf",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "asientos",
                schema: "s_contabilidad");

            migrationBuilder.DropTable(
                name: "movimientos_bancarios",
                schema: "s_bancos");

            migrationBuilder.DropTable(
                name: "movimientos_caja",
                schema: "s_tesoreria");

            migrationBuilder.DropTable(
                name: "tipos_origen_retencion_emitida",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "impuestos",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "devoluciones_compras",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "compras",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "movimientos_inventario",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "cuentas_por_cobrar",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "cuentas_por_pagar",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "productos_presentaciones",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "devoluciones_ventas",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "facturas",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "periodos_contables",
                schema: "s_contabilidad");

            migrationBuilder.DropTable(
                name: "tipos_origen_asiento",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "cuentas_bancarias",
                schema: "s_bancos");

            migrationBuilder.DropTable(
                name: "tipos_movimiento_bancario",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "cajas_sesiones",
                schema: "s_tesoreria");

            migrationBuilder.DropTable(
                name: "cobros_medios",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "pagos_medios",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "tipos_movimiento_caja",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "tipos_origen_devolucion_compra",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "documentos_recibidos_sri",
                schema: "s_compras");

            migrationBuilder.DropTable(
                name: "bodegas",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "tipos_movimiento_inventario",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "tipos_origen_movimiento_inventario",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "productos",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "tipos_origen_devolucion_venta",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "proformas",
                schema: "s_ventas");

            migrationBuilder.DropTable(
                name: "puntos_emision",
                schema: "s_configuracion");

            migrationBuilder.DropTable(
                name: "cajas",
                schema: "s_tesoreria");

            migrationBuilder.DropTable(
                name: "cobros",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "medios_pago",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "pagos",
                schema: "s_cartera");

            migrationBuilder.DropTable(
                name: "tipos_comprobante",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "categorias_productos",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "marcas",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "unidades_medida",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "establecimientos",
                schema: "s_configuracion");

            migrationBuilder.DropTable(
                name: "plan_cuentas",
                schema: "s_contabilidad");

            migrationBuilder.DropTable(
                name: "formas_pago",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "empresas_terceros",
                schema: "s_comercial");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "s_seguridad");

            migrationBuilder.DropTable(
                name: "listas_precio",
                schema: "s_inventario");

            migrationBuilder.DropTable(
                name: "terceros",
                schema: "s_comercial");

            migrationBuilder.DropTable(
                name: "empresas",
                schema: "s_configuracion");

            migrationBuilder.DropTable(
                name: "tipos_identificacion",
                schema: "s_catalogos");

            migrationBuilder.DropTable(
                name: "regimenes_tributarios",
                schema: "s_catalogos");
        }
    }
}
