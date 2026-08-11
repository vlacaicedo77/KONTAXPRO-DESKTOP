using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManageUnsupportedOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "operacion_sustituida_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sin_sustento_operacion_sustituida_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "operacion_sustituida_id",
                unique: true,
                filter: "operacion_sustituida_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_operaciones_sin_sustento_operaciones_sin_sustento_operacion~",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento",
                column: "operacion_sustituida_id",
                principalSchema: "s_tesoreria",
                principalTable: "operaciones_sin_sustento",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_operaciones_sin_sustento_operaciones_sin_sustento_operacion~",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento");

            migrationBuilder.DropIndex(
                name: "IX_operaciones_sin_sustento_operacion_sustituida_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento");

            migrationBuilder.DropColumn(
                name: "operacion_sustituida_id",
                schema: "s_tesoreria",
                table: "operaciones_sin_sustento");
        }
    }
}
