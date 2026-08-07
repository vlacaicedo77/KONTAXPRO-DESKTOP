using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSuppliersV1AndThirdPartyIdentifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "credito_proveedor_habilitado",
                schema: "s_comercial",
                table: "empresas_terceros",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "estado_proveedor",
                schema: "s_comercial",
                table: "empresas_terceros",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "observacion_proveedor",
                schema: "s_comercial",
                table: "empresas_terceros",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "plazo_credito_proveedor_dias",
                schema: "s_comercial",
                table: "empresas_terceros",
                type: "integer",
                nullable: true);

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

            migrationBuilder.AddCheckConstraint(
                name: "ck_empresas_terceros_estado_proveedor",
                schema: "s_comercial",
                table: "empresas_terceros",
                sql: "estado_proveedor IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_empresas_terceros_plazo_credito_proveedor",
                schema: "s_comercial",
                table: "empresas_terceros",
                sql: "(NOT credito_proveedor_habilitado AND (plazo_credito_proveedor_dias IS NULL OR plazo_credito_proveedor_dias = 0)) OR (credito_proveedor_habilitado AND plazo_credito_proveedor_dias BETWEEN 1 AND 3650)");

            migrationBuilder.Sql(
                """
                INSERT INTO s_comercial.terceros_identificaciones
                    (tercero_id, tipo_identificacion_id,
                     numero_identificacion, numero_normalizado,
                     es_principal, estado_verificacion,
                     fuente_verificacion, verificado_at,
                     estado, created_at, updated_at)
                SELECT
                    id,
                    tipo_identificacion_id,
                    numero_identificacion,
                    UPPER(regexp_replace(trim(numero_identificacion), '\s+', '', 'g')),
                    TRUE,
                    estado_verificacion,
                    fuente_verificacion,
                    verificado_at,
                    estado,
                    created_at,
                    updated_at
                FROM s_comercial.terceros;
                """);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "terceros_identificaciones",
                schema: "s_comercial");

            migrationBuilder.DropCheckConstraint(
                name: "ck_empresas_terceros_estado_proveedor",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.DropCheckConstraint(
                name: "ck_empresas_terceros_plazo_credito_proveedor",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.DropColumn(
                name: "credito_proveedor_habilitado",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.DropColumn(
                name: "estado_proveedor",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.DropColumn(
                name: "observacion_proveedor",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.DropColumn(
                name: "plazo_credito_proveedor_dias",
                schema: "s_comercial",
                table: "empresas_terceros");
        }
    }
}
