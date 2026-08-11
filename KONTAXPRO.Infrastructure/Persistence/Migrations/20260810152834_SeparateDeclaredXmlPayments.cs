using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeparateDeclaredXmlPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_pagos_compras_compra_id",
                schema: "s_compras", table: "compras_pagos");
            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_pagos_plazo", schema: "s_compras",
                table: "compras_pagos");
            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_pagos_valor", schema: "s_compras",
                table: "compras_pagos");
            migrationBuilder.RenameTable(
                name: "compras_pagos", schema: "s_compras",
                newName: "documentos_recibidos_sri_pagos",
                newSchema: "s_compras");
            migrationBuilder.Sql(
                "ALTER TABLE s_compras.documentos_recibidos_sri_pagos RENAME CONSTRAINT \"PK_compras_pagos\" TO \"PK_documentos_recibidos_sri_pagos\";");
            migrationBuilder.RenameColumn(
                name: "compra_id", schema: "s_compras",
                table: "documentos_recibidos_sri_pagos",
                newName: "documento_recibido_sri_id");
            migrationBuilder.RenameIndex(
                name: "IX_compras_pagos_compra_id",
                schema: "s_compras",
                table: "documentos_recibidos_sri_pagos",
                newName: "IX_documentos_recibidos_sri_pagos_documento_recibido_sri_id");
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM s_compras.documentos_recibidos_sri_pagos AS p
                        JOIN s_compras.compras AS c
                          ON c.id = p.documento_recibido_sri_id
                        WHERE c.documento_recibido_sri_id IS NULL)
                    THEN
                        RAISE EXCEPTION 'Existe un pago XML sin documento SRI asociado.';
                    END IF;
                END $$;

                UPDATE s_compras.documentos_recibidos_sri_pagos AS p
                SET documento_recibido_sri_id = c.documento_recibido_sri_id
                FROM s_compras.compras AS c
                WHERE c.id = p.documento_recibido_sri_id;
                """);
            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_sri_pagos_plazo", schema: "s_compras",
                table: "documentos_recibidos_sri_pagos",
                sql: "plazo IS NULL OR plazo >= 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_sri_pagos_valor", schema: "s_compras",
                table: "documentos_recibidos_sri_pagos", sql: "valor > 0");
            migrationBuilder.AddForeignKey(
                name: "FK_documentos_recibidos_sri_pagos_documentos_recibidos_sri_doc~",
                schema: "s_compras", table: "documentos_recibidos_sri_pagos",
                column: "documento_recibido_sri_id",
                principalSchema: "s_compras",
                principalTable: "documentos_recibidos_sri",
                principalColumn: "id", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documentos_recibidos_sri_pagos_documentos_recibidos_sri_doc~",
                schema: "s_compras", table: "documentos_recibidos_sri_pagos");
            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_sri_pagos_plazo", schema: "s_compras",
                table: "documentos_recibidos_sri_pagos");
            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_sri_pagos_valor", schema: "s_compras",
                table: "documentos_recibidos_sri_pagos");
            migrationBuilder.Sql(
                """
                UPDATE s_compras.documentos_recibidos_sri_pagos AS p
                SET documento_recibido_sri_id = c.id
                FROM s_compras.compras AS c
                WHERE c.documento_recibido_sri_id = p.documento_recibido_sri_id;
                """);
            migrationBuilder.RenameIndex(
                name: "IX_documentos_recibidos_sri_pagos_documento_recibido_sri_id",
                schema: "s_compras",
                table: "documentos_recibidos_sri_pagos",
                newName: "IX_compras_pagos_compra_id");
            migrationBuilder.RenameColumn(
                name: "documento_recibido_sri_id", schema: "s_compras",
                table: "documentos_recibidos_sri_pagos", newName: "compra_id");
            migrationBuilder.RenameTable(
                name: "documentos_recibidos_sri_pagos", schema: "s_compras",
                newName: "compras_pagos", newSchema: "s_compras");
            migrationBuilder.Sql(
                "ALTER TABLE s_compras.compras_pagos RENAME CONSTRAINT \"PK_documentos_recibidos_sri_pagos\" TO \"PK_compras_pagos\";");
            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_pagos_plazo", schema: "s_compras",
                table: "compras_pagos", sql: "plazo IS NULL OR plazo >= 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_pagos_valor", schema: "s_compras",
                table: "compras_pagos", sql: "valor > 0");
            migrationBuilder.AddForeignKey(
                name: "FK_compras_pagos_compras_compra_id",
                schema: "s_compras", table: "compras_pagos",
                column: "compra_id", principalSchema: "s_compras",
                principalTable: "compras", principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
