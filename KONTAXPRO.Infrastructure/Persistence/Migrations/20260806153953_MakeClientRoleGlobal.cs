using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeClientRoleGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_terceros_consumidor_final_protegido",
                schema: "s_comercial",
                table: "terceros");

            migrationBuilder.DropCheckConstraint(
                name: "ck_empresas_terceros_tipo",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.AddColumn<bool>(
                name: "es_cliente",
                schema: "s_comercial",
                table: "terceros",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "estado_cliente",
                schema: "s_comercial",
                table: "terceros",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE s_comercial.terceros AS tercero
                SET
                    es_cliente = TRUE,
                    estado_cliente = CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM s_comercial.empresas_terceros AS relacion_activa
                            WHERE relacion_activa.tercero_id = tercero.id
                              AND relacion_activa.estado = 1)
                            THEN 1
                        ELSE 0
                    END
                WHERE EXISTS (
                    SELECT 1
                    FROM s_comercial.empresas_terceros AS relacion
                    WHERE relacion.tercero_id = tercero.id
                      AND relacion.es_cliente);

                UPDATE s_comercial.terceros
                SET es_cliente = TRUE,
                    estado_cliente = 1
                WHERE numero_identificacion = '9999999999999';
                """);

            migrationBuilder.DropColumn(
                name: "es_cliente",
                schema: "s_comercial",
                table: "empresas_terceros");

            migrationBuilder.AddCheckConstraint(
                name: "ck_terceros_consumidor_final_protegido",
                schema: "s_comercial",
                table: "terceros",
                sql: "numero_identificacion <> '9999999999999' OR (razon_social = 'CONSUMIDOR FINAL' AND es_cliente AND estado_cliente = 1 AND estado = 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_terceros_estado_cliente",
                schema: "s_comercial",
                table: "terceros",
                sql: "estado_cliente IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_terceros_consumidor_final_protegido",
                schema: "s_comercial",
                table: "terceros");

            migrationBuilder.DropCheckConstraint(
                name: "ck_terceros_estado_cliente",
                schema: "s_comercial",
                table: "terceros");

            migrationBuilder.AddColumn<bool>(
                name: "es_cliente",
                schema: "s_comercial",
                table: "empresas_terceros",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE s_comercial.empresas_terceros
                SET es_cliente = TRUE;
                """);

            migrationBuilder.DropColumn(
                name: "es_cliente",
                schema: "s_comercial",
                table: "terceros");

            migrationBuilder.DropColumn(
                name: "estado_cliente",
                schema: "s_comercial",
                table: "terceros");

            migrationBuilder.AddCheckConstraint(
                name: "ck_terceros_consumidor_final_protegido",
                schema: "s_comercial",
                table: "terceros",
                sql: "numero_identificacion <> '9999999999999' OR (razon_social = 'CONSUMIDOR FINAL' AND estado = 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_empresas_terceros_tipo",
                schema: "s_comercial",
                table: "empresas_terceros",
                sql: "es_cliente");
        }
    }
}
