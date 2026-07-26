# Auditoría previa a la reconstrucción de base de datos

Fecha de auditoría: 2026-07-26  
Solución: `KONTAXPRO.slnx`  
Alcance: inspección de solo lectura del modelo actual. No se eliminó la base, no se generaron migraciones y no se hicieron cambios masivos.

## 1. Estado base

Se leyó `AGENTS.md` completo y se tomó como fuente de verdad por encima del modelo existente.

Comando ejecutado:

```text
dotnet build KONTAXPRO.slnx
```

Resultado:

```text
Compilación correcta.
0 advertencias
0 errores
```

La solución contiene los cuatro proyectos aprobados y respeta sus referencias principales:

- `KONTAXPRO.Domain`
- `KONTAXPRO.Application`
- `KONTAXPRO.Infrastructure`
- `KONTAXPRO.Desktop`

El directorio inspeccionado no contiene metadatos Git accesibles (`git status` responde que no es un repositorio), por lo que no fue posible usar Git para distinguir cambios previos del usuario.

## 2. Inventario localizado

### Persistencia

- DbContext: `KONTAXPRO.Infrastructure/Persistence/KontaxDbContext.cs`.
- Configuraciones EF: `KONTAXPRO.Infrastructure/Persistence/Configurations/*.cs`.
- Seeder actual: `KONTAXPRO.Infrastructure/Persistence/DatabaseSeeder.cs`.
- Migraciones: no existe directorio ni clase de migración en el repositorio inspeccionado.
- Registro y arranque: `KONTAXPRO.Desktop/App.xaml.cs`.
- Conexión: `KONTAXPRO.Desktop/appsettings.json`.

`KontaxDbContext` usa `ApplyConfigurationsFromAssembly`, lo cual es reutilizable, y actualmente expone 26 `DbSet`. El modelo solo cubre partes de catálogos, configuración, seguridad e inventario. No contiene los módulos definitivos comercial, ventas, compras, cartera, tesorería, bancos, contabilidad, tributación ni facturación electrónica.

### Domain

Entidades localizadas:

- Catálogos: `Marca`, `RegimenTributario`, `TarifaImpuesto`, `TipoIdentificacion`, `UnidadMedida`.
- Configuración: `ConfiguracionInventario`, `Empresa`, `Establecimiento`, `PuntoEmision`.
- Seguridad: `Usuario`, `Rol`, `Permiso`, `RolPermiso`, `UsuarioEmpresa`, `UsuarioEmpresaRol`, `UsuarioEmpresaConfiguracion`.
- Inventario: `Bodega`, `CategoriaProducto`, `ListaPrecio`, `Producto`, `ProductoPresentacion`, `ProductoPresentacionPrecio`, `ProductoCosto`, `ProductoExistencia`, `ProductoLote`, `ProductoLoteExistencia`, `ProductoSerie`, `MovimientoInventario`, `MovimientoInventarioDetalle`.

No hay entidades para la mayoría del modelo aprobado. Tampoco se localizaron enums o códigos de dominio que centralicen los workflows; predominan cadenas literales en entidades y servicios.

### Servicios y aplicación

- Autenticación: `KONTAXPRO.Infrastructure/Security/AuthenticationService.cs`.
- Hash real: `KONTAXPRO.Infrastructure/Security/PasswordHasher.cs`.
- Empresas del usuario: `KONTAXPRO.Infrastructure/Security/UsuarioEmpresaService.cs`.
- Sesión: `KONTAXPRO.Application/Session/CurrentSession.cs`.
- Productos: `KONTAXPRO.Infrastructure/Products/ProductService.cs`.
- Catálogos de producto: `KONTAXPRO.Infrastructure/Products/ProductCatalogService.cs`.
- Inventario: `KONTAXPRO.Infrastructure/Inventory/InventoryService.cs`.
- Interfaces y DTOs: `KONTAXPRO.Application/Interfaces` y `KONTAXPRO.Application/Models`.

Los servicios EF usan `IDbContextFactory<KontaxDbContext>` y el ingreso inicial comparte contexto y transacción. No se encontraron ViewModels que inyecten directamente `KontaxDbContext`.

### Desktop y DI

`KONTAXPRO.Desktop/App.xaml.cs` funciona actualmente como composition root. Registra:

- `AddDbContextFactory<KontaxDbContext>`;
- servicios de autenticación, empresa, productos e inventario;
- `CurrentSession`;
- ViewModels, navegación, tema y ventanas;
- el seeder temporal.

En el arranque solo se comprueba `CanConnectAsync`; no se llama `MigrateAsync`. Después se ejecuta incondicionalmente `DatabaseSeeder.SeedAsync`, sin separación entre datos estructurales y demo ni comprobación del ambiente.

`CurrentSession` ya conserva usuario, empresa, roles, permisos, establecimiento y punto de emisión. Todavía no conserva `BodegaId`, `CajaId`, `CajaNombre` ni `CajaSesionId`.

## 3. Archivos a conservar

“Conservar” significa mantener el archivo o su responsabilidad, con ajustes puntuales cuando el modelo nuevo lo requiera.

### Conservar como base

- `KONTAXPRO.slnx` y los cuatro `.csproj`: estructura de capas correcta y stack .NET 10/WPF vigente.
- `KONTAXPRO.Infrastructure/Persistence/KontaxDbContext.cs`: conservar la clase, el constructor, `IDbContextFactory` y `ApplyConfigurationsFromAssembly`; ampliar y reorganizar sus `DbSet`.
- `KONTAXPRO.Infrastructure/Security/PasswordHasher.cs`: reutilizarlo en `DemoSeeder`.
- `KONTAXPRO.Infrastructure/Security/AuthenticationService.cs`: conserva separación por interfaz, factory por operación y mensajes neutros; adaptar al `Usuario` definitivo y actualizar `ultimo_acceso_at` cuando corresponda.
- `KONTAXPRO.Infrastructure/Security/UsuarioEmpresaService.cs`: reutilizar el flujo multiempresa, adaptando preferencias y autorización.
- `KONTAXPRO.Application/Session/CurrentSession.cs`: conservar el concepto y extenderlo con bodega/caja/sesión de caja.
- `KONTAXPRO.Desktop/Services/SessionFlowService.cs`, `LoginViewModel.cs`, `SeleccionarEmpresaViewModel.cs` y las ventanas de login/selección: conservar el flujo funcional de una o varias empresas.
- `KONTAXPRO.Desktop/App.xaml.cs`: conservarlo como composition root, pero reemplazar la inicialización de datos y separar registros por extensiones si crece demasiado.
- Interfaces de Application para autenticación, usuario-empresa, productos e inventario: conservar la frontera de capa, modificando contratos para el modelo final.
- UI, navegación, estilos y servicio de tema: no dependen materialmente de la reconstrucción; conservar, salvo enlaces del formulario/listado de productos.

### Entidades/configuraciones parcialmente reutilizables

Se pueden conservar nombre y responsabilidad, pero deben alinearse campo por campo:

- `RegimenTributario`, `TipoIdentificacion`, `UnidadMedida`, `Marca`.
- `Empresa`, `Establecimiento`, `PuntoEmision`.
- `Usuario`, `Rol`, `Permiso`, `RolPermiso`, `UsuarioEmpresa`, `UsuarioEmpresaRol`.
- `CategoriaProducto`, `ListaPrecio`, `Bodega`.
- `Producto`, `ProductoPresentacion`, `ProductoPresentacionPrecio`, `ProductoCosto`, `ProductoExistencia`, `ProductoLote`, `ProductoLoteExistencia`, `ProductoSerie`.
- Sus clases `IEntityTypeConfiguration`.

No deben conservarse por compatibilidad columnas que contradigan `AGENTS.md`.

## 4. Archivos a reemplazar o reestructurar

### Reemplazo directo

- `KONTAXPRO.Infrastructure/Persistence/DatabaseSeeder.cs`: reemplazar por `StructuralSeeder` idempotente y `DemoSeeder` exclusivo de Development. El actual crea solo un usuario, usa datos personales, depende del código `CI`, no crea empresas ni permisos y retorna por la existencia del usuario.
- `MovimientoInventario.cs` y `MovimientoInventarioConfiguration.cs`: reemplazar su forma actual de origen/destino por un movimiento de una sola bodega, catálogos de tipo/origen, secuencial, reverso y datos de anulación.
- `MovimientoInventarioDetalle.cs` y su configuración: reestructurar snapshots y mover lotes/series a tablas detalle separadas.
- `InventoryService.cs`: adaptar al movimiento `INVENTARIO_INICIAL`, secuencial transaccional, una bodega, snapshots anterior/nuevo y tablas puente de lotes/series.
- `ProductService.cs`: reescribir las partes ligadas al agregado antiguo, especialmente impuesto en producto, stock mínimo global, lista predeterminada antigua y generación con `MAX`.
- DTOs/request de productos y ViewModel/XAML del formulario: reemplazar contratos y bindings que exponen campos antiguos.

### Reubicación o cambio conceptual

- `CategoriaProducto` está bajo `Entities/Inventario`, pero el esquema aprobado es `s_catalogos`; mover su organización lógica a Catálogos.
- `ListaPrecio` está bajo Inventario, aunque su responsabilidad comercial debe tratarse coherentemente con el modelo aprobado.
- `UsuarioEmpresaConfiguracion` debe convertirse en `usuarios_configuracion_empresa`: la clave aprobada es `(usuario_id, empresa_id)` y contiene establecimiento, punto opcional y bodega opcional. No debe depender únicamente de `usuario_empresa_id` ni exigir punto de emisión.
- `ConfiguracionInventario` no figura como tabla aprobada con ese contrato. Antes de conservarla debe justificarse cada campo por una inconsistencia funcional real; de lo contrario, sustituirla por la configuración mínima aprobada.
- El registro del seeding en `App.xaml.cs` debe separar seeders y condicionar `DemoSeeder` al ambiente.

### Archivos nuevos necesarios

Faltan entidades y configuraciones para los catálogos y módulos descritos en `AGENTS.md`, incluyendo:

- catálogos tributarios, comerciales, de workflows y orígenes;
- secuenciales internos, de comprobantes y asientos;
- terceros y empresas-terceros;
- transferencias y ajustes de inventario;
- ventas, compras y documentos relacionados;
- cartera/CxP y movimientos append-only;
- cajas, bancos y sus movimientos;
- contabilidad;
- retenciones;
- comprobantes electrónicos y eventos;
- auditoría;
- configuración de facturación electrónica;
- autorizaciones por establecimiento.

No deben crearse todos simultáneamente: se incorporarán por los cinco bloques aprobados.

## 5. Entidades y campos obsoletos o incompatibles

### Prohibidos expresamente y presentes

| Ubicación actual | Elemento | Dependencias observadas |
|---|---|---|
| `Producto` | `TarifaImpuestoId` / `tarifa_impuesto_id` | configuración EF, DTOs, `ProductService`, `ProductFormViewModel`, XAML |
| `Producto` | `StockMinimo` / `stock_minimo` | configuración EF, DTOs, listado, formulario y servicio |
| `MovimientoInventario` | `BodegaOrigenId` / `bodega_origen_id` | configuración EF e `InventoryService` |
| `MovimientoInventario` | `BodegaDestinoId` / `bodega_destino_id` | configuración EF e `InventoryService` |

No se localizaron en el código actual `productos.precio`, `precio_a/b/c`, `productos.costo`, `productos.stock`, `productos.codigo_barras`, `cobros.medio_pago_id`, claves de acceso en documentos emisores, `clientes`, `proveedores`, `plan_cuentas.saldo_actual`, `sesiones_login`, `usuarios_permisos`, `usuarios_empresas_establecimientos_roles`, `tipos_credito_proveedor` ni `CREDITO_PROVEEDOR_APLICADO`. Varios no aparecen porque los módulos aún no existen.

### Incompatibles adicionales

- `Marca`: faltan `uuid` y `codigo`.
- `CategoriaProducto`: faltan `uuid`, `categoria_padre_id` y `codigo`.
- `TarifaImpuesto`: no referencia `Impuesto`; faltan tipo de cálculo, vigencias, valor específico y descripción; conserva `EsTarifaCero`.
- `TipoIdentificacion`: falta `codigo_sri`; longitudes son opcionales aunque el modelo las exige.
- `UnidadMedida`: falta descripción; `abreviatura` es opcional; conserva `PermiteDecimales`, no aprobado.
- `Empresa`: conserva `TipoIdentificacionId`, actividad, dirección matriz, representante, datos de agente, logo binario, ruta y password de certificado; faltan `contribuyente_especial_numero` y `correo` con el nombre aprobado. Ruta/password/binario contradicen las reglas de archivos y secretos.
- `Establecimiento`: falta `prefijo`; dirección es opcional; no existe índice único de prefijo ni índice parcial de una matriz.
- `Usuario`: conserva `TipoIdentificacionId`, teléfono y `Theme`; falta `RequiereCambioClave`; usa `UltimoAcceso` en lugar de `UltimoAccesoAt`.
- `UsuarioEmpresaConfiguracion`: modelo, clave y campos no coinciden con `usuarios_configuracion_empresa`.
- Falta `UsuarioEmpresaEstablecimiento`.
- `Bodega`: depende directamente de `EmpresaId`; debe depender de `EstablecimientoId`. `EsPrincipal`, `PermiteVentas` y `PermiteCompras` no corresponden a los flags aprobados.
- `Producto`: falta `uuid`; conserva `TipoControlInventario`, `PermiteVentaSinStock` y `AlertaStockMinimo`; la política de stock mínimo debe vivir en `productos_existencias`.
- `ProductoPresentacion`: falta `uuid` y `empresa_id`; conserva `UnidadMedidaId` y `CodigoBarrasInterno`, no aprobados.
- `ListaPrecio`: `EsPredeterminada` debe ser `EsListaBase`; faltan porcentaje predeterminado y orden.
- `ProductoPresentacionPrecio`: método `RECARGO_COSTO`, `PrecioManual` y precio no nullable no representan los métodos aprobados.
- `ProductoCosto`: sobra `CostoMaximoExistencia`.
- `ProductoExistencia`: faltan `StockMinimo` y `Ubicacion`.
- `ProductoLote`: `FechaFabricacion` debe ser `FechaElaboracion`; sobra costo en el lote.
- `ProductoLoteExistencia`: falta `Ubicacion`.
- `ProductoSerie`: bodega debe ser obligatoria; el estado debe referenciar `estados_serie`; sobran costo y estado en texto; falta ubicación.
- `MovimientoInventario`: además de las dos bodegas, faltan número de movimiento, tipo/origen por FK, `origen_id`, anulación y reverso.
- `MovimientoInventarioDetalle`: faltan stocks y costos promedio anterior/nuevo y `updated_at`; lote/serie no deben estar como FK directas.

## 6. Configuraciones EF: hallazgos transversales

- Se localizaron configuraciones para todas las entidades actuales y se aplican automáticamente.
- Varias relaciones de maestros usan `DeleteBehavior.Cascade`, por ejemplo empresa-establecimiento, usuario-empresa y configuraciones. Deben revisarse contra la preferencia obligatoria por `Restrict` para maestros e historia.
- Varias entidades de inventario usan `timestamp without time zone`; los eventos exactos aprobados deben ser `timestamp with time zone` y manejar UTC.
- Los estados se modelan como `short`; la convención aprobada es `INTEGER`.
- Faltan identidades UUID en catálogos portables.
- Faltan integridades compuestas multiempresa, especialmente producto-presentación y validaciones entre producto, bodega, listas y empresa.
- Faltan índices parciales aprobados: una matriz, una presentación base, una lista base, una sesión abierta y reversos únicos, entre otros.
- Faltan checks de cantidades, costos, secuenciales y reglas de métodos de precio.

## 7. Migraciones actuales

No se encontró ninguna migración EF Core ni `ModelSnapshot`.

Consecuencias:

- el esquema actual no es reproducible desde el repositorio;
- `App.xaml.cs` no ejecuta migraciones;
- el build no valida que el modelo sea aplicable a una base vacía;
- no existe todavía la `InitialCreate` limpia requerida.

Esto evita tener que depurar una cadena experimental, pero aumenta la necesidad de verificar el modelo completo con una migración temporal de validación antes de crear la `InitialCreate` final. La migración final no debe generarse hasta completar los cinco bloques.

## 8. Seeders actuales

Solo existe `DatabaseSeeder`.

Problemas:

- mezcla la intención de demo con el arranque general;
- se ejecuta en cualquier ambiente;
- no es un `StructuralSeeder`;
- no crea catálogos estructurales;
- crea un único usuario con identificación, nombre y correo personales;
- contiene una contraseña demo fija;
- busca `TipoIdentificacion.Codigo == "CI"`, mientras el catálogo aprobado exige código `CEDULA` y código SRI `05`;
- no crea dos empresas, roles, permisos, establecimientos, punto, bodega, caja, lista, secuenciales ni preferencias;
- si el usuario existe, abandona todo el proceso, por lo que no es idempotente por conjunto de datos.

## 9. DI, sesión y autenticación

### Aspectos reutilizables

- DI centralizado con `Microsoft.Extensions.DependencyInjection`.
- Uso consistente de `IDbContextFactory`.
- Interfaces en Application e implementaciones en Infrastructure.
- `CurrentSession` se comparte como singleton.
- Login y selección de empresa están separados.
- `HasPermission` permite acceso total al rol `ADMINISTRADOR`.

### Ajustes requeridos

- Incorporar ambiente (`IHostEnvironment` o equivalente) para impedir `DemoSeeder` fuera de Development.
- Ejecutar migraciones y seeders con orden explícito: migración, estructural y luego demo condicionado.
- Agregar bodega/caja a `CurrentSession` y limpiar todos esos valores al cambiar/cerrar sesión.
- Adaptar la carga de preferencias al contrato `usuarios_configuracion_empresa`.
- Validar establecimientos autorizados mediante `usuarios_empresas_establecimientos`.
- Registrar `ultimo_acceso_at` sin degradar la neutralidad de errores de login.
- Evaluar scopes/lifetimes cuando crezcan servicios; los singleton actuales dependen de factory y sesión singleton, por lo que hoy no capturan un DbContext.

## 10. Dependencias concretas del modelo viejo

### Cadena de productos

```text
Domain Producto/Presentación/Precio
→ configuraciones EF
→ DTOs y requests de Application
→ ProductService/ProductCatalogService
→ ProductFormViewModel/ProductsViewModel
→ ProductFormView/ProductsView
```

La retirada de `TarifaImpuestoId`, `StockMinimo`, `TipoControlInventario`, `EsPredeterminada` y campos auxiliares exige adaptar toda esta cadena dentro del mismo bloque para mantener el build verde.

`ProductService.GenerarCodigoBarrasInternoAsync` contiene SQL con `SELECT MAX(...)`. Aunque genera código de barras y no un secuencial documental, mantiene el mismo patrón inseguro bajo concurrencia y debe reemplazarse. Los secuenciales aprobados nunca pueden usar `MAX + 1`.

### Cadena de inventario inicial

```text
IngresoInventarioRequest
→ InventoryService
→ MovimientoInventario con BodegaDestinoId
→ existencias/costos/lotes/series
→ MovimientoInventarioDetalle con lote/serie directos
```

Debe migrar a:

```text
secuencial interno transaccional
→ movimiento INVENTARIO_INICIAL de una bodega
→ snapshots completos
→ detalles de lotes
→ detalles de series
→ existencias y costo promedio
```

### Cadena de empresa/sesión

```text
UsuarioEmpresa
→ UsuarioEmpresaConfiguracion
→ PuntoEmision
→ UsuarioEmpresaService
→ CurrentSession
→ SessionFlowService/UI
```

El nuevo contrato debe separar autorización (`usuarios_empresas_establecimientos`) de preferencias (`usuarios_configuracion_empresa`) e incorporar establecimiento, punto y bodega.

## 11. Riesgos

### Críticos

- **Credenciales en repositorio:** `KONTAXPRO.Desktop/appsettings.json` contiene usuario y contraseña PostgreSQL en texto plano. Deben rotarse y moverse a configuración local/almacenamiento seguro antes de distribuir.
- **Arranque sin migraciones:** una base vacía no puede construirse oficialmente desde el código actual.
- **Seeder no condicionado:** datos demo/personales pueden insertarse fuera de Development.
- **Modelo incompleto:** faltan la mayoría de módulos; crear ahora `InitialCreate` produciría una fuente de verdad falsa.
- **Concurrencia:** generación mediante `MAX` y ausencia de entidades de secuenciales.

### Altos

- **Aislamiento multiempresa incompleto:** bodegas no pertenecen a establecimiento y faltan claves compuestas.
- **Borrado en cascada:** puede eliminar datos dependientes que deberían conservar historia.
- **Timestamps inconsistentes:** uso de hora local y columnas sin zona horaria.
- **Inventario no auditable:** movimiento sin número, origen normalizado, snapshots completos, anulación ni reverso.
- **Secretos/documentos en BD:** `Empresa` permite password/ruta de certificado y logo binario.
- **Códigos de workflow como texto libre:** riesgo de estados inválidos y divergencia entre capas.

### Medios

- No hay proyecto de pruebas localizado.
- No hay factory de diseño explícita para `dotnet ef`; habrá que validar el proyecto de inicio y configuración al generar migraciones.
- El repositorio no tiene metadatos Git visibles, lo que reduce trazabilidad y recuperación durante una reconstrucción extensa.
- El bloque Productos tiene alta superficie de cambio en Domain, Infrastructure, Application y Desktop.

## 12. Plan exacto por bloques

Regla para todos los bloques: inspeccionar primero, realizar cambios coherentes verticalmente, ejecutar `dotnet build KONTAXPRO.slnx`, corregir todo y no iniciar el bloque siguiente con build roto. No borrar la base ni crear la migración final durante los bloques.

### Preparación

1. Confirmar que el árbol de trabajo esté respaldado o bajo control de versiones.
2. Extraer y rotar las credenciales actualmente versionadas.
3. Introducir códigos/enums de Domain para estados y tipos aprobados.
4. Preparar convenciones comunes de EF: nombres, `BIGINT` identity, `INTEGER` para maestros, UTC/timestamptz, precisiones y `Restrict`.
5. Mantener temporalmente el arranque compatible, sin aplicar aún cambios destructivos a la base.
6. Build obligatorio.

### Bloque 1: catálogos, configuración, seguridad y comercial

1. Completar catálogos y sus códigos estructurales.
2. Alinear `Empresa`, `Establecimiento`, `PuntoEmision` y agregar secuenciales/configuración electrónica.
3. Alinear usuarios, roles, permisos y relaciones.
4. Crear autorización por establecimiento y rehacer preferencias usuario/empresa.
5. Crear auditoría append-only.
6. Crear `Tercero` y `EmpresaTercero`; no crear `Cliente`/`Proveedor`.
7. Implementar `StructuralSeeder` idempotente con catálogos, roles, permisos y consumidor final.
8. Implementar el esqueleto de `DemoSeeder`, condicionado estrictamente a Development.
9. Adaptar autenticación, selección de empresa y `CurrentSession`.
10. Adaptar DI/arranque sin ejecutar aún la reconstrucción final.
11. Build obligatorio.

### Bloque 2: inventario

1. Alinear categorías, marcas, unidades y productos portables con UUID.
2. Retirar del producto impuesto y stock mínimo; adaptar DTOs, servicios, ViewModels y XAML.
3. Alinear presentaciones, listas, precios, costos y existencias.
4. Alinear lotes/series y crear sus catálogos/tablas detalle.
5. Rehacer movimientos para una sola bodega con tipo/origen, secuencial, snapshots y reversos.
6. Crear transferencias y ajustes.
7. Rehacer inventario inicial sobre el nuevo movimiento.
8. Eliminar toda generación `MAX + 1`.
9. Completar bodega/preferencias demo mínimas.
10. Build obligatorio y prueba focal de Productos a nivel de servicio/UI sin reconstruir todavía la BD oficial.

### Bloque 3: ventas y compras

1. Implementar proformas sin efectos contables/físicos.
2. Implementar XF, notas de entrega y facturas con enlaces documentales y regla de stock único.
3. Implementar impuestos snapshot y formas de pago.
4. Implementar devoluciones y notas crédito/débito.
5. Implementar guías sin movimiento de inventario.
6. Implementar documentos recibidos SRI, compras y liquidaciones.
7. Implementar devoluciones y ajustes de compra.
8. Añadir secuenciales transaccionales, anulación y reversos.
9. Build obligatorio.

### Bloque 4: cartera, tesorería y bancos

1. Implementar CxC/CxP y movimientos append-only con secuencia transaccional.
2. Implementar cobros/pagos multi-medio y aplicaciones/reversos.
3. Implementar cajas compartidas y sesión única abierta por caja.
4. Añadir caja activa a `CurrentSession` y selección automática/manual.
5. Implementar movimientos de caja append-only y depósitos caja-banco.
6. Implementar cuentas y movimientos bancarios y transferencias.
7. Completar caja principal demo y preferencias locales pertinentes.
8. Build obligatorio.

### Bloque 5: contabilidad, tributación y facturación electrónica

1. Implementar plan, configuración de cuentas, períodos y secuenciales.
2. Implementar asientos balanceados, automáticos/manuales y reversos.
3. Implementar retenciones emitidas/recibidas y su efecto exclusivo sobre cartera.
4. Implementar comprobantes electrónicos y eventos append-only.
5. Implementar coordinación del worker solo para instalaciones SERVIDOR y recuperación de trabajos.
6. Implementar filesystem autorizado sin binarios/rutas absolutas/passwords en PostgreSQL.
7. Completar configuración y datos demo mínimos.
8. Build obligatorio.

### Cierre y reconstrucción controlada

Solo después de los cinco bloques con build verde:

1. Confirmar explícitamente ambiente `Development`.
2. Mostrar y verificar host, puerto y nombre de base; abortar ante cualquier indicio de Production.
3. Eliminar migraciones experimentales si se hubieran generado para validación.
4. Generar una única migración `InitialCreate`.
5. Revisar SQL/model snapshot: esquemas, tipos, precisiones, FK, checks, índices parciales y ausencia de campos prohibidos.
6. Ejecutar `dotnet restore` y `dotnet build KONTAXPRO.slnx`.
7. Solo con autorización y verificación anterior, eliminar la base de desarrollo.
8. Aplicar `InitialCreate`.
9. Ejecutar `StructuralSeeder`.
10. Ejecutar `DemoSeeder` únicamente en Development.
11. Iniciar la aplicación y probar login.
12. Confirmar selección entre dos empresas y cambio de empresa.
13. Confirmar establecimiento, punto, bodega, caja y sesión en `CurrentSession`.
14. Probar el módulo Productos e inventario inicial.
15. Registrar resultados y ejecutar un build final.

## 13. Criterio para la siguiente fase

La reconstrucción no debe comenzar con una eliminación de base. La siguiente fase segura es el Bloque 1, empezando por Domain y configuraciones EF, adaptando Application/Desktop solo en la medida necesaria para conservar el build. La `InitialCreate` y el borrado de la base quedan expresamente diferidos hasta completar y validar los cinco bloques.
