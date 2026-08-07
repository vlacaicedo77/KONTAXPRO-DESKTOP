# Pruebas de integración PostgreSQL

La suite relacional de `KONTAXPRO.Tests/PostgreSql` es opt-in. Complementa las pruebas unitarias rápidas y comprueba migraciones, el token `xmin`, índices únicos, rollback transaccional y la independencia de los roles Cliente/Proveedor sobre PostgreSQL real.

## Guardas obligatorias

La suite no reutiliza `KONTAXPRO_CONNECTION_STRING`. Solo acepta `KONTAXPRO_TEST_CONNECTION_STRING` y valida antes de conectarse que:

- `DOTNET_ENVIRONMENT` sea `Development`;
- el nombre de la base termine en `_test`;
- el nombre no sea `kontax_desktop` ni contenga `prod`.

La suite aplica las migraciones oficiales y el `StructuralSeeder`, pero no elimina la base. Cada prueba usa identificadores sintéticos únicos y limpia los registros que confirma. Nunca deben configurarse credenciales o documentos reales dentro del repositorio.

El contexto exclusivo de pruebas omite `PendingModelChangesWarning` al invocar `MigrateAsync`, debido a la comparación en tiempo de ejecución del token de sistema `xmin`. Esta excepción no se aplica a Desktop ni Infrastructure: el cierre continúa exigiendo `dotnet ef migrations has-pending-model-changes` por separado.

## Configuración local

Ejemplo para la sesión actual de PowerShell, reemplazando únicamente credenciales locales:

```powershell
$env:DOTNET_ENVIRONMENT = "Development"
$env:KONTAXPRO_TEST_CONNECTION_STRING = "Host=localhost;Database=kontaxpro_integration_test;Username=USUARIO_LOCAL;Password=CLAVE_LOCAL"
```

La base debe existir y ser exclusiva para pruebas. El usuario necesita permisos para aplicar migraciones y manipular datos dentro de ella.

## Ejecución

```powershell
dotnet test KONTAXPRO.Tests/KONTAXPRO.Tests.csproj --filter "Category=PostgreSQL"
```

Sin la variable de conexión, estas pruebas aparecen como omitidas de forma explícita. Una conexión configurada con ambiente o nombre inseguro hace fallar la suite antes de aplicar migraciones.

GUIA/SIFAE reales y la vista WPF permanecen fuera de esta suite: se validan mediante el procedimiento manual controlado para no depender de red, credenciales o documentos personales en la ejecución automatizada.

## Validación local

El 6 de agosto de 2026 se creó `localhost/kontaxpro_integration_test`, separada de `kontax_desktop`, y se ejecutaron correctamente los cinco escenarios. La suite completa finalizó con 251/251 pruebas correctas y ninguna omitida. La primera ejecución detectó un defecto del `StructuralSeeder` con tipos de identificación nuevos aún sin Id definitivo; la corrección quedó cubierta por una prueba de regresión idempotente.
