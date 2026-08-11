using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreservePurchaseSupplierSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "proveedor_identificacion",
                schema: "s_compras",
                table: "compras",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proveedor_razon_social",
                schema: "s_compras",
                table: "compras",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE s_compras.compras AS c
                SET proveedor_identificacion = COALESCE(
                        (SELECT d.ruc_emisor
                         FROM s_compras.documentos_recibidos_sri AS d
                         WHERE d.id = c.documento_recibido_sri_id),
                        (SELECT t.numero_identificacion
                         FROM s_comercial.empresas_terceros AS et
                         JOIN s_comercial.terceros AS t ON t.id = et.tercero_id
                         WHERE et.id = c.empresa_tercero_id
                           AND et.empresa_id = c.empresa_id)),
                    proveedor_razon_social = COALESCE(
                        (SELECT d.razon_social_emisor
                         FROM s_compras.documentos_recibidos_sri AS d
                         WHERE d.id = c.documento_recibido_sri_id),
                        (SELECT t.razon_social
                         FROM s_comercial.empresas_terceros AS et
                         JOIN s_comercial.terceros AS t ON t.id = et.tercero_id
                         WHERE et.id = c.empresa_tercero_id
                           AND et.empresa_id = c.empresa_id));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "proveedor_identificacion",
                schema: "s_compras",
                table: "compras",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "proveedor_razon_social",
                schema: "s_compras",
                table: "compras",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "proveedor_identificacion",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.DropColumn(
                name: "proveedor_razon_social",
                schema: "s_compras",
                table: "compras");
        }
    }
}
