# Patrones de desarrollo KONTAXPRO Desktop

Este documento registra patrones técnicos para módulos nuevos o cambios estructurales. Complementa, pero no duplica, [UI_UX_KONTAXPRO.md](UI_UX_KONTAXPRO.md).

## 1. Arquitectura y dependencias

| Proyecto | Responsabilidad | Dependencias permitidas en el patrón actual |
|---|---|---|
| `KONTAXPRO.Domain` | Entidades, códigos de dominio y reglas puras | No depende de Desktop ni Infrastructure. |
| `KONTAXPRO.Application` | DTOs, interfaces, casos de uso, validaciones y sesión | Depende de Domain; no de implementaciones EF/WPF. |
| `KONTAXPRO.Infrastructure` | EF Core, PostgreSQL, servicios concretos, seeders y servicios técnicos | Implementa contratos de Application y usa Domain. |
| `KONTAXPRO.Desktop` | WPF, Views, ViewModels, navegación y composition root | Consume Application; registra implementaciones en DI. |

No acceder a `KontaxDbContext` desde ViewModels. Una regla de negocio no debe quedar escondida en XAML o code-behind. Las referencias entre proyectos deben conservar esta dirección.

## 2. MVVM

La View declara layout, bindings, templates, behaviors y estados visuales. El ViewModel expone estado observable y comandos usando CommunityToolkit.Mvvm (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`). Application/Infrastructure valida y ejecuta la operación.

Reglas:

- El ViewModel no conoce ni crea una `Window` concreta.
- El ViewModel no usa `MessageBox`.
- Los comandos asíncronos manejan carga y evitan reentrada.
- La lógica de dominio reutilizable vive fuera del ViewModel.
- Code-behind se limita a foco, scroll, medición y mecánica de controles.
- Propiedades calculadas notifican explícitamente sus dependencias.
- Un binding TwoWay solo apunta a una propiedad escribible.

## 3. Dependency Injection

Las interfaces de Application se registran en `KONTAXPRO.Desktop/App.xaml.cs`. Preferir inyección por constructor. No usar service locator dentro de ViewModels ni instanciar servicios concretos en Views.

Elegir lifetime por responsabilidad:

- Singleton para sesión y servicios de UI con estado global controlado.
- Transient para ViewModels/Views cuando la composición lo requiera.
- Los servicios EF crean un contexto por operación mediante factory; no conservan un contexto singleton.

## 4. EF Core y PostgreSQL

- Entidades y configuraciones EF son la fuente de verdad estructural.
- Usar `IEntityTypeConfiguration<T>` y nombres explícitos de tabla/esquema.
- Preferir `DeleteBehavior.Restrict` en maestros e historia.
- Cantidades/costos: `numeric(18,6)`; dinero: `numeric(18,2)` salvo decisión documentada.
- Fechas civiles: `date`; eventos: `timestamp with time zone` y UTC.
- Índices únicos y checks deben proteger invariantes además de la validación de aplicación.
- Consultas de lectura usan `AsNoTracking` cuando no habrá actualización.
- No crear migraciones experimentales ni scripts manuales como fuente oficial.

Los servicios concretos prefieren `IDbContextFactory<KontaxDbContext>`. Una operación compuesta crea un contexto, abre una transacción y comparte ambos hasta confirmar o revertir.

## 5. Multiempresa

Toda entrada de servicio que opere datos empresariales recibe o deriva `EmpresaId`. El filtro debe alcanzar la entidad objetivo y sus relaciones; no basta ocultar datos en la UI.

Antes de escribir:

- validar que el usuario pertenece a la empresa;
- validar que establecimiento, bodega, listas y relaciones pertenecen a esa empresa;
- usar claves/índices compuestos donde refuercen aislamiento;
- devolver no encontrado cuando el ID existe en otra empresa;
- evitar cargar una entidad por ID y comprobar empresa demasiado tarde.

## 6. CurrentSession

`KONTAXPRO.Application/Session/CurrentSession.cs` mantiene usuario, empresa, establecimiento, punto de emisión, bodega, caja, roles y permisos activos. Los ViewModels consumen estos datos para contexto y presentación; los servicios vuelven a validar pertenencia y autorización.

`HasPermission` facilita visibilidad o habilitación, pero no sustituye la comprobación en la frontera que confirma la operación. El rol ADMINISTRADOR obtiene acceso global según la implementación actual.

## 7. Servicios

Un servicio representa una capacidad coherente, no una pantalla. Su interfaz vive en Application y sus modelos de entrada/salida son DTOs. Infrastructure implementa persistencia y detalles técnicos.

Patrón de una mutación:

1. Validar forma básica del request.
2. Crear contexto.
3. Abrir transacción si participan varias escrituras.
4. Exigir permiso y pertenencia multiempresa.
5. Cargar catálogos/entidades necesarias.
6. Ejecutar reglas e invariantes.
7. Escribir cabecera, detalles y efectos derivados.
8. Guardar y confirmar.
9. Revertir y traducir conflictos a un resultado comprensible.

No exponer entidades EF a Desktop ni reutilizar un contexto entre operaciones independientes.

## 8. DTOs

Separar:

- DTO de listado: proyección compacta y lista para renderizar.
- DTO de detalle: datos necesarios para editar/consultar.
- Request de operación: intención y valores capturados.
- Result: éxito, identificador y mensaje sin excepción técnica.

No agregar al DTO una entidad completa por comodidad. Las propiedades derivadas de solo lectura deben usar bindings OneWay. Si la UI necesita editar, crear un editor ViewModel con propiedad escribible.

## 9. Listados

El patrón de Productos combina query con empresa, búsqueda, filtros, orden, página y tamaño. La consulta filtra, cuenta, ordena y pagina en servidor. Datos secundarios de la página se completan en consultas agrupadas por IDs, no dentro de un bucle por fila.

Para búsquedas remotas:

- debounce;
- `CancellationToken` por solicitud;
- secuencia o identidad para descartar respuestas atrasadas;
- volver a página 1 al cambiar criterios;
- estado de carga y estado vacío explícitos.

KPIs y filtros deben compartir exactamente la misma definición funcional.

## 10. Formularios Nuevo/Editar

Compartir ViewModel cuando ambos modos manipulan el mismo agregado. Exponer `IsEditing` y derivar visibilidad, títulos y operaciones. La inicialización debe limpiar todo estado anterior antes de cargar catálogos o detalle.

Guardar construye un request, delega al servicio y solo cierra tras éxito. Cancelar limpia editores y diálogos temporales. Al ocultar, restablecer scroll. Los hechos históricos se muestran o gestionan mediante operaciones separadas; no se convierten en campos editables del maestro.

## 11. Modales y diálogos

Un diálogo auxiliar puede vivir como overlay dentro de la View o como ventana resuelta por el servicio de UI. En ambos casos:

- estado y comando en ViewModel;
- footer fijo;
- cierre seguro y descartable;
- loading/confirmación global cuando corresponde;
- foco inicial gestionado por una señal o evento de UI, no por referencia a controles desde el ViewModel.

Evitar abrir dos diálogos bloqueantes simultáneos. `MessageDialogService` ya protege esa situación.

## 12. Operaciones históricas

Un hecho confirmado no se edita ni elimina físicamente. Para modificar consecuencias se usa anulación/reverso o una operación correctiva nueva según el dominio. Inventario conserva movimiento, detalle, stocks y costos snapshot.

No recalcular toda la historia para cambiar un maestro. Un dato identificador corregible puede actualizar el maestro y registrar una corrección explícita; las cantidades históricas permanecen intactas.

## 13. Snapshots

Guardar snapshot cuando el significado histórico no debe cambiar si cambia el maestro: número/documento, precio, costo, factor, stock anterior/nuevo, nombre de motivo y datos tributarios. Una FK sola no preserva necesariamente la información observada al confirmar.

Definir el snapshot en Domain/EF y llenarlo dentro de la misma transacción del hecho. No reconstruirlo posteriormente desde valores actuales.

## 14. Secuenciales

Nunca usar `MAX + 1`. Utilizar la tabla de secuenciales correspondiente y actualización transaccional/atómica. El número asignado se guarda como parte del mismo commit que la operación.

Los consecutivos tributarios e internos pertenecen a su ámbito —empresa, establecimiento, punto, tipo y ambiente según corresponda— y no se comparten por conveniencia.

## 15. Transacciones y concurrencia

Usar transacción cuando una intención modifica más de una tabla o combina efecto físico e histórico. Para clasificaciones que dependen de stock estable puede ser necesario `Serializable`, como en conversión de control.

Dentro de la transacción:

- volver a leer y validar el estado relevante;
- aplicar efectos una sola vez;
- guardar snapshots;
- manejar `DbUpdateException` y conflictos únicos;
- rollback ante cualquier fallo.

No iniciar una transacción en ViewModel. La frontera transaccional está en Infrastructure.

## 16. Auditoría

Las tablas append-only no se editan ni eliminan. Toda operación sensible conserva usuario, empresa, establecimiento, momento y motivo cuando el modelo lo exige. Correcciones y anulaciones identifican valor anterior/nuevo o referencia al reverso.

No registrar secretos, hashes, contraseñas, certificados ni connection strings. Los mensajes de auditoría describen intención de negocio, no detalles internos de excepción.

## 17. Permisos

Los permisos se definen como códigos estructurales y se asignan a roles mediante seeders idempotentes. La UI puede ocultar o deshabilitar acciones con `CurrentSession.HasPermission`; el servicio debe volver a exigir el permiso antes de mutar.

Lista de control:

- permiso sembrado;
- asignación de rol aprobada;
- comprobación en servicio concreto;
- filtro de empresa y pertenencia del usuario;
- estado visual coherente;
- prueba de autorizado y no autorizado cuando la operación sea crítica.

Un permiso existente solo en el seeder no constituye autorización efectiva.

## 18. Código muerto

Antes de eliminar código:

1. Buscar referencias en C#, XAML, DI, reflexión, migración y tests.
2. Confirmar que no es una API pública o extensión prevista.
3. Retirar código, recursos y registro DI juntos.
4. Compilar y ejecutar pruebas.

No conservar implementaciones antiguas que contradigan `AGENTS.md`, pero tampoco borrar código dudoso dentro de una tarea no autorizada. Los hallazgos de auditoría se documentan y se corrigen en una tarea específica.

## 19. Performance

- Proyectar solo columnas necesarias.
- Filtrar/ordenar/paginar en PostgreSQL.
- Evitar `Include` indiscriminado en listados.
- Agrupar consultas de datos secundarios por IDs de página.
- Evitar cualquier query dentro de un `foreach` de resultados.
- Usar índices acordes a claves únicas y filtros frecuentes.
- Cancelar búsquedas obsoletas.
- No bloquear el hilo UI durante I/O.

Medir antes de introducir caché. La caché nunca debe mezclar empresas ni ocultar cambios transaccionales.

## 20. Errores y feedback

Las reglas previsibles devuelven mensajes funcionales. Conflictos de integridad se traducen sin exponer SQL. Excepciones inesperadas se registran por el mecanismo técnico y la UI muestra un mensaje seguro.

Desktop usa `IMessageDialogService`, `INotificationService` e `ILoadingService`; no `MessageBox`. Un comando restaura su estado de carga en `finally`. No capturar silenciosamente una excepción que deje al usuario creyendo que la operación fue confirmada.

## 21. Archivos y rutas

Al crear una capacidad, seguir la organización existente:

| Elemento | Proyecto y carpeta | Tipo |
|---|---|---|
| Entidad | `KONTAXPRO.Domain/Entities/{Modulo}` | Clase de dominio |
| DTO/request | `KONTAXPRO.Application/Models/{Modulo}` | Modelo de frontera |
| Interfaz | `KONTAXPRO.Application/Interfaces` | Contrato |
| Regla pura | `KONTAXPRO.Application/{Modulo}` | Clase estática o servicio de aplicación |
| Servicio EF | `KONTAXPRO.Infrastructure/{Modulo}` | Implementación concreta |
| Configuración EF | `KONTAXPRO.Infrastructure/Persistence/Configurations` | `IEntityTypeConfiguration<T>` |
| ViewModel | `KONTAXPRO.Desktop/ViewModels/{Modulo}` | Estado/comandos |
| View | `KONTAXPRO.Desktop/Views/{Modulo}` | XAML y code-behind mínimo |
| Estilo global | `KONTAXPRO.Desktop/Styles` o `Themes` | `ResourceDictionary` |
| Test | `KONTAXPRO.Tests/{Modulo}` | xUnit |

Registrar servicios y diccionarios globales en los puntos de composición actuales. No inventar una carpeta paralela para el mismo concepto.

## Productos como módulo de referencia

Productos ofrece patrones reutilizables para:

- layout de catálogo, KPIs, búsqueda, sort y paginación;
- DataGrid temático y acciones compactas;
- formulario compartido Nuevo/Editar con header, cuerpo y footer;
- cards y diálogos auxiliares;
- mensajes, notificaciones y loading global;
- contratos Application e implementación EF con factory;
- DTOs de query, detalle y operación;
- validaciones puras probables;
- transacciones de operaciones históricas;
- autorización en la frontera de servicio.

Consultar [PRODUCTOS_V1.md](PRODUCTOS_V1.md) para las reglas concretas y [UI_UX_KONTAXPRO.md](UI_UX_KONTAXPRO.md) para el estándar visual.

## Qué no copiar ciegamente

Productos es referencia, no plantilla rígida. No copiar sin análisis:

- sus campos o columnas;
- el número de KPIs;
- anchos y alturas específicos;
- reglas de stock, costos, lotes o series;
- sus permisos;
- las operaciones de inventario;
- la estructura exacta del formulario;
- el número o tipo de diálogos.

Cada módulo adapta los patrones a su dominio y mantiene únicamente las convenciones globales pertinentes.

## 22. Validación de una tarea

Antes de cerrar cambios de implementación:

```text
dotnet restore
dotnet build KONTAXPRO.slnx
dotnet test
dotnet ef migrations has-pending-model-changes --project KONTAXPRO.Infrastructure --startup-project KONTAXPRO.Desktop
git diff --check
```

La comprobación EF se ejecuta cuando el cambio puede afectar el modelo o como cierre de fase; no se genera una migración salvo autorización expresa. Si una regla documentada cambia, actualizar el documento correspondiente en la misma tarea.
