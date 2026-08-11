using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceManualPurchasesSafely : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "compra_sustituida_id",
                schema: "s_compras",
                table: "compras",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_compras_compra_sustituida",
                schema: "s_compras",
                table: "compras",
                column: "compra_sustituida_id",
                unique: true,
                filter: "compra_sustituida_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_compras_compras_compra_sustituida_id",
                schema: "s_compras",
                table: "compras",
                column: "compra_sustituida_id",
                principalSchema: "s_compras",
                principalTable: "compras",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_compras_compra_sustituida_id",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.DropIndex(
                name: "ux_compras_compra_sustituida",
                schema: "s_compras",
                table: "compras");

            migrationBuilder.DropColumn(
                name: "compra_sustituida_id",
                schema: "s_compras",
                table: "compras");
        }
    }
}
