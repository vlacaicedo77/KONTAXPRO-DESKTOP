using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAccountingDetailCompanyIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_asientos_detalles_asientos_asiento_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_asientos_detalles_empresas_terceros_empresa_tercero_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_asientos_detalles_plan_cuentas_cuenta_contable_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.DropIndex(
                name: "IX_asientos_detalles_cuenta_contable_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.DropIndex(
                name: "IX_asientos_detalles_empresa_tercero_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.AddColumn<long>(
                name: "empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE s_contabilidad.asientos_detalles AS d
                SET empresa_id = a.empresa_id
                FROM s_contabilidad.asientos AS a
                WHERE a.id = d.asiento_id;

                DO $accounting_isolation$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM s_contabilidad.asientos_detalles d
                        LEFT JOIN s_contabilidad.plan_cuentas c
                          ON c.id = d.cuenta_contable_id
                         AND c.empresa_id = d.empresa_id
                        LEFT JOIN s_comercial.empresas_terceros et
                          ON et.id = d.empresa_tercero_id
                         AND et.empresa_id = d.empresa_id
                        WHERE d.empresa_id IS NULL
                           OR c.id IS NULL
                           OR (d.empresa_tercero_id IS NOT NULL AND et.id IS NULL)
                    ) THEN
                        RAISE EXCEPTION 'Existen detalles contables con cuentas o terceros pertenecientes a otra empresa.';
                    END IF;
                END
                $accounting_isolation$;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_asientos_id_empresa",
                schema: "s_contabilidad",
                table: "asientos",
                columns: new[] { "id", "empresa_id" });

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

            migrationBuilder.AddForeignKey(
                name: "FK_asientos_detalles_asientos_asiento_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                columns: new[] { "asiento_id", "empresa_id" },
                principalSchema: "s_contabilidad",
                principalTable: "asientos",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_asientos_detalles_empresas_terceros_empresa_tercero_id_empr~",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                columns: new[] { "empresa_tercero_id", "empresa_id" },
                principalSchema: "s_comercial",
                principalTable: "empresas_terceros",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_asientos_detalles_plan_cuentas_cuenta_contable_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                columns: new[] { "cuenta_contable_id", "empresa_id" },
                principalSchema: "s_contabilidad",
                principalTable: "plan_cuentas",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_asientos_detalles_asientos_asiento_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_asientos_detalles_empresas_terceros_empresa_tercero_id_empr~",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_asientos_detalles_plan_cuentas_cuenta_contable_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.DropIndex(
                name: "IX_asientos_detalles_asiento_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.DropIndex(
                name: "IX_asientos_detalles_cuenta_contable_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.DropIndex(
                name: "IX_asientos_detalles_empresa_tercero_id_empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_asientos_id_empresa",
                schema: "s_contabilidad",
                table: "asientos");

            migrationBuilder.DropColumn(
                name: "empresa_id",
                schema: "s_contabilidad",
                table: "asientos_detalles");

            migrationBuilder.CreateIndex(
                name: "IX_asientos_detalles_cuenta_contable_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                column: "cuenta_contable_id");

            migrationBuilder.CreateIndex(
                name: "IX_asientos_detalles_empresa_tercero_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                column: "empresa_tercero_id");

            migrationBuilder.AddForeignKey(
                name: "FK_asientos_detalles_asientos_asiento_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                column: "asiento_id",
                principalSchema: "s_contabilidad",
                principalTable: "asientos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_asientos_detalles_empresas_terceros_empresa_tercero_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                column: "empresa_tercero_id",
                principalSchema: "s_comercial",
                principalTable: "empresas_terceros",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_asientos_detalles_plan_cuentas_cuenta_contable_id",
                schema: "s_contabilidad",
                table: "asientos_detalles",
                column: "cuenta_contable_id",
                principalSchema: "s_contabilidad",
                principalTable: "plan_cuentas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
