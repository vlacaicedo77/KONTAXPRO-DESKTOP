# Compras V1 — diseño e implementación

## Estado

Documento vivo de implementación. No constituye todavía la auditoría definitiva `COMPRAS_V1.md`.

- Inicio: 6 de agosto de 2026.
- Rama inspeccionada: `dev`.
- Commit base: `b0caf37a3f229a89beaa33b0e04a61e9c927a5a1`.
- Cambio previo preservado: `promp.md` modificado por el usuario.
- Línea base: restore correcto; build correcto con 0 advertencias y 0 errores usando un directorio de salida temporal, porque una instancia abierta de KONTAXPRO bloqueaba los binarios ordinarios de `Desktop/bin`.
- Estado al 7 de agosto de 2026: fases funcionales implementadas, migración aplicada en la base local de Development y validación final en curso.

## 1. Principios funcionales

1. Una **Compra** representa el documento recibido y la obligación comercial.
2. Una **Recepción** representa el ingreso físico de mercadería.
3. Guardar o importar una Compra no modifica stock, costos ni Kardex.
4. Solo confirmar una Recepción produce un movimiento físico.
5. Una Compra puede tener cero, una o varias Recepciones.
6. La importación XML y la captura manual terminan en el mismo agregado Compra.
7. El XML es el flujo recomendado, pero nunca crea proveedores o productos sin confirmación.
8. El Kardex continúa derivándose de `movimientos_inventario`; no se crea uno paralelo.
9. Una Compra confirmada conserva snapshots y no se edita libremente.
10. Empresa, establecimiento, usuario, proveedor, bodega y productos se validan nuevamente en Infrastructure.

```text
XML / captura manual
        ↓
      Compra
        ↓  (sin efecto físico)
Recepción 1 + Recepción 2 + ...
        ↓
Movimiento de inventario por recepción y bodega
        ↓
Existencias + lotes + series + costos
        ↓
Kardex general
```

## 2. Resultado de la exploración

### Elementos reutilizables

- `Tercero`, `TerceroIdentificacion` y `EmpresaTercero` para proveedor global y ancla empresarial.
- `IProveedorService` y Proveedores V1 para buscar, crear o habilitar el perfil Proveedor.
- `ClaveIdentidadTercero` para equivalencia entre cédula y RUC natural.
- `Producto`, `ProductoPresentacion`, impuestos, bodegas, existencias, lotes, series y costos.
- `ProductFormView` y `ProductFormViewModel`, que ya publican `ProductSaved` y permiten volver al consumidor sin duplicar el formulario.
- `MovimientoInventario` y sus detalles de lote/serie como única fuente de Kardex.
- secuenciales internos transaccionales con bloqueo `FOR UPDATE`.
- `CurrentSession`, eventos de cambio de empresa y permisos persistidos.
- servicios globales de mensajes, notificaciones y loading.
- estilos, temas, `AdaptiveGrid` y `ResponsiveDataGridBehavior`.
- entidades base `DocumentoRecibidoSri`, `Compra`, detalles e impuestos.

### Vacíos encontrados

- no existe servicio de Compras;
- no existe parser XML;
- no existe servicio de almacenamiento de XML recibido;
- no existen Recepciones;
- no existen equivalencias código de proveedor–presentación;
- no existe motor de reconocimiento en lote;
- no existe UI de Compras;
- no existen pruebas de Compras;
- el menú contiene opciones visuales sin rutas funcionales;
- el esqueleto actual guarda `BodegaId` en la línea de Compra y presupone estado `CONFIRMADA`, lo que mezcla factura y recepción;
- el ingreso de inventario existente está orientado a inventario inicial/ajustes y no debe invocarse como una segunda transacción desde Compras.

### Almacenamiento documental

Existe la abstracción de almacenamiento de comprobantes electrónicos emitidos, pero no una implementación de filesystem reutilizable. Compras introducirá un contrato específico para XML recibido y mantendrá la ruta física fuera de PostgreSQL. En base solo se conservarán ruta relativa, SHA-256, tamaño y metadatos.

## 3. Modelo objetivo

### `documentos_recibidos_sri`

Se reutiliza y amplía como snapshot técnico del XML recibido. No es una Compra.

Datos persistidos relevantes:

- empresa y proveedor global opcional;
- tipo y número de comprobante;
- clave de acceso única;
- ambiente y tipo de emisión;
- RUC, razón social y nombre comercial del emisor;
- identificación y razón social del receptor;
- establecimiento, punto de emisión, secuencial y direcciones informativas;
- fechas de emisión/autorización cuando estén disponibles;
- subtotal, IVA, propina e importe total;
- moneda;
- presencia de firma, sin afirmar validez criptográfica;
- estado de validación local;
- ruta relativa, SHA-256, tamaño y fecha de importación;
- estado de procesamiento.

Protecciones:

- índice único de clave de acceso;
- índice único de hash del archivo;
- ninguna ruta absoluta;
- ningún XML binario o texto completo en PostgreSQL.

### `compras`

Cabecera empresarial común para importación y captura manual:

- empresa, establecimiento, relación empresa–tercero y usuario;
- documento recibido opcional;
- tipo `FACTURADA | SIN_FACTURA`;
- tipo/número de comprobante;
- fechas de emisión, ingreso y vencimiento opcional;
- subtotales, descuentos, impuestos y total;
- condición contado/crédito;
- estado y datos de anulación;
- `xmin` como versión de concurrencia.

Estados:

- `BORRADOR`;
- `PENDIENTE_RECEPCION`;
- `PARCIALMENTE_RECIBIDA`;
- `RECIBIDA`;
- `ANULADA`.

Una compra sin líneas inventariables queda `RECIBIDA` al confirmarse porque no requiere ingreso físico.

### `compras_detalles`

La línea conserva el documento comercial, no una bodega:

- orden original;
- códigos principal y auxiliar del proveedor;
- descripción original;
- estado de reconocimiento;
- producto y presentación opcionales;
- indicador inventariable/no inventariable;
- cantidad de presentación, factor y cantidad base como snapshots;
- precio, descuento, subtotal y costo efectivo revisado;
- bonificación;
- timestamps.

`BodegaId` se retira de esta tabla y se traslada a la Recepción.

Estados de reconocimiento:

- `RECONOCIDA`;
- `SUGERIDA`;
- `NO_RECONOCIDA`;
- `NO_INVENTARIABLE`.

Una sugerencia textual nunca se transforma automáticamente en `RECONOCIDA`.

### `compras_detalles_impuestos`

Conserva snapshots SRI por línea: códigos, tipo de cálculo, porcentaje/valor específico, base imponible y valor.

### `compras_pagos`

Permite uno o varios nodos del XML:

- código SRI de forma de pago;
- valor;
- plazo opcional;
- unidad de tiempo opcional.

La condición pertenece a la Compra, no al Proveedor.

### `proveedores_productos_equivalencias`

Memoria explícitamente confirmada:

- empresa;
- tercero proveedor global;
- código del proveedor normalizado;
- tipo `PRINCIPAL | AUXILIAR`;
- presentación de producto;
- descripción original opcional;
- timestamps y usuario que confirma.

Índice único: `(empresa_id, tercero_id, tipo_codigo, codigo_proveedor_normalizado)`.

La empresa forma parte de la clave porque la presentación pertenece a una empresa. Nunca se comparte una equivalencia con otro proveedor ni se crea desde una similitud textual sin confirmación.

### `compras_recepciones`

Cabecera física:

- compra, empresa, bodega destino y usuario;
- fecha, observación y estado;
- movimiento de inventario único;
- timestamps y `xmin`.

Una Recepción afecta una sola bodega, igual que un movimiento de inventario.

### `compras_recepciones_detalles`

- recepción y detalle de Compra;
- producto/presentación;
- cantidad de presentación;
- factor histórico;
- cantidad base;
- costo unitario base y total aplicados;
- bonificación.

La suma acumulada confirmada no puede superar la cantidad facturada pendiente.

### Lotes y series de recepción

Las tablas hijas relacionan cada detalle recibido con los lotes y series creados o reutilizados. La confirmación también escribe las tablas hijas del movimiento general para que Kardex y trazabilidad física permanezcan unificados.

## 4. Importación XML

### Contrato

Application define un lector de comprobantes recibidos. Infrastructure implementa el parser. Desktop solo selecciona un archivo y consume el resultado.

### Seguridad

- tamaño máximo configurable y razonable;
- stream vacío rechazado;
- `XmlReader` con `DtdProcessing.Prohibit`;
- `XmlResolver = null`;
- límite de caracteres del documento;
- XML mal formado traducido a error funcional;
- tipo documental distinto de factura rechazado;
- ninguna referencia externa es resuelta;
- `CancellationToken` respetado;
- decimales leídos con cultura invariante;
- el contenido nunca se ejecuta.

### Formatos previstos

El lector acepta:

- factura como elemento raíz;
- envoltura de autorización con la factura dentro de CDATA/texto.

La arquitectura separa el lector de factura del orquestador para incorporar otros tipos documentales posteriormente.

### Validaciones locales

- `codDoc = 01`;
- clave de acceso normalizada de 49 dígitos;
- número compuesto por establecimiento, punto y secuencial;
- receptor equivalente a la empresa activa mediante identidad canónica;
- ambiente visible y advertido si es Pruebas;
- presencia de `Signature` detectada, no validada criptográficamente;
- totales contrastados con tolerancia monetaria explícita de un centavo.

No se afirmará que el comprobante está autorizado por el SRI solo por estructura, clave o firma presente.

## 5. Resolución del proveedor

1. Buscar el RUC con `IProveedorService.BuscarPorRucAsync`.
2. Si el tercero ya tiene perfil Proveedor activo, utilizarlo.
3. Si existe como tercero/cliente sin perfil, ofrecer habilitar Proveedor mediante el formulario existente.
4. Si no existe, ofrecer Crear proveedor con datos seguros precargados.
5. Nunca crear automáticamente.
6. Al volver, conservar el XML, los emparejamientos y la etapa del asistente.
7. Al guardar la Compra, Infrastructure vuelve a exigir un proveedor activo y crea bajo demanda la relación neutral `EmpresaTercero` de la empresa activa.

## 6. Motor de reconocimiento

El servicio resuelve todas las líneas en lote para evitar N+1:

1. código de barras exacto y único de una presentación activa que permite compra;
2. equivalencia confirmada para empresa + proveedor + código;
3. candidatos por descripción/códigos/nombre/modelo;
4. acción manual.

Resultados:

- coincidencia exacta única o equivalencia válida: reconocida automáticamente;
- código ambiguo, producto/presentación inactivos o similitud: requiere revisión;
- sin resultado: no reconocida.

## 7. Integración con ProductForm

El asistente mantiene una instancia de `ProductFormViewModel` como editor superpuesto. Precarga únicamente nombre sugerido y posible código de barras.

`ProductSaved` permite:

1. volver al asistente sin reconstruirlo;
2. volver a ejecutar el motor de reconocimiento;
3. mostrar la presentación guardada como coincidencia exacta o sugerencia;
4. permitir su confirmación explícita y memorizar la equivalencia cuando el usuario lo solicite.

Cancelar vuelve al asistente sin perder estado.

## 8. Confirmación de recepción

La frontera transaccional estará en Infrastructure y realizará, en un solo contexto y transacción:

1. autorizar usuario, empresa y permiso;
2. bloquear/volver a leer Compra y cantidades recibidas;
3. validar estado, pendientes y doble confirmación;
4. validar bodega, productos y presentaciones dentro de la empresa;
5. crear Recepción y detalles;
6. crear/reutilizar lotes y validar fechas;
7. crear series únicas y comprobar su cantidad;
8. actualizar existencias generales y por lote;
9. calcular costos agrupando por producto;
10. crear un movimiento `COMPRA`, ligado a la Compra y enlazado uno a uno desde la Recepción;
11. crear detalles de Kardex, lotes y series;
12. actualizar el estado derivado de Compra;
13. registrar auditoría;
14. confirmar.

Cualquier fallo revierte el conjunto.

El asistente de importación incorpora un cuarto paso, **Recibir mercadería**.
Por defecto propone recibir la totalidad en la bodega activa y permite capturar
lotes, caducidades y series según la configuración del producto. El usuario
puede desmarcar la recepción inmediata para guardar la Compra como
`PENDIENTE_RECEPCION` y utilizar después el flujo de recepción total o parcial.

Cuando se recibe en el asistente, Compra, CxP, movimiento de deuda, asiento,
Recepción, existencias, costos, lotes/series, Kardex y auditoría comparten el
mismo `KontaxDbContext` y la misma transacción serializable. La Recepción no
genera un segundo asiento: el hecho contable continúa siendo la Compra.

## 9. Costos y bonificaciones

- `UltimoPrecioCompra`: último precio de presentación convertido a unidad base.
- `UltimoCostoEfectivo`: costo efectivo confirmado por unidad base.
- `CostoPromedio`: promedio ponderado sobre existencia total del producto.
- las entradas pagadas y bonificadas del mismo producto se agrupan antes de calcular el promedio, evitando resultados dependientes del orden de líneas;
- las unidades bonificadas aumentan el denominador sin agregar costo;
- el factor se guarda como snapshot y no se relee para reconstruir historia.

Los impuestos se conservan separados. Mientras no exista una política formal de impuesto recuperable/no recuperable, Compras exige revisar el costo efectivo y no incorpora impuestos silenciosamente.

## 10. Cuentas por pagar

El modelo existente de CxP está fuera del alcance operativo solicitado para Compras V1. La Compra conserva condición, plazo y vencimiento, pero esta fase no implementará pagos, cuotas ni aplicaciones. La creación automática de CxP se integrará cuando se defina el punto formal de confirmación comercial y el tratamiento de compras de contado, evitando dejar deudas falsas.

## 11. Seguridad y concurrencia

Permisos implementados:

- `COMPRAS_VER`;
- `COMPRAS_CREAR`;
- `COMPRAS_EDITAR`;
- `COMPRAS_IMPORTAR_XML`;
- `COMPRAS_RECIBIR`;
- `COMPRAS_ANULAR`;
- `COMPRAS_VER_COSTOS`.

El `StructuralSeeder` los asigna a `ADMINISTRADOR`; `GUARDALMACEN` recibe `COMPRAS_VER` y `COMPRAS_RECIBIR`.

Las escrituras validan sesión, empresa activa, vínculo usuario–empresa y permiso persistido. Compra y Recepción usan `xmin`. Clave de acceso, hash, equivalencias y movimiento por recepción poseen barreras únicas físicas.

Si cambia la empresa durante importación, edición o recepción, el ViewModel cancela operaciones, descarta respuestas tardías y cierra el flujo para impedir guardado cruzado.

## 12. UI implementada

### Catálogo

- encabezado Compras;
- botón principal Importar XML;
- alternativa Nueva compra manual;
- KPIs: total de Compras, pendientes, parciales, recibidas y valor registrado;
- búsqueda y filtro por estado ejecutados en servidor, con límite defensivo de 500 filas en V1;
- columnas esenciales Documento, Proveedor, Total, Estado y Acciones;
- columnas adaptativas Fecha, RUC, Condición y Recepción.

### Asistente XML

1. Comprobante y proveedor.
2. Productos y reconocimiento.
3. Revisión, condición y guardado.

El formulario manual reutiliza Compra/Detalle, permite líneas inventariables y no inventariables y nunca modifica stock. La Recepción muestra facturado, recibido, pendiente y recibir ahora; admite varios lotes mediante `LOTE|cantidad|elaboración|caducidad` y series mediante `SERIE` o `SERIE@LOTE`.

## 13. Pruebas ejecutadas

- parser seguro, formatos, múltiples impuestos/pagos, firma y decimales;
- receptor exacto y equivalencia cédula–RUC;
- duplicados por clave y hash;
- resolución de proveedor;
- reconocimiento exacto, equivalencia, sugerencia y ambigüedad;
- aislamiento por proveedor y empresa;
- recepción total/parcial, exceso y doble confirmación;
- lotes, múltiples lotes, series y duplicados;
- factor histórico, bonificación y costo ponderado;
- existencia, movimiento y Kardex únicos;
- almacenamiento físico, hash y bloqueo de traversal;
- regla de pendientes, varios lotes y cardinalidad de series;
- compilación completa de solución, incluyendo XAML: 0 advertencias, 0 errores;
- pruebas automatizadas de Compras: 37 correctas;
- suite completa final: 283 correctas y 5 PostgreSQL omitidas por configuración, sin errores. Una ejecución anterior tuvo un timeout aislado en una prueba preexistente de Proveedores; pasó aislada y también dentro de la repetición completa final.

Las pruebas PostgreSQL optativas no se habilitaron porque el proyecto exige una base aislada para ellas y se decidió no crear una base de pruebas. La migración real sí fue aplicada y validada en la base local de Development.

## 14. Limitaciones conscientes

- V1 importa automáticamente solo Factura electrónica.
- No valida criptográficamente XAdES.
- No consulta autorización externa ni hace scraping.
- No importa todavía la bandeja masiva del SRI.
- No implementa notas de crédito/débito, retenciones o liquidaciones desde XML.
- Pagos y aplicaciones permanecen pendientes; cada Compra ya genera su CxP,
  movimiento de origen y asiento contable automático en una sola transacción.
- No clasifica todavía Gastos o Activos con módulos especializados.
- El almacenamiento autoritativo en topología servidor/cliente requerirá configuración operativa del directorio compartido mientras no exista un servicio remoto de archivos.

## 15. Regla de mantenimiento

Este documento se actualizará después de cada fase importante con archivos, migraciones, pruebas y limitaciones reales. Ninguna capacidad se declarará comprobada por ejecución si solo fue revisada estáticamente.

## 16. Componentes implementados

### Application

- contratos del lector XML, importación, almacenamiento, reconocimiento, Compra y Recepción;
- DTOs de factura, impuestos, pagos, catálogo, captura manual, equivalencias y recepción;
- reglas puras de receptor, cuadre, normalización de códigos y similitud textual;
- permisos y acciones de auditoría de Compras.

### Infrastructure

- parser XML seguro y limitado a Factura código `01`;
- sesión temporal de importación ligada a usuario y empresa, con vigencia de 30 minutos;
- reconocimiento en lote por código de barras, equivalencia y similitud;
- persistencia de Compra importada/manual; el inventario solo se afecta al
  confirmar una Recepción;
- generación transaccional de CxP, `ORIGEN_DEUDA` y asiento automático de Compra;
- clasificación y cuenta histórica por línea para Gasto, Activo u Otro;
- almacenamiento original mediante escritura temporal + movimiento atómico, SHA-256 y ruta relativa;
- procesador transaccional reutilizable de Recepción con existencias, lotes,
  series, costos y movimiento general, usado tanto por la recepción posterior
  como por la recepción inmediata del XML;
- anulación append-only de Compras sin recepciones; las que ya movieron inventario se bloquean hasta tener un reverso formal;
- seeder de permisos;
- configuraciones EF, índices, restricciones y `xmin`.

### Desktop

- ruta real `Compras` para Nueva compra, Recepción, Importar XML y Compras registradas;
- catálogo, KPIs, búsqueda, filtro, badges y selección;
- asistente XML de cuatro pasos con proveedor y ProductForm superpuestos sin
  perder estado; el último paso recibe toda la mercadería por defecto o permite
  dejarla pendiente;
- captura manual;
- confirmación de recepción;
- diálogo de anulación con motivo obligatorio;
- cierre/cancelación del flujo ante cambio de empresa.

## 17. Migración y almacenamiento

Migración aplicada:

- `20260807053951_ComprasV1`;
- ambiente: `Development`;
- host: `localhost`;
- base: `kontax_desktop`.

La migración retira `compras_detalles.bodega_id`; esta eliminación es intencional porque la bodega pertenece a cada Recepción. `xmin` se mapea a la columna de sistema de PostgreSQL y no se crea como columna física.

Directorio documental:

1. `KONTAXPRO_DOCUMENTS_PATH`, si está definido;
2. `DocumentStorage:ComprasRoot` en configuración;
3. por defecto `data/documentos` relativo al directorio de la aplicación.

En una instalación Cliente/Servidor, todas las estaciones deben resolver este directorio a una ubicación compartida y protegida que sea accesible por el equipo que guarda y consulta documentos. La base conserva solo la ruta relativa.

## 18. Decisiones y límites pendientes

- Los pagos aplicados permanecen fuera de Compras V1; la CxP nace con toda
  compra y conserva condición, vencimiento y saldo de origen.
- La contabilización exige período abierto y cuentas configuradas para
  Inventario, Cuentas por pagar e IVA crédito tributario cuando corresponda.
- La anulación sin recepciones ni pagos genera `ANULACION_DEUDA` y un asiento
  inverso; no elimina ni reescribe movimientos históricos.
- Impuestos distintos de IVA, propina o cargos sin tratamiento contable se
  bloquean para evitar asientos incorrectos.
- La validación de firma informa presencia, no validez criptográfica ni autorización externa.
- La anulación de una Recepción requiere un movimiento reverso formal y no se simula.
- IVA permanece separado del costo hasta aprobar una política de recuperabilidad tributaria.
- Antes de la auditoría definitiva debe ejecutarse una prueba manual visual Light/Dark y a resoluciones objetivo con la aplicación reiniciada para cargar la migración y los permisos sembrados.
