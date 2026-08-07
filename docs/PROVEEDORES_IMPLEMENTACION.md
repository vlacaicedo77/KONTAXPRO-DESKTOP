# Proveedores V1 — diseño e implementación final

## Alcance aprobado

Proveedor es un rol global de `Tercero`. Proveedores V1 administra un catálogo único, visible desde cualquier empresa de una sesión autenticada, y admite únicamente RUC nacional ecuatoriano.

La razón social es el nombre principal mostrado y editable solo cuando el flujo oficial habilita el modo manual. `Tercero.NombreComercial` permanece en el modelo compartido porque puede ser útil para otros módulos, pero Proveedores V1 no lo muestra ni permite modificarlo manualmente. Un valor no vacío obtenido de una fuente oficial puede conservarse; una respuesta externa vacía nunca elimina un valor existente.

Quedan fuera de alcance proveedores extranjeros, liquidaciones de compra, Compras, Cuentas por pagar, retenciones e historial de compras.

## Modelo de datos

No existe una tabla `proveedores` ni una entidad `ProveedorEmpresa`.

- `Tercero.EsProveedor` indica que el tercero pertenece al catálogo global.
- `Tercero.EstadoProveedor` mantiene el estado activo/inactivo del rol sin alterar `Tercero.Estado` ni el perfil Cliente.
- `TerceroIdentificacion` conserva el RUC y permite reutilizar una persona que ya existe por cédula.
- `EmpresaTercero` es una relación interna neutral entre empresa y tercero. No define roles; conserva configuración comercial del cliente y será el ancla multiempresa de operaciones.

La cédula y el RUC de persona natural terminado en `001` comparten `ClaveIdentidad`. Al convertir un Cliente en Proveedor se reutiliza un solo `Tercero`, se conservan ambos documentos y no se sobrescribe la configuración del Cliente.

Las condiciones de pago no pertenecen al proveedor. Condición, plazo, vencimiento y observaciones se definirán en cada futura Compra.

## Migración correctiva

`MakeSuppliersGlobal` se agrega después de `AddSuppliersV1AndThirdPartyIdentifications`; no modifica migraciones ya aplicadas. La migración:

1. agrega `es_proveedor` y `estado_proveedor` a `s_comercial.terceros`;
2. consolida allí los roles y estados empresariales existentes;
3. elimina relaciones que existían únicamente para representar Proveedor;
4. conserva las relaciones con configuración de Cliente;
5. elimina de `empresas_terceros` el rol y la configuración obsoleta de proveedor;
6. deja temporalmente el check de `empresas_terceros` como relación exclusiva de Cliente; `MakeClientRoleGlobal` elimina después ese rol redundante y vuelve neutral la relación.

## Caso de uso

`ProveedorService` usa `IDbContextFactory<KontaxDbContext>` y un contexto por operación. Las consultas requieren usuario, empresa activa y pertenencia habilitada. Crear, editar, activar o inactivar exige además `TERCEROS_GESTIONAR`, validado contra roles y permisos persistidos; inicialmente se asigna solo a `ADMINISTRADOR`.

El catálogo continúa siendo global: la empresa activa no filtra proveedores, únicamente aporta el contexto de autorización. Si la empresa cambia con un formulario abierto, este se cierra para evitar guardar con permisos de un contexto diferente.

El guardado valida el RUC, reutiliza la clave canónica, agrega la identificación RUC cuando falta, normaliza correo y teléfono, actualiza los datos compartidos y activa el rol global. Inactivar un proveedor solo cambia `EstadoProveedor`.

La consulta global ofrece búsqueda, paginación, estado, verificación y estos KPI:

- proveedores activos;
- pendientes de verificación;
- sin correo;
- sin contacto digital.

`Sin contacto digital` usa la misma regla de Clientes: no existe teléfono y el correo está vacío o contiene el correo predeterminado `cliente@kontax.com`.

## Verificación de RUC

El formulario usa exclusivamente RUC nacional de 13 dígitos. Primero busca la identidad local y después consulta GUIA/SIFAE con `PropositoConsultaIdentificacion.Proveedor`.

- Un resultado oficial completa razón social, primer correo válido y dirección.
- Un documento inválido o no encontrado no habilita modo manual.
- Tras tres respuestas de fuentes no disponibles se habilita el ingreso manual sin verificación.
- Un proveedor no verificado puede volver a verificarse al editar.
- El RUC permanece bloqueado durante la edición.

La consulta emite una constancia temporal, de un solo uso, vinculada al usuario, RUC y propósito Proveedor. `ProveedorService` obtiene de esa constancia el estado, fuente, razón social y nombre comercial oficial; el contrato de guardado no permite declarar esos datos como verificados. Una constancia offline aparece únicamente en la tercera ronda completa con GUIA y SIFAE no disponibles. Los resultados inválido, no encontrado o datos incompletos no la emiten.

## Interfaz final

El diseño conserva el lenguaje visual de Clientes: encabezado, tarjetas KPI, filtros, tabla adaptable y formulario lateral con pie fijo.

El formulario contiene únicamente:

- tipo RUC nacional;
- estado y fuente de verificación;
- número de RUC;
- razón social;
- dirección;
- correo electrónico;
- teléfono/WhatsApp;
- acciones verificar, guardar, cancelar y cerrar.

No muestra nombre comercial, crédito, plazo, observación comercial ni configuración por empresa.

La tabla contiene RUC, razón social, dirección, teléfono, correo, verificación, estado y acciones. Las columnas esenciales son RUC, razón social, estado y acciones; las demás se ocultan progresivamente en anchos menores.

## Pruebas y criterios de cierre

Las pruebas cubren creación global sin `EmpresaTercero`, reutilización cédula/RUC, conservación del perfil Cliente, catálogo estable al cambiar de empresa, estado independiente del tercero, KPI de contacto, preservación de nombre comercial oficial, rechazo sin sesión o empresa, lectura autorizada sin permiso de mantenimiento, bloqueo de mutaciones, flujo GUIA/SIFAE, tres fallos y modo manual, RUC inválido, contratos EF, conflictos de concurrencia y auditoría transaccional.

La suite opt-in `KONTAXPRO.Tests/PostgreSql` añade cinco escenarios contra PostgreSQL real: migraciones y mapeo `xmin`, conflicto de versión obsoleta, índice único de identidad canónica, rollback conjunto de mutación/auditoría e independencia de estados Cliente/Proveedor. Exige `Development` y una base exclusiva indicada por `KONTAXPRO_TEST_CONNECTION_STRING` cuyo nombre termine en `_test`; nunca utiliza `kontax_desktop`.

El agregado global `Tercero` usa la columna de sistema PostgreSQL `xmin` como token de concurrencia, expuesta como `uint Version`. El listado y el formulario conservan la versión leída y los servicios la comparan al guardar o cambiar el estado. Si otro usuario modificó el registro, la operación no sobrescribe sus datos: devuelve un conflicto, recarga el catálogo y solicita revisar la versión vigente.

Creación, asignación del rol Proveedor, actualización, verificación oficial, activación e inactivación generan eventos append-only en `s_seguridad.auditoria`. Usuario, empresa y establecimiento activos describen el contexto de ejecución; no convierten al proveedor en empresarial. Los mensajes no guardan datos personales y el evento se confirma en la misma transacción que la mutación. Rechazos y conflictos de concurrencia no dejan auditoría.

Proveedores V1 se considera funcionalmente cerrado cuando restore, build, suite completa, revisión de migraciones y aplicación en Development finalicen correctamente. Productos y Clientes deben permanecer estables y no se incorporan funcionalidades de Compras.

## Validación final — 6 de agosto de 2026

- `dotnet restore KONTAXPRO.slnx -m:1`: correcto.
- `dotnet build KONTAXPRO.slnx --no-restore -m:1`: correcto, 0 advertencias y 0 errores.
- `dotnet test KONTAXPRO.Tests/KONTAXPRO.Tests.csproj --no-build --no-restore -m:1`: 251/251 pruebas correctas, incluidas 246 rápidas y 5 relacionales.
- Suite PostgreSQL: 5/5 correctas contra `localhost/kontaxpro_integration_test`; se comprobaron migraciones, `xmin`, identidad canónica, rollback y estados independientes sin modificar `kontax_desktop`.
- La primera ejecución relacional detectó y permitió corregir el manejo de tipos de identificación nuevos con Id temporal durante el `StructuralSeeder`; una prueba de regresión comprueba ahora el seeding idempotente desde base vacía.
- EF Core: sin cambios pendientes respecto del snapshot.
- PostgreSQL Development `localhost/kontax_desktop`: migraciones `20260806034059_MakeSuppliersGlobal` y `20260806153953_MakeClientRoleGlobal` aplicadas.
- Esquema verificado: `terceros` contiene los roles y estados globales Cliente/Proveedor; `empresas_terceros` no conserva columnas de rol.
- `StructuralSeeder` aplicado el 6 de agosto de 2026: `TERCEROS_GESTIONAR` activo y asignado inicialmente solo a `ADMINISTRADOR`.
