# PRODUCTOS V1 — KONTAXPRO Desktop

**ESTADO: FINALIZADO COMO PRIMERA VERSIÓN FUNCIONAL**

PRODUCTOS V1 queda congelado como referencia. Los cambios posteriores deben responder a requerimientos concretos, errores confirmados o integración con otros módulos, no a rediseños arbitrarios.

Para convenciones visuales globales consultar [UI/UX KONTAXPRO](UI_UX_KONTAXPRO.md). Para estructurar nuevos módulos consultar [Patrones de desarrollo](PATRONES_DESARROLLO_KONTAXPRO.md).

## 1. Resumen y límites del módulo

Productos administra el catálogo comercial de una empresa: identidad, clasificación, unidad base, impuesto, presentaciones, precios y configuración de control físico. También ofrece la puerta de entrada a operaciones de inventario relacionadas con el producto.

La separación esencial es:

- **Producto** define información comercial y comportamiento.
- **Inventario** registra hechos físicos, costos, existencias y trazabilidad.

El editor no modifica directamente hechos confirmados. Una entrada inicial, ajuste, conversión o corrección usa servicios de inventario y deja el registro correspondiente. Ventas, compras, transferencias y demás módulos transaccionales no forman parte del alcance funcional de Productos V1.

## 2. Arquitectura real

| Componente | Archivo o ruta | Responsabilidad |
|---|---|---|
| Listado | `KONTAXPRO.Desktop/Views/Products/ProductsView.xaml` y `ProductsCatalogContentView.xaml` | Contenedor y catálogo visual de productos. |
| Editor | `KONTAXPRO.Desktop/Views/Products/ProductFormView.xaml` | Formulario Nuevo/Editar y diálogos de operaciones. |
| ViewModels | `KONTAXPRO.Desktop/ViewModels/Products/ProductsViewModel.cs` y `ProductFormViewModel.cs` | Estado de pantalla, comandos y orquestación de UI. |
| Contratos | `KONTAXPRO.Application/Interfaces/IProductService.cs`, `IProductCatalogService.cs`, `IInventoryService.cs` | Frontera entre Desktop e Infrastructure. |
| DTOs | `KONTAXPRO.Application/Models/Productos` y `Models/Inventario` | Entradas y resultados sin exponer entidades EF. |
| Reglas puras | `KONTAXPRO.Application/Products` e `Inventory` | Validaciones reutilizables de producto, catálogo, ajustes y control. |
| Dominio | `KONTAXPRO.Domain/Entities/Inventario` | Entidades persistentes y relaciones del módulo. |
| Servicios | `KONTAXPRO.Infrastructure/Products` e `Inventory/InventoryService.cs` | Consultas, persistencia, transacciones y autorización operativa. |
| Configuración EF | `KONTAXPRO.Infrastructure/Persistence/Configurations/Inventario*Configuration.cs` y `MotivosOperacionInventarioConfiguration.cs` | Esquema, índices, restricciones y relaciones PostgreSQL. |
| Pruebas | `KONTAXPRO.Tests/Products` | Cobertura de reglas puras del módulo. |

Desktop no accede a `KontaxDbContext`. Los servicios concretos usan `IDbContextFactory<KontaxDbContext>` y contexto por operación.

## 3. Modelo Producto

`Producto` pertenece a una empresa y contiene categoría y marca opcionales, unidad de medida base, código, nombre, descripción, modelo, tipo `PRODUCTO` o `SERVICIO`, configuración de inventario, lotes, series, caducidad, alertas, observación, estado y timestamps.

No almacena directamente precio, costo, stock, stock mínimo, código de barras ni tarifa de impuesto. Esos datos pertenecen a tablas especializadas. `Uuid` proporciona identidad portable y `Codigo` es único por empresa.

Los indicadores `ManejaInventario`, `ManejaLotes`, `ManejaSeries` y `ManejaFechaCaducidad` expresan el comportamiento actual. El tipo de control mostrado por la aplicación se deriva así:

| Lotes | Series | Control |
|---|---|---|
| No | No | NORMAL |
| Sí | No | LOTE |
| No | Sí | SERIE |
| Sí | Sí | LOTE_Y_SERIE |

## 4. Presentaciones

Cada producto posee exactamente una presentación base activa. La base usa código `BASE`, factor de conversión `1` y representa la unidad de medida del producto. Las presentaciones adicionales tienen código propio, nombre, factor positivo, código de barras opcional y permisos de compra/venta.

El factor expresa cuántas unidades base contiene una presentación. Las cantidades físicas se normalizan a base antes de afectar inventario. Una presentación usada en documentos de inventario o comerciales no puede cambiar libremente su factor. La unidad base tampoco puede cambiar si existen historia, stock, lotes o series.

La implementación actual de guardado exige que las presentaciones enviadas permitan compra y venta. La restricción de una sola base y el factor base igual a uno también están protegidos en EF.

## 5. Códigos de barras

El código de fabricante es opcional y se valida por empresa. La restricción PostgreSQL es única sobre `(empresa_id, codigo_barras)` cuando el valor no es nulo.

Si una presentación no recibe código de fabricante, después de obtener su identidad se genera uno interno con el formato:

```text
KPX-{PREFIJO_ESTABLECIMIENTO}-{ID_PRESENTACION:00000000}
```

La generación ocurre dentro de la transacción de guardado. El flujo Nuevo permite comprobar previamente un código: si ya existe abre el producto correspondiente; si no existe, puede continuar. La restricción de base de datos resuelve además carreras concurrentes.

## 6. Impuestos

La relación vigente se almacena en `productos_impuestos`; el producto no contiene `tarifa_impuesto_id`. El editor carga las tarifas activas desde `IProductCatalogService` y el guardado valida su existencia. La tarifa aplicable se asocia al producto mediante una relación con estado y timestamps.

## 7. Precios por lista

Los precios pertenecen a una combinación presentación/lista. `productos_presentaciones_precios` guarda método de cálculo, porcentaje o precio fijo y estado.

- Lista base: `PORCENTAJE_COSTO` o `PRECIO_FIJO`.
- Listas preferenciales: `DESCUENTO_PORCENTAJE` o `PRECIO_FIJO`.

En Nuevo, Lista A inicia con precio fijo. Al cambiar de método, el foco pasa al valor correspondiente. El editor presenta advertencias y precio sugerido cuando una presentación con factor mayor produce una relación ilógica frente al precio unitario; la advertencia no reemplaza la decisión explícita del usuario.

Las listas demo son:

- A: `LISTA A - PRECIO NORMAL`, lista base.
- B: `LISTA B - DISTRIBUIDOR`, descuento predeterminado 5 %.
- C: `LISTA C - MAYORISTA`, descuento predeterminado 10 %.

La configuración EF persiste `precio` como `numeric(18,2)`. Los porcentajes y cálculos internos usan decimales; cualquier cambio futuro de precisión persistida exige una decisión de modelo y migración explícita.

## 8. Costos

`productos_costos` mantiene último precio de compra, último costo efectivo y costo promedio, con precisión `numeric(18,6)` y valores no negativos. El costo de ingreso se convierte a costo unitario base usando el factor de presentación.

Las entradas actualizan el promedio ponderado dentro de la misma transacción que el stock y el movimiento. Las salidas usan el costo vigente y no recalculan masivamente la historia. En el editor, el costo promedio actual se muestra con mínimo dos y máximo seis decimales sin reducir la precisión persistida.

## 9. Existencias y bodegas

`productos_existencias` mantiene por producto y bodega `stock_actual`, `stock_reservado`, `stock_minimo` y ubicación. Disponible es `stock_actual - stock_reservado`. Cantidades y costos usan seis decimales; el listado normaliza solo la visualización.

En Editar únicamente mínimo y ubicación son datos maestros editables. El stock confirmado cambia mediante operaciones de inventario. PostgreSQL permite stock actual negativo cuando una política funcional lo autorice, pero reservado y mínimo no pueden ser negativos.

Las bodegas se validan por establecimiento y empresa activos. La demo crea por establecimiento:

- `FAC` — PRODUCTOS CON FACTURA, permite venta facturada.
- `SFA` — PRODUCTOS SIN FACTURA, no permite venta facturada.

## 10. Lotes

Un lote pertenece a un producto y su número es único dentro de este. Puede contener elaboración, caducidad y observación; las fechas son anulables cuando el producto no controla caducidad. Si ambas existen, elaboración no puede ser posterior a caducidad.

El stock por lote y bodega se almacena en `productos_lotes_existencias`. Los movimientos guardan la distribución en `movimientos_inventario_detalles_lotes`.

Para entradas, el lote puede ser existente o nuevo. La búsqueda de sugerencias es parcial, sin distinguir mayúsculas y tolerante a espacios y guiones solo para localizar; no altera automáticamente el código real. Al editar el texto después de escoger un lote, se conserva el identificador únicamente mientras el texto siga coincidiendo con ese lote.

## 11. Series

Una serie pertenece a un producto, una bodega y opcionalmente un lote. Su número es único por producto. El estado proviene del catálogo de estados de serie: disponible, reservada, vendida, devuelta o baja.

Una entrada crea o registra series nuevas; una salida solo puede escoger series existentes y disponibles. El control por series exige cantidades enteras y una serie por unidad base. En `LOTE_Y_SERIE`, cada serie queda asociada a un lote y la distribución debe cuadrar.

## 12. Flujo Nuevo producto

1. El usuario inicia desde el catálogo y opcionalmente comprueba un código de barras.
2. El ViewModel carga catálogos de la empresa activa.
3. Se capturan datos generales, clasificación, inventario, presentación base, presentaciones adicionales y precios.
4. Opcionalmente se activa Registrar inventario inicial y se distribuye por bodega, lote y serie según el control.
5. `IProductService.GuardarProductoAsync` valida pertenencia multiempresa, catálogos, presentaciones y precios.
6. El servicio guarda producto y relaciones dentro de una transacción.
7. Si hay inventario inicial, agrega movimientos confirmados reales antes del commit.

El formulario normaliza mayúsculas en los campos definidos y el servicio vuelve a normalizar datos clave. Cancelar o cerrar descarta el estado no confirmado y restablece el scroll al ocultarse.

## 13. Inventario inicial

No existe una tabla especial de inventario inicial. Es una entrada física con tipo y origen `INVENTARIO_INICIAL`, detalle por presentación y distribuciones de lote/serie. Actualiza existencia, costo promedio y Kardex de forma atómica.

En Editar, Completar inventario inicial solo está disponible mientras no exista un movimiento confirmado distinto de inventario inicial. Permite registrar una presentación repetida porque corrige una omisión de la carga anterior. La captura no persiste lotes ni series hasta confirmar el conjunto.

## 14. Editar producto

Editar carga el producto filtrado por `producto_id` y `empresa_id`. Permite mantener datos maestros, precios, mínimos y ubicación. No permite sustituir directamente el tipo de control cuando existen stock o historia; debe usarse Convertir tipo de control.

Las operaciones disponibles son:

- Completar inventario inicial.
- Registrar ajuste.
- Ver Kardex.
- Convertir tipo de control.
- Corregir lotes / series, visible solo cuando el control no es NORMAL.

## 15. Ajustes de inventario

Un ajuste confirmado crea cabecera, detalle y movimiento real dentro de una transacción.

### Entrada

Admite lote existente o nuevo. Para lotes múltiples, la suma asignada debe igualar la cantidad requerida. Las series nuevas deben ser únicas. En `LOTE_Y_SERIE`, las series se vinculan con los lotes capturados. El costo de referencia ayuda al usuario, pero el valor confirmado se valida antes de persistir.

### Salida

Trabaja exclusivamente con inventario existente. Los lotes se precargan y el usuario indica cantidades sin exceder disponible. Las series se seleccionan entre las disponibles. En control combinado, la distribución por lote se deriva de las series seleccionadas. No se crean lotes ni series durante una salida.

El permiso `INVENTARIO_REGISTRAR_AJUSTE` se exige en Infrastructure, no solo en la interfaz.

## 16. Catálogo de motivos

Los motivos pertenecen a uno de estos tipos:

- `INVENTARIO_INICIAL_ADICIONAL`
- `AJUSTE_ENTRADA`
- `AJUSTE_SALIDA`
- `CONVERSION_CONTROL`
- `CORRECCION_LOTE_SERIE`

Existen motivos estructurales globales y motivos propios de empresa. Las consultas muestran los activos globales o de la empresa activa y del tipo requerido. Crear un motivo requiere `INVENTARIO_CREAR_MOTIVO`; el nombre se normaliza y no puede duplicar uno visible del mismo tipo. Las operaciones conservan el nombre del motivo como snapshot.

## 17. Conversión del tipo de control

La conversión clasifica el stock existente sin cambiar su cantidad total. Se ejecuta con aislamiento `Serializable` y requiere `INVENTARIO_CONVERTIR_TIPO_CONTROL`.

Transiciones admitidas:

| Actual | Destinos |
|---|---|
| NORMAL | LOTE, SERIE, LOTE_Y_SERIE |
| LOTE | NORMAL, LOTE_Y_SERIE |
| SERIE | NORMAL, LOTE_Y_SERIE |
| LOTE_Y_SERIE | NORMAL, LOTE, SERIE |

La distribución asignada debe coincidir exactamente con el stock por bodega. Series requiere stock entero. Si se desconoce una porción del lote, la regularización está explícitamente marcada y exige `INVENTARIO_CREAR_LOTE_REGULARIZACION`. Una reducción de control exige cero stock, cero reservas y ausencia de historia relevante; por ello no reescribe trazabilidad existente.

## 18. Corrección controlada

Corregir lotes o series cambia identificadores o fechas, no cantidades. Cada corrección valida empresa, producto, motivo y permiso (`INVENTARIO_CORREGIR_LOTE` o `INVENTARIO_CORREGIR_SERIE`) y registra valores anteriores y nuevos en `correcciones_datos_inventario`.

Los historiales siguen apuntando al identificador del lote o serie; por eso muestran el dato corregido sin alterar cantidades ni snapshots de movimientos. La operación confirmada no se elimina ni se reemplaza.

## 19. Kardex

`IInventoryService.ObtenerKardexAsync` consulta detalles de movimientos filtrados por empresa y producto, con filtros opcionales de establecimiento, bodega, fechas, tipo, origen y documento. Devuelve fecha, número, tipo, origen, bodega, presentación, entrada/salida base, stocks anterior/nuevo, costos anterior/nuevo, usuario y observación.

Es una consulta de historial; no modifica estado. La ventana usa la infraestructura global de scrollbars para desplazamiento horizontal y vertical.

## 20. Movimientos y transacciones

Una operación física genera `movimientos_inventario` y sus detalles. Cada movimiento afecta una sola bodega. Los detalles conservan cantidades de presentación, factor, cantidad base, costos y stocks anterior/nuevo; lotes y series se registran en tablas hijas.

Los números de movimiento provienen del mecanismo de secuenciales, nunca de `MAX(...) + 1`. Guardado con inventario inicial, ajustes, conversiones y correcciones usan una transacción por operación. Ante validación, concurrencia o conflicto de integridad se revierte el conjunto.

## 21. ProductsView

El catálogo combina encabezado, descripción, botón Nuevo producto, KPIs, búsqueda, filtro de estado, orden y paginación. La carga y búsqueda muestran un indicador visual y el flujo Editar usa un loading bloqueante KONTAXPRO para impedir solicitudes duplicadas.

La consulta se ejecuta en servidor mediante `ProductoCatalogoQuery`. Después de obtener la página se realizan dos consultas agrupadas por los IDs de esa página: nombres comerciales de presentaciones y precios por lista de la presentación base. No se detectó una consulta por cada producto.

Las columnas visibles son Producto, Categoría, Unidad, Stock, Costo, Precios, IVA, Estado y Acciones. Código no se muestra como columna independiente; sigue siendo buscable.

## 22. KPIs

| KPI | Regla actual |
|---|---|
| Todos | Productos dentro del filtro de estado y empresa. |
| Stock bajo | Alguna bodega operativa tiene disponible mayor que cero, mínimo mayor que cero y disponible menor o igual al mínimo. |
| Sin stock | La suma disponible de las bodegas operativas es menor o igual a cero. |
| Por caducar | Existe un lote activo con stock, en bodega operativa, cuya caducidad está entre hoy y los días de alerta. |

Por caducar cuenta productos, no cantidad de lotes. Un lote agotado no participa.

## 23. Búsqueda, orden y paginación

La búsqueda tiene debounce de 300 ms, cancelación y descarte de respuestas obsoletas. Usa coincidencia parcial sin distinguir mayúsculas sobre código, nombre, modelo, descripción, categoría, marca, unidad, presentación, código de presentación y código de barras. Una coincidencia exacta de barcode se prioriza.

El orden se ejecuta en servidor por código, producto, categoría, unidad, stock, costo promedio, precio base o estado. La página inicia en 1 y los tamaños disponibles son 25, 50 y 100. El paginador genera un conjunto compacto de números y elipsis.

## 24. Columna Producto, estados y acciones

Producto muestra nombre con marca, modelo o especificación y hasta tres presentaciones; las restantes aparecen como `+N` con detalle emergente. Los precios base se presentan una lista por línea.

Estado usa badges semánticos. Las acciones Editar y Activar/Inactivar usan iconos blancos sobre colores sólidos, tooltip y confirmación mediante `IMessageDialogService`. El cambio de estado siempre se filtra por empresa.

## Reglas invariantes de PRODUCTOS V1

- Toda consulta o mutación de producto se delimita por la empresa activa.
- Desktop y sus ViewModels no acceden directamente a `KontaxDbContext`.
- Stock confirmado y costo no se editan directamente desde el formulario.
- Existe una sola presentación base por producto y su factor es uno.
- Los códigos de producto son únicos por empresa; los barcodes no nulos son únicos por empresa.
- Precios pertenecen a presentación y lista; costos pertenecen al producto.
- Cantidades y costos físicos se normalizan a unidad base.
- Lotes y series mantienen trazabilidad por producto y bodega.
- Los movimientos confirmados no se reescriben ni se eliminan físicamente.
- Correcciones de lote o serie dejan registro de valores anterior y nuevo.
- Las operaciones físicas que afectan varias tablas son transaccionales.
- Los secuenciales no se calculan con `MAX + 1`.
- Los ViewModels usan servicios globales de mensajes, notificaciones y loading; no usan `MessageBox` nativo.
- El cambio de tipo de control con existencia o historia se realiza mediante conversión controlada.

## 25. Tablas involucradas

| Tabla | Propósito | Escritura principal |
|---|---|---|
| `s_inventario.productos` | Maestro y comportamiento del producto | Guardar/editar producto; conversión de control |
| `productos_presentaciones` | Unidades comerciales y factores | Guardar/editar producto |
| `productos_impuestos` | Tarifa vigente del producto | Guardar/editar producto |
| `listas_precio` | Listas comerciales por empresa | Seeder/catálogo |
| `productos_presentaciones_precios` | Precio por presentación y lista | Guardar/editar producto |
| `productos_costos` | Últimos costos y promedio | Entradas de inventario |
| `bodegas` | Ubicaciones físicas operativas | Configuración/seeder |
| `productos_existencias` | Stock, reserva, mínimo y ubicación | Operaciones; editor solo mínimo/ubicación |
| `productos_lotes` | Identidad y fechas del lote | Entradas, conversión y corrección |
| `productos_lotes_existencias` | Stock de lote por bodega | Entradas y salidas |
| `productos_series` | Identidad, bodega y estado de serie | Entradas, salidas, conversión y corrección |
| `movimientos_inventario` | Cabecera histórica del hecho físico | Inventario inicial y ajustes |
| `movimientos_inventario_detalles` | Cantidades, stocks y costos snapshot | Inventario inicial y ajustes |
| `movimientos_inventario_detalles_lotes` | Distribución histórica por lote | Operaciones con lote |
| `movimientos_inventario_detalles_series` | Series involucradas | Operaciones con serie |
| `ajustes_inventario` | Cabecera del ajuste | Registrar ajuste |
| `ajustes_inventario_detalles` | Detalle funcional del ajuste | Registrar ajuste |
| `conversiones_control_inventario` | Cabecera y snapshot de conversión | Convertir control |
| `conversiones_control_inventario_detalles` | Distribución por lote | Convertir control |
| `conversiones_control_inventario_series` | Series de la conversión | Convertir control |
| `s_catalogos.motivos_operacion_inventario` | Motivos globales o de empresa | Seeder y creación autorizada |
| `correcciones_datos_inventario` | Auditoría de corrección lote/serie | Corregir lote o serie |

## 26. Servicios

| Servicio | Proyecto | Responsabilidad y patrón |
|---|---|---|
| `IProductService` / `ProductService` | Application / Infrastructure | Detalle, guardado transaccional, estado, barcode, similares y catálogo paginado. Usa `IDbContextFactory`. |
| `IProductCatalogService` / `ProductCatalogService` | Application / Infrastructure | Categorías, marcas, unidades, tarifas, listas, bodegas y altas rápidas de categoría/marca. Usa contextos por operación. |
| `IInventoryService` / `InventoryService` | Application / Infrastructure | Inventario inicial, Kardex, ajustes, control, conversiones, correcciones y motivos. Usa contextos y transacciones por operación. |
| `IMessageDialogService` | Application / Desktop | Mensajes modales personalizados y confirmaciones. |
| `INotificationService` | Application / Desktop | Notificaciones temporales no bloqueantes. |
| `ILoadingService` | Application / Desktop | Loading bloqueante con alcance descartable. |
| `CurrentSession` | Application | Identidad activa, empresa, establecimiento, bodega, caja, roles y permisos. |

## 27. Pruebas

| Área | Casos cubiertos por la suite actual |
|---|---|
| Producto nuevo | Factor/costo, promedio ponderado, fechas opcionales, caducidad y barcode interno. |
| Presentaciones y precios | Métodos permitidos, distribución y advertencia de equivalencia comercial. |
| Catálogo | Presentaciones `3 + N`, búsqueda flexible, campos indexados, KPIs y paginación. |
| Inventario inicial | Regla para completar antes de movimientos posteriores y cálculo del costo pendiente. |
| Lotes | Normalización de búsqueda, conflictos, entradas y salidas multilote. |
| Series | Unicidad, selección existente, cantidades enteras y vínculo lote-serie. |
| Ajustes | Cuadre asignado/requerido para entrada y salida según control. |
| Conversión | Transiciones, reservas, distribución, series fraccionarias y reducción con historia. |
| Motivos | Visibilidad global/empresa, estado, tipo, unicidad, códigos aprobados y snapshot. |

La suite actual prueba reglas puras. No contiene una prueba integral PostgreSQL que reconcilie en una operación completa existencia del producto, suma por lotes y conteo de series.

## Fuera de alcance de PRODUCTOS V1

PRODUCTOS V1 está finalizado para alta, edición, catálogo y operaciones de inventario descritas. Quedan fuera de esta versión:

- Integración completa con compras, ventas y transferencias, que corresponde a sus propios módulos.
- Pruebas de integración PostgreSQL de reconciliación completa entre stock, lotes y series.
- Aplicación uniforme en Infrastructure de los permisos ya catalogados para consultar Kardex y completar inventario inicial.
- Decisión explícita sobre alinear la validación de precios con la precisión persistida `numeric(18,2)` si se desea guardar más de dos decimales.

Estos puntos no autorizan rediseñar Productos; deben tratarse mediante requerimientos concretos.

## Estado de auditoría de cierre

**Fecha:** 2026-08-03

| Verificación | Resultado |
|---|---|
| Restore | Correcto; todos los proyectos estaban actualizados. |
| Build | Correcto; 0 advertencias y 0 errores. |
| Tests | Correcto; 99 superadas, 0 fallidas y 0 omitidas. |
| EF Core | Correcto; el modelo no tiene cambios pendientes respecto de `InitialCreate`. |

Observaciones de auditoría:

- No se detectó acceso directo a EF desde ViewModels ni uso residual de `MessageBox` nativo en Desktop.
- No se detectó patrón N+1 por producto en el catálogo: la página se completa con consultas agrupadas.
- `INVENTARIO_REGISTRAR_AJUSTE`, conversión y correcciones se exigen en Infrastructure.
- **PENDIENTE DE CORRECCIÓN:** `INVENTARIO_VER_KARDEX` e `INVENTARIO_AGREGAR_ENTRADA_INICIAL` están sembrados, pero las rutas actuales de Kardex e ingreso inicial no llaman a la validación de permiso en Infrastructure.
- StructuralSeeder y DemoSeeder presentan patrones idempotentes de búsqueda/actualización. DemoSeeder solo se invoca cuando el ambiente es `Development`.
