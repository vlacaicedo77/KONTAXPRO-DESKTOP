using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePurchaseReceiptLineage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_compras_detalles_compra_detall~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
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

            migrationBuilder.AddColumn<long>(
                name: "compra_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE s_compras.compras_recepciones_detalles AS d
                SET compra_id = r.compra_id
                FROM s_compras.compras_recepciones AS r
                WHERE r.id = d.compra_recepcion_id;

                DO $lineage$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM s_compras.compras_recepciones_detalles rd
                        LEFT JOIN s_compras.compras_detalles d
                          ON d.id = rd.compra_detalle_id
                         AND d.compra_id = rd.compra_id
                         AND d.empresa_id = rd.empresa_id
                        WHERE rd.compra_id IS NULL OR d.id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Existen detalles de recepción enlazados a una línea de otra compra.';
                    END IF;
                END
                $lineage$;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "compra_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_compras_recepciones_id_compra_empresa",
                schema: "s_compras",
                table: "compras_recepciones",
                columns: new[] { "id", "compra_id", "empresa_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_compras_detalles_id_compra_empresa",
                schema: "s_compras",
                table: "compras_detalles",
                columns: new[] { "id", "compra_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_compra_detalle_id_compra_id_em~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_detalle_id", "compra_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_recepciones_detalles_compra_recepcion_id_compra_id_~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "compra_id", "empresa_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_compras_detalles_compra_detall~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_detalle_id", "compra_id", "empresa_id" },
                principalSchema: "s_compras",
                principalTable: "compras_detalles",
                principalColumns: new[] { "id", "compra_id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                schema: "s_compras",
                table: "compras_recepciones_detalles",
                columns: new[] { "compra_recepcion_id", "compra_id", "empresa_id" },
                principalSchema: "s_compras",
                principalTable: "compras_recepciones",
                principalColumns: new[] { "id", "compra_id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_compras_detalles_compra_detall~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_recepciones_detalles_compras_recepciones_compra_rec~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_compra_detalle_id_compra_id_em~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropIndex(
                name: "IX_compras_recepciones_detalles_compra_recepcion_id_compra_id_~",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_compras_recepciones_id_compra_empresa",
                schema: "s_compras",
                table: "compras_recepciones");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_compras_detalles_id_compra_empresa",
                schema: "s_compras",
                table: "compras_detalles");

            migrationBuilder.DropColumn(
                name: "compra_id",
                schema: "s_compras",
                table: "compras_recepciones_detalles");

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
        }
    }
}
