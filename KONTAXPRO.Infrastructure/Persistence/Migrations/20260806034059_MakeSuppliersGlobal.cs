using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeSuppliersGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_empresas_terceros_estado_proveedor",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.DropCheckConstraint(
                name: "ck_empresas_terceros_plazo_credito_proveedor",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.DropCheckConstraint(
                name: "ck_empresas_terceros_tipo",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.AddColumn<bool>(
                name: "es_proveedor",
                schema: "s_comercial",
                table: "terceros",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "estado_proveedor",
                schema: "s_comercial",
                table: "terceros",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE s_comercial.terceros AS t
                SET es_proveedor = TRUE,
                    estado_proveedor = CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM s_comercial.empresas_terceros AS et
                            WHERE et.tercero_id = t.id
                              AND et.es_proveedor
                              AND et.estado_proveedor = 1)
                        THEN 1
                        ELSE 0
                    END
                WHERE EXISTS (
                    SELECT 1
                    FROM s_comercial.empresas_terceros AS et
                    WHERE et.tercero_id = t.id
                      AND et.es_proveedor);

                DELETE FROM s_comercial.empresas_terceros
                WHERE es_proveedor
                  AND NOT es_cliente;
                """);

            migrationBuilder.DropColumn(
                name: "credito_proveedor_habilitado",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.DropColumn(
                name: "es_proveedor",
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

            migrationBuilder.AddCheckConstraint(
                name: "ck_terceros_estado_proveedor",
                schema: "s_comercial",
                table: "terceros",
                sql: "estado_proveedor IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_empresas_terceros_tipo",
                schema: "s_comercial",
                table: "empresas_terceros",
                sql: "es_cliente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_terceros_estado_proveedor",
                schema: "s_comercial",
                table: "terceros");

            migrationBuilder.DropCheckConstraint(
                name: "ck_empresas_terceros_tipo",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.DropColumn(
                name: "es_proveedor",
                schema: "s_comercial",
                table: "terceros");

            migrationBuilder.DropColumn(
                name: "estado_proveedor",
                schema: "s_comercial",
                table: "terceros");

            migrationBuilder.AddColumn<bool>(
                name: "credito_proveedor_habilitado",
                schema: "s_comercial",
                table: "empresas_terceros",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "es_proveedor",
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

            migrationBuilder.AddCheckConstraint(
                name: "ck_empresas_terceros_tipo",
                schema: "s_comercial",
                table: "empresas_terceros",
                sql: "es_cliente OR es_proveedor");
        }
    }
}
