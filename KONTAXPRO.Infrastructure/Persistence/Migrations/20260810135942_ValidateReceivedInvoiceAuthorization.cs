using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ValidateReceivedInvoiceAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_recibidos_sri_validacion",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_recibidos_sri_validacion",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                sql: "estado_validacion IN ('AUTORIZADO_SRI', 'VALIDADO_LOCALMENTE', 'ADVERTENCIA', 'RECHAZADO')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_recibidos_sri_validacion",
                schema: "s_compras",
                table: "documentos_recibidos_sri");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_recibidos_sri_validacion",
                schema: "s_compras",
                table: "documentos_recibidos_sri",
                sql: "estado_validacion IN ('VALIDADO_LOCALMENTE', 'ADVERTENCIA', 'RECHAZADO')");
        }
    }
}
