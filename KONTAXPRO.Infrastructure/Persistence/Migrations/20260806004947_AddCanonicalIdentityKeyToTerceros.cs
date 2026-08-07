using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KONTAXPRO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalIdentityKeyToTerceros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "clave_identidad",
                schema: "s_comercial",
                table: "terceros",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH documentos AS (
                    SELECT
                        tercero.id,
                        tipo.codigo,
                        upper(regexp_replace(trim(tercero.numero_identificacion), '[[:space:]]', '', 'g')) AS numero
                    FROM s_comercial.terceros AS tercero
                    INNER JOIN s_catalogos.tipos_identificacion AS tipo
                        ON tipo.id = tercero.tipo_identificacion_id
                ), identidades AS (
                    SELECT
                        documento.id,
                        CASE
                            WHEN documento.codigo = 'CEDULA'
                                AND documento.numero ~ '^[0-9]{10}$'
                                AND (
                                    substring(documento.numero, 1, 2)::integer BETWEEN 1 AND 24
                                    OR (
                                        substring(documento.numero, 1, 2)::integer = 30
                                        AND substring(documento.numero, 3, 1)::integer IN (4, 5)
                                    )
                                )
                                AND (
                                    substring(documento.numero, 1, 2)::integer = 30
                                    OR substring(documento.numero, 3, 1)::integer < 7
                                )
                                AND (
                                    10 - (
                                        SELECT sum(
                                            CASE
                                                WHEN position % 2 = 1 THEN
                                                    CASE
                                                        WHEN substring(documento.numero, position, 1)::integer * 2 > 9
                                                            THEN substring(documento.numero, position, 1)::integer * 2 - 9
                                                        ELSE substring(documento.numero, position, 1)::integer * 2
                                                    END
                                                ELSE substring(documento.numero, position, 1)::integer
                                            END)
                                        FROM generate_series(1, 9) AS series(position)
                                    ) % 10
                                ) % 10 = substring(documento.numero, 10, 1)::integer
                                THEN 'NAT:' || documento.numero
                            WHEN documento.codigo = 'CEDULA'
                                THEN 'CED:' || documento.numero
                            WHEN documento.codigo = 'RUC'
                                AND documento.numero ~ '^[0-9]{13}$'
                                AND right(documento.numero, 3) = '001'
                                AND (
                                    substring(documento.numero, 1, 2)::integer BETWEEN 1 AND 24
                                    OR (
                                        substring(documento.numero, 1, 2)::integer = 30
                                        AND substring(documento.numero, 3, 1)::integer IN (4, 5)
                                    )
                                )
                                AND substring(documento.numero, 3, 1)::integer < 6
                                AND (
                                    10 - (
                                        SELECT sum(
                                            CASE
                                                WHEN position % 2 = 1 THEN
                                                    CASE
                                                        WHEN substring(documento.numero, position, 1)::integer * 2 > 9
                                                            THEN substring(documento.numero, position, 1)::integer * 2 - 9
                                                        ELSE substring(documento.numero, position, 1)::integer * 2
                                                    END
                                                ELSE substring(documento.numero, position, 1)::integer
                                            END)
                                        FROM generate_series(1, 9) AS series(position)
                                    ) % 10
                                ) % 10 = substring(documento.numero, 10, 1)::integer
                                THEN 'NAT:' || left(documento.numero, 10)
                            WHEN documento.codigo = 'RUC'
                                THEN 'RUC:' || documento.numero
                            WHEN documento.codigo = 'PASAPORTE'
                                THEN 'PAS:' || documento.numero
                            WHEN documento.codigo = 'EXTERIOR'
                                THEN 'EXT:' || documento.numero
                            WHEN documento.codigo = 'CONSUMIDOR_FINAL'
                                THEN 'CF:' || documento.numero
                            ELSE NULL
                        END AS clave
                    FROM documentos AS documento
                )
                UPDATE s_comercial.terceros AS tercero
                SET clave_identidad = identidad.clave
                FROM identidades AS identidad
                WHERE identidad.id = tercero.id;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    ids_conflictivos text;
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM s_comercial.terceros
                        WHERE clave_identidad IS NULL)
                    THEN
                        SELECT string_agg(id::text, ', ' ORDER BY id)
                        INTO ids_conflictivos
                        FROM s_comercial.terceros
                        WHERE clave_identidad IS NULL;

                        RAISE EXCEPTION
                            'No se pudo construir la identidad canónica de los terceros con Id interno: %.',
                            ids_conflictivos;
                    END IF;

                    SELECT string_agg(grupo.ids, '; ' ORDER BY grupo.ids)
                    INTO ids_conflictivos
                    FROM (
                        SELECT string_agg(id::text, ', ' ORDER BY id) AS ids
                        FROM s_comercial.terceros
                        GROUP BY clave_identidad
                        HAVING count(*) > 1
                    ) AS grupo;

                    IF ids_conflictivos IS NOT NULL THEN
                        RAISE EXCEPTION
                            'Existen identidades canónicas duplicadas. Consolide manualmente los terceros con Id interno: %.',
                            ids_conflictivos;
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "clave_identidad",
                schema: "s_comercial",
                table: "terceros",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(24)",
                oldMaxLength: 24,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_terceros_clave_identidad",
                schema: "s_comercial",
                table: "terceros",
                column: "clave_identidad",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_terceros_clave_identidad",
                schema: "s_comercial",
                table: "terceros");

            migrationBuilder.DropColumn(
                name: "clave_identidad",
                schema: "s_comercial",
                table: "terceros");
        }
    }
}
