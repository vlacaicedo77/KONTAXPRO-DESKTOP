# COMPRAS V1 — memoria funcional, técnica y visual

## 1. Propósito y estado

Este documento es la memoria consolidada del módulo **Compras V1** de KONTAXPRO Desktop. Describe el comportamiento implementado al 10 de agosto de 2026: diseño, experiencia de usuario, reglas comerciales, importación XML, captura manual, recepción física, inventario, costos, cuentas por pagar, contabilidad, seguridad, concurrencia, persistencia y pruebas.

La fuente de verdad continúa siendo el código, las configuraciones EF Core y las migraciones. Este documento sirve para mantener las decisiones del módulo y orientar su operación, mantenimiento y evolución.

Principio central:

> La Compra registra el documento y la obligación; la Recepción registra el ingreso físico.

## 2. Alcance funcional

Compras V1 incluye:

- catálogo principal con búsqueda, filtros, KPIs y acciones contextuales;
- importación de facturas electrónicas XML;
- validación local y consulta de autorización en el SRI;
- preparación automática del proveedor del XML;
- reconocimiento, búsqueda, creación y relación de productos;
- memoria de equivalencias entre códigos del proveedor y presentaciones;
- compras manuales respaldadas por factura;
- acceso separado a operaciones sin sustento tributario, que no se almacenan como Compras;
- clasificación de líneas inventariables y no inventariables;
- recepción inmediata desde el XML o posterior desde el catálogo;
- recepciones totales y parciales;
- lotes, caducidades y series;
- existencias, costos y Kardex mediante movimientos de inventario;
- cuenta por pagar y movimiento de origen;
- asiento contable automático;
- anulación de compras y reverso formal de recepciones;
- corrección de compras manuales mediante sustitución histórica.

No se implementan todavía devoluciones de compra, liquidaciones de compra, retenciones, notas de crédito o débito recibidas, pagos/aplicaciones completos, cuotas complejas, activos fijos ni bandeja masiva del SRI.

## 3. Arquitectura

### Domain

Contiene los agregados persistentes del esquema `s_compras`:

- `DocumentoRecibidoSri` y `DocumentoRecibidoSriPago`;
- `Compra`, `CompraDetalle` y `CompraDetalleImpuesto`;
- `ProveedorProductoEquivalencia`;
- `CompraRecepcion` y sus detalles de producto, lote y serie;
- estructuras futuras de liquidaciones, devoluciones y ajustes.

### Application

Expone contratos y DTOs, sin EF ni WPF:

- `ICompraService`;
- `ICompraImportacionService`;
- `ICompraProductoResolverService`;
- `ICompraRecepcionService`;
- `IComprobanteCompraXmlReader`;
- `IConsultaAutorizacionComprobanteSri`;
- `IArchivoCompraStorage`;
- modelos de catálogo, análisis XML, reconocimiento, guardado y recepción;
- reglas puras de identidad del receptor, cuadre, similitud, códigos de barras y descripción de productos.

### Infrastructure

- `CompraService`: catálogo, formularios, guardado XML/manual, sustitución y anulación;
- `CompraImportacionService`: análisis XML, autorización SRI y proveedor;
- `ComprobanteCompraXmlReader`: parser seguro;
- `ConsultaAutorizacionComprobanteSri`: consulta del comprobante autorizado;
- `CompraProductoResolverService`: reconocimiento, búsqueda y equivalencias;
- `CompraReceiptProcessor`: recepción transaccional reutilizable;
- `CompraRecepcionService`: consulta, confirmación y reverso de recepciones;
- `CompraAccountingProcessor`: CxP y contabilidad;
- `ArchivoCompraFileStorage`: almacenamiento físico seguro del XML;
- `ImportacionCompraStore`: sesión temporal del asistente.

Los servicios usan `IDbContextFactory<KontaxDbContext>` y contexto por operación. Los procesos complejos comparten contexto y transacción.

### Desktop

La interfaz está concentrada en:

- `ComprasView.xaml`;
- `ComprasViewModel`;
- modelos de línea para XML, compra manual y recepción.

El ViewModel usa CommunityToolkit.Mvvm, servicios por DI y overlays para formularios complementarios. No accede directamente al DbContext.

## 4. Relación entre Compra y Recepción

```text
XML autorizado o captura manual
              ↓
            Compra
      ┌───────┼────────┐
      ↓       ↓        ↓
     CxP   Asiento   Documento XML
              │
              └─ La Compra no modifica stock

Compra con mercadería
              ↓
Recepción 1 + Recepción 2 + ...
              ↓
Movimiento de inventario por recepción
              ↓
Existencias + lotes + series + costos + Kardex
```

Reglas:

1. Guardar una Compra no duplica el ingreso físico.
2. Cada Recepción afecta una sola bodega.
3. Una Compra puede recibirse total o parcialmente.
4. Cada Recepción confirmada genera exactamente un movimiento de inventario.
5. La Recepción no genera otro asiento contable: el hecho contable es la Compra.
6. Las líneas no inventariables no requieren Recepción.
7. Una Compra sin líneas inventariables queda `RECIBIDA` desde su registro.

## 5. Catálogo principal

El encabezado ofrece **Nueva compra manual** e **Importar XML**.

### KPIs filtrables

- Compras totales.
- Pendientes de recepción.
- Recibidas parcialmente.
- Recibidas.
- Total monetario registrado.

Al seleccionar un KPI se aplica el filtro correspondiente. La búsqueda admite documento, proveedor o identificación. Existe filtro por estado, actualización y limpieza de filtros.

### Tabla

Presenta fecha, documento/origen, proveedor/RUC, condición, total, estado, recepción y acciones. Las acciones visibles dependen del registro y de los permisos:

- editar/corregir compra manual elegible;
- recibir mercadería pendiente;
- revertir una recepción confirmada;
- anular compra.

La consulta está aislada por empresa y limitada defensivamente a 500 filas. El estado vacío utiliza el patrón visual común de Productos, Clientes y Proveedores con una sola explicación y botón **Limpiar filtros**.

## 6. Estados

### Compra

- `BORRADOR`: reservado por el modelo; no es el estado final del guardado actual.
- `PENDIENTE_RECEPCION`: tiene mercadería sin recibir.
- `PARCIALMENTE_RECIBIDA`: parte de la mercadería fue recibida.
- `RECIBIDA`: no queda mercadería pendiente o no contiene inventariables.
- `ANULADA`: operación revertida y conservada históricamente.

### Reconocimiento de una línea

- `RECONOCIDA`: producto/presentación confirmado.
- `SUGERIDA`: existe una coincidencia que requiere decisión.
- `NO_RECONOCIDA`: no se encontró relación.
- `NO_INVENTARIABLE`: se clasifica contablemente sin producto.

### Recepción

- `CONFIRMADA`.
- `ANULADA`.

### Documento recibido

- `PENDIENTE`.
- `PROCESADO`.
- `NO_APLICA` como estado previsto por el modelo.

## 7. Importación de factura electrónica

La importación acepta únicamente facturas electrónicas, código SRI `01`, como XML directo o dentro de una envoltura de autorización.

### Seguridad del parser

- límite de 5 MB;
- rechazo de contenido vacío o mal formado;
- `DtdProcessing.Prohibit`;
- `XmlResolver = null`;
- límite de caracteres;
- sin resolución de referencias externas;
- decimales con cultura invariante;
- cancelación cooperativa;
- validación de estructura, datos obligatorios y clave de acceso.

### Validación del receptor y ambiente

La identificación del receptor debe corresponder a la empresa activa. Se utiliza equivalencia canónica cuando aplica. Un comprobante de pruebas solo puede importarse en Development y se muestra como advertencia.

### Firma y autorización

Se detecta la firma y se valida el contenido del comprobante consultado contra el SRI. La respuesta se clasifica como:

- autorizado;
- no autorizado;
- pendiente de anulación;
- anulado;
- no encontrado;
- servicio no disponible.

Si el SRI está disponible, el comprobante debe constar autorizado y el contenido consultado debe coincidir con el XML. Un estado anulado, pendiente de anulación, no autorizado o inconsistente bloquea el registro. Si el servicio no está disponible, se conserva la advertencia de validación local; no se presenta como autorización confirmada.

La UI distingue **AUTORIZADO SRI** de **VALIDACIÓN LOCAL**.

### Duplicados

Se bloquean por:

- clave de acceso;
- SHA-256 del XML;
- vínculo único entre Compra y documento recibido;
- comprobante activo equivalente por empresa, proveedor, tipo y número.

La validación se ejecuta durante el análisis y nuevamente dentro de la transacción de guardado.

## 8. Asistente XML de cuatro pasos

### Paso 1 — Proveedor y comprobante

Muestra dos tarjetas principales:

- **Información del proveedor**, con RUC, razón social, dirección y condición de proveedor nuevo/existente;
- **Información del comprobante**, con número de documento destacado, fecha, clave de acceso, ambiente, validación, subtotal, descuento, impuestos y total.

El proveedor del XML se busca globalmente por identidad. Si es necesario, se verifica en fuentes oficiales, se crea o se reactiva el perfil Proveedor y queda listo para asociarse a la empresa. La interfaz informa si fue registrado automáticamente. Infrastructure vuelve a validar RUC, estado global y relación empresarial antes de guardar.

### Paso 2 — Relacionar productos

Cada línea muestra:

- orden;
- descripción y códigos del XML;
- cantidad facturada con máximo dos decimales visibles;
- producto/presentación relacionado;
- indicador por icono y tooltip;
- sugerencias con nombre comercial, marca, presentación y confianza;
- búsqueda manual mediante botón compacto de lupa;
- acción verde `+` para crear producto;
- opción no inventariable;
- bonificación solo cuando existe una relación inventariable;
- acción para limpiar la relación.

Seleccionar una sugerencia confirma la relación. Limpiarla elimina también `Es bonificación`, evitando que el estado sobreviva a una nueva selección.

Una línea no relacionada puede continuar únicamente si se marca como no inventariable y se completa su clasificación/cuenta contable.

### Paso 3 — Revisar compra

Resume documento, proveedor, líneas y valores. Permite indicar:

- contado o crédito;
- fecha de vencimiento con máscara `dd/MM/yyyy`;
- observación.

En crédito, la fecha de vencimiento es obligatoria y no puede ser anterior a la emisión. Al activar crédito, el foco se dirige a la fecha.

Los pagos declarados por el proveedor dentro del XML se conservan como metadatos del documento recibido; no determinan la condición comercial real de KONTAXPRO.

### Paso 4 — Recibir mercadería

Permite:

- recibir toda la mercadería inmediatamente, opción inicial;
- seleccionar bodega;
- indicar fecha con formato `dd/MM/yyyy`;
- agregar observación;
- revisar cantidades a recibir;
- configurar lotes, caducidad o series;
- desmarcar recepción inmediata y guardar para recepción posterior.

Si no existen líneas inventariables, no se exige bodega ni configuración física.

## 9. Reconocimiento de productos

El motor trabaja por lotes para evitar N+1 y aplica esta prioridad:

1. código de barras GS1 exacto y único;
2. equivalencia confirmada para empresa + proveedor + código;
3. coincidencias textuales como sugerencias;
4. búsqueda manual;
5. creación de producto.

Una similitud textual nunca relaciona automáticamente. Los productos o presentaciones inactivos no se aceptan. Los códigos ambiguos requieren decisión del usuario.

### Equivalencias

La memoria se identifica por:

`empresa + tercero proveedor + tipo de código + código normalizado`

La presentación relacionada debe pertenecer a la misma empresa. Las equivalencias se guardan explícitamente y pueden reemplazarse de manera controlada.

## 10. Creación de producto desde Compras

Se reutiliza el formulario oficial de Productos como overlay. Compras no crea un formulario alterno.

La descripción se analiza para sugerir:

- nombre comercial depurado;
- marca existente;
- presentación de compra;
- factor de conversión;
- presentación base;
- costo referencial.

Ejemplos como cajas, frascos y cantidades por empaque se descomponen para que la unidad base no herede incorrectamente el costo de la presentación mayorista.

Los códigos principal y auxiliar solo se precargan como código de barras cuando cumplen reglas GS1. Si ambos son candidatos, el usuario elige. Un código interno del proveedor no se convierte silenciosamente en código de barras.

El ingreso inicial de inventario permanece deshabilitado en este contexto: el stock se registra al confirmar la Recepción. Después de guardar el producto, el asistente conserva su estado y actualiza la línea correspondiente.

## 11. Compra manual

La ventana **Nueva compra manual** reutiliza el mismo lenguaje visual del asistente XML.

### Información de compra

- tipo fijo `FACTURADA`;
- establecimiento;
- tipo y número de comprobante;
- fecha de emisión;
- proveedor;
- condición contado/crédito;
- vencimiento;
- observación.

Toda compra manual exige Factura código `01` y número `EEE-PPP-SSSSSSSSS`. Cada segmento se completa con ceros a la izquierda. Los egresos o adquisiciones sin comprobante se registran en el flujo separado Operación sin sustento tributario.

### Proveedor

Se usa una búsqueda incremental por coincidencias, no un combo masivo. Al seleccionar un proveedor, el texto queda protegido y una X permite limpiar la selección. Si no existe, se abre el registro básico con RUC, razón social y correo opcional, usando verificación oficial.

### Detalle

Cada línea permite:

- descripción normalizada en mayúsculas;
- búsqueda de producto/presentación desde descripción o campo específico;
- X para romper la relación;
- cantidad y precio unitario;
- descuento;
- tarifa de impuesto, calculada automáticamente desde el producto;
- inventariable/no inventariable;
- clasificación `GASTO`, `ACTIVO` u `OTRO` y cuenta contable para no inventariables;
- creación de producto cuando no existe coincidencia.

Cantidad, precio, descuento e impuesto se presentan centrados. El resumen flotante conserva visibles subtotal, descuento, impuestos y total. Las filas crecen con el formulario y el scroll pertenece a la vista completa.

### Corrección de compra manual

Una compra manual solo es editable si:

- no está anulada;
- no tiene recepciones confirmadas;
- su cuenta por pagar permanece íntegra, pendiente y sin movimientos posteriores;
- no proviene de XML.

La corrección no reescribe historia. En una transacción serializable:

1. anula la versión anterior;
2. revierte su CxP y asiento;
3. crea una nueva Compra;
4. enlaza `CompraSustituidaId`;
5. crea nueva CxP y nuevo asiento;
6. registra auditoría.

Solo una sustituta puede apuntar a una compra anterior y ambas deben pertenecer a la misma empresa.

## 12. Líneas inventariables y no inventariables

### Inventariable

Exige:

- producto activo;
- presentación activa que permita compra;
- producto que maneje inventario;
- clasificación `INVENTARIO`;
- cuenta contable de inventario configurada;
- estado de reconocimiento `RECONOCIDA`.

### No inventariable

No lleva producto ni presentación. Exige:

- clasificación `GASTO`, `ACTIVO` u `OTRO`;
- cuenta contable activa, de la empresa y que acepte movimientos;
- estado `NO_INVENTARIABLE`.

La clasificación permite registrar correctamente servicios, gastos u otros conceptos aunque todavía no exista un módulo especializado de activos fijos.

## 13. Recepción posterior

Desde el catálogo se abre **Confirmar recepción**. La ventana presenta:

- compra y proveedor;
- bodega destino;
- fecha y observación;
- por línea: facturado, recibido, pendiente y a recibir;
- acceso a Configurar control cuando el producto usa lotes o series.

Solo se envían líneas con cantidad mayor que cero. La suma histórica confirmada no puede superar lo facturado. Dos recepciones pueden completar una compra parcial.

La bodega de una Compra debe permitir venta facturada. El inventario adquirido sin comprobante usa exclusivamente bodegas no facturables dentro del flujo separado de Tesorería.

`OperacionUuid` hace idempotente la confirmación y bloquea doble clic o reintentos duplicados.

## 14. Configurar control: lotes y series

La ventana de control adapta su contenido al producto.

### Lotes

- muestra cantidad asignada, requerida y pendiente en tiempo real;
- el pendiente se vuelve rojo si existe exceso;
- inicia el foco en Número de lote;
- muestra lotes existentes y stock en la bodega;
- sugiere coincidencias mientras se escribe;
- al elegir una sugerencia, enfoca Cantidad base;
- Cantidad base inicia en cero, queda vacía al enfocar y vuelve a cero si pierde foco vacía;
- permite distribuir una recepción entre varios lotes;
- captura elaboración y caducidad cuando corresponde;
- detecta nombres similares y exige confirmación explícita antes de crear un lote distinto.

### Series

- exige una serie por cada unidad base recibida;
- no acepta duplicados;
- permite asociar la serie con lote cuando el producto usa ambos controles;
- valida la cardinalidad antes de aplicar.

La configuración permanece en memoria al moverse entre pasos o si el guardado falla. Solo se descarta al cerrar o reiniciar el flujo.

## 15. Inventario, costos y Kardex

La confirmación de Recepción ejecuta en una sola transacción:

1. valida permiso, empresa, compra, bodega y pendientes;
2. crea cabecera y detalles de Recepción;
3. crea o reutiliza lotes;
4. crea series disponibles;
5. actualiza existencias generales y por lote;
6. actualiza costos;
7. crea un movimiento de inventario `COMPRA`;
8. crea detalles de movimiento, lote y serie;
9. enlaza Recepción y movimiento;
10. actualiza el estado de Compra;
11. registra auditoría.

La trazabilidad física conserva claves compuestas de empresa, compra, detalle, producto y bodega para impedir cruces entre empresas o líneas no relacionadas.

### Costos

- cantidades y costos usan precisión `NUMERIC(18,6)`;
- valores monetarios usan `NUMERIC(18,2)`;
- el factor de conversión se guarda como snapshot;
- `UltimoPrecioCompra` refleja el precio convertido a unidad base;
- `UltimoCostoEfectivo` refleja el costo efectivo recibido;
- `CostoPromedio` se actualiza mediante promedio ponderado;
- líneas pagadas y bonificadas del mismo producto se agrupan antes del cálculo;
- la bonificación aumenta unidades sin agregar costo.

## 16. Cuenta por pagar y contabilidad

Toda Compra genera, en la misma transacción:

- `CuentaPorPagar` con origen `COMPRA`;
- movimiento inicial `ORIGEN_DEUDA`;
- saldo por el total de la Compra;
- asiento automático balanceado.

La fecha de vencimiento es la indicada para crédito; si no existe, se usa la fecha de emisión. V1 no aplica pagos ni cuotas desde este formulario.

### Asiento de Compra

- Debe: cuentas de Inventario, Gasto, Activo u Otro agrupadas por cuenta.
- Debe: IVA crédito tributario, cuando existe IVA.
- Haber: Cuentas por pagar.

Se requieren:

- período contable abierto para la fecha de emisión;
- cuenta `CUENTAS_POR_PAGAR`;
- cuenta `INVENTARIO` cuando existen líneas inventariables;
- cuenta `IVA_CREDITO_TRIBUTARIO` cuando existe IVA;
- cuentas de línea activas, empresariales y con movimientos habilitados.

El secuencial se obtiene transaccionalmente con `INSERT ... ON CONFLICT ... RETURNING`; nunca usa `MAX + 1`.

Se admite únicamente una diferencia de redondeo de un centavo y se ajusta sobre la mayor base contable. Impuestos distintos de IVA, propina o cargos sin tratamiento definido bloquean el guardado.

## 17. Anulación y reversos

### Anulación de Compra

Exige motivo y permiso `COMPRAS_ANULAR`. No elimina registros.

- La Compra pasa a `ANULADA`.
- El documento XML asociado pasa a estado de procesamiento `NO_APLICA`.
- La CxP recibe `ANULACION_DEUDA`, saldo cero y estado `ANULADA`.
- Se genera asiento inverso.
- El asiento original queda `ANULADO` y enlazado con su reverso.
- Se registra usuario, fecha, motivo y auditoría.

Si la CxP tiene pagos u otros movimientos posteriores, la anulación se bloquea. Si existen recepciones confirmadas, primero deben revertirse.

El asiento inverso usa la fecha civil actual de Ecuador y un período abierto actual; no intenta escribir retroactivamente en un período original cerrado.

### Reverso de Recepción

Se selecciona una Recepción confirmada y se exige motivo. En una transacción:

- crea movimiento inverso de inventario;
- revierte existencias y lotes;
- actualiza estados de series;
- restaura costos anteriores conservados como snapshots;
- marca movimiento y Recepción originales como anulados;
- recalcula el estado pendiente/parcial/recibido de la Compra;
- registra auditoría.

## 18. Proveedor y snapshots históricos

`Tercero` es global y `EmpresaTercero` relaciona al proveedor con la empresa activa. La Compra persiste además:

- identificación del proveedor;
- razón social del proveedor.

Estos snapshots evitan que cambios posteriores del maestro alteren la lectura histórica del comprobante.

La importación XML verifica que el proveedor elegido corresponda al RUC emisor. No permite asociar otro tercero.

## 19. Almacenamiento del XML

PostgreSQL conserva:

- ruta relativa;
- SHA-256;
- tamaño;
- fecha de obtención;
- metadatos tributarios.

No guarda una ruta absoluta ni el XML completo.

`ArchivoCompraFileStorage`:

- valida empresa y clave de 49 dígitos;
- recalcula y compara SHA-256;
- restringe tamaño a 5 MB;
- bloquea path traversal;
- escribe primero un archivo temporal;
- usa `File.Move` para publicar el archivo final;
- elimina temporales ante error o cancelación.

La Compra importada coordina archivo y transacción PostgreSQL. Ante fallo o cancelación antes del commit, revierte la base y elimina el XML. Existe un último punto explícito de cancelación y el commit final se completa sin token cancelable para evitar un resultado ambiguo de base confirmada/archivo eliminado.

Orden de configuración del directorio:

1. variable `KONTAXPRO_DOCUMENTS_PATH`;
2. `DocumentStorage:ComprasRoot`;
3. directorio local predeterminado configurado por la aplicación.

En topología Cliente/Servidor, el directorio debe ser compartido, protegido y accesible desde las estaciones autorizadas o sustituirse en el futuro por un servicio documental de servidor.

## 20. Sesión temporal de importación

El XML analizado se conserva durante 30 minutos y queda ligado a:

- UUID de importación;
- usuario;
- empresa;
- factura interpretada;
- bytes originales.

Solo existe una importación activa por usuario y empresa; cargar otra sustituye la anterior. El acceso está sincronizado y los bytes se copian defensivamente. Las sesiones de otros usuarios/empresas son independientes y los elementos expirados se purgan al acceder.

Al guardar, se exige que el UUID pertenezca al usuario y empresa actuales. La sesión se elimina después de un commit exitoso.

## 21. Seguridad y permisos

Permisos:

- `COMPRAS_VER`;
- `COMPRAS_CREAR`;
- `COMPRAS_EDITAR`;
- `COMPRAS_IMPORTAR_XML`;
- `COMPRAS_RECIBIR`;
- `COMPRAS_ANULAR`;
- `COMPRAS_VER_COSTOS`.

`ADMINISTRADOR` recibe todos. `GUARDALMACEN` recibe `COMPRAS_VER`, `COMPRAS_RECIBIR` e `INVENTARIO_VER_COSTO`.

Infrastructure valida permisos persistidos a través de usuario–empresa–rol. La importación exige `COMPRAS_IMPORTAR_XML` antes de leer el archivo, consultar al SRI o preparar un proveedor. Guardar una importación exige además `COMPRAS_CREAR`; recibir inmediatamente exige `COMPRAS_RECIBIR`.

Las relaciones de empresa, establecimiento, proveedor, productos, presentaciones, cuentas, bodegas, recepciones y asientos se validan otra vez en Infrastructure. No se confía en los IDs enviados por WPF.

## 22. Multiempresa e integridad relacional

El módulo usa FKs y claves alternas compuestas para reforzar aislamiento:

- Compra ↔ establecimiento/empresa;
- Compra ↔ EmpresaTercero/empresa;
- Compra ↔ documento recibido/empresa;
- CompraDetalle ↔ Compra/empresa;
- producto/presentación ↔ línea/empresa;
- Recepción ↔ Compra/bodega/establecimiento/empresa;
- detalle de Recepción ↔ detalle de Compra/compra/empresa;
- lote ↔ producto;
- serie ↔ producto/bodega;
- asiento detalle ↔ asiento/cuenta/empresa.

`DeleteBehavior.Restrict` protege maestros e historia. No se eliminan físicamente operaciones confirmadas.

## 23. Concurrencia e idempotencia

- Compra y Recepción usan `xmin` como `rowversion` de PostgreSQL.
- Las operaciones críticas usan transacción `Serializable`.
- `OperacionUuid` evita confirmar dos veces la misma recepción.
- Clave de acceso, hash, documento recibido, comprobante activo, equivalencia, recepción–movimiento y líneas relacionadas tienen índices únicos.
- Los conflictos se traducen a mensajes funcionales.
- Un cambio de empresa cancela operaciones, cierra overlays y descarta respuestas tardías.
- Las comprobaciones se repiten inmediatamente antes del commit.

## 24. Auditoría

Acciones registradas:

- `COMPRA_CREADA`;
- `COMPRA_ANULADA`;
- `COMPRA_SUSTITUIDA`;
- `PROVEEDOR_PRODUCTO_EQUIVALENCIA_CREADA`;
- `PROVEEDOR_PRODUCTO_EQUIVALENCIA_ACTUALIZADA`;
- `COMPRA_RECEPCION_CONFIRMADA`;
- `COMPRA_RECEPCION_ANULADA`.

La auditoría se agrega dentro de la misma transacción funcional. Conserva usuario, empresa, establecimiento, entidad, ID, descripción y fecha.

## 25. Diseño visual y estilos

Compras toma como base el lenguaje visual de Productos y Clientes sin modificar esos módulos.

Características:

- tarjetas redondeadas y bordes suaves;
- jerarquía clara de título, descripción, sección y ayuda;
- números de paso en recuadros sólidos;
- barra de progreso de cuatro etapas;
- iconografía Material Design;
- acciones primarias verde `primary dark`;
- azul para relación/información;
- ámbar para advertencia/no relacionado;
- rojo para anulación, exceso o error;
- badges compactos y tooltips;
- controles de igual altura dentro de cada sección;
- tablas compactas con scroll afinado para rueda del mouse;
- botones Atrás/Continuar compactos y alineados a la derecha;
- overlays integrados para Producto, Proveedor, Recepción, Configurar control y anulaciones.

Recursos locales relevantes:

- `PurchaseCard`, `PurchaseFormSectionStyle`, `PurchaseTableShellStyle`;
- `PurchaseFieldBorderStyle`, `PurchaseFieldTextBoxStyle`;
- `PurchaseProgressNodeStyle`, `PurchaseProgressLineStyle`;
- estilos KPI All/Pending/Partial/Received;
- estilos de filas/celdas XML y manuales;
- estilos de búsqueda, limpiar relación, editar, recibir, revertir y anular;
- `KontaxPrimaryButtonStyle` y `KontaxSecondaryButtonStyle`.

Los fondos, bordes y textos consumen `DynamicResource` (`PageBackgroundBrush`, `CardBackgroundBrush`, `PrimaryTextBrush`, `SecondaryTextBrush`, `InputBackgroundBrush`, `BorderBrush`, etc.), por lo que las vistas principales son compatibles con Light/Dark. Los acentos sólidos conservan contraste blanco.

## 26. Adaptabilidad y scroll

La vista utiliza espacios de trabajo superpuestos dentro de la ventana principal, evitando ventanas desconectadas. Las secciones extensas tienen scroll propio cuando corresponde; la lista manual crece dentro del scroll general para no atrapar la rueda del mouse.

Las filas de relación y recepción se compactaron para mostrar más productos sin ocultar controles. El scroll de tablas de productos usa desplazamiento lógico/ajustado para evitar saltos que omitan líneas intermedias.

Resoluciones objetivo del diseño: 1920×1080, 1600×900, 1366×768 y 1280×1024. La comprobación visual manual completa en todas ellas debe repetirse antes de cierre de release.

## 27. Validaciones esenciales

- total de Compra mayor que cero;
- al menos una línea válida;
- cantidades y factores mayores que cero;
- descuentos no negativos ni superiores a la línea;
- impuesto de la línea procedente de tarifa válida;
- fecha de vencimiento obligatoria en crédito;
- vencimiento no anterior a emisión;
- proveedor activo y correspondiente al documento;
- establecimiento activo de la empresa;
- producto/presentación activos y de la empresa;
- cuenta contable válida por línea;
- bodega compatible con tipo de compra;
- recepción no superior al pendiente;
- lotes distribuidos exactamente;
- series completas y únicas;
- período contable abierto;
- asiento balanceado;
- motivo obligatorio para anular o revertir.

## 28. Migraciones que construyen Compras V1

- `20260807053951_ComprasV1`;
- `20260809051523_PurchaseAccountingIntegration`;
- `20260810051715_AllowReplacementOfVoidedPurchases`;
- `20260810135942_ValidateReceivedInvoiceAuthorization`;
- `20260810144654_ReverseConfirmedPurchaseReceipts`;
- `20260810145515_PreservePurchaseSupplierSnapshot`;
- `20260810150651_ReplaceManualPurchasesSafely`;
- `20260810151146_EnforcePurchaseReplacementCompany`;
- `20260810152834_SeparateDeclaredXmlPayments`;
- `20260810154127_EnforcePurchaseDetailCompanyIsolation`;
- `20260810154845_EnforcePurchaseReceiptLineage`;
- `20260810155717_EnforcePurchaseReceiptTraceability`;
- `20260810163301_EnforceAccountingDetailCompanyIsolation`.

Todas forman parte de la base Development actual. Cuando se reconstruya la base final deberán consolidarse en una única `InitialCreate`, conforme a `AGENTS.md`.

## 29. Hallazgos de auditoría corregidos

1. Autorización real del comprobante recibido y consistencia con el XML.
2. Reverso formal de recepciones confirmadas.
3. Snapshots de identificación y razón social del proveedor.
4. Corrección manual sin reescritura destructiva.
5. Pagos declarados del XML separados de la condición comercial y CxP.
6. Aislamiento multiempresa en detalles de Compra.
7. Linaje obligatorio detalle de Recepción ↔ detalle de Compra.
8. Trazabilidad compuesta de producto, bodega, lote y serie.
9. Aislamiento multiempresa en detalles contables.
10. Reversos contables en período actual abierto y fecha Ecuador.
11. Diferencias de redondeo de un centavo y bloqueo de cargos no soportados.
12. Determinismo en búsquedas manuales automatizadas.
13. Consistencia entre archivo XML y transacción PostgreSQL ante fallos/cancelación.
14. Acumulación de XML temporales en memoria.
15. Validación temprana de `COMPRAS_IMPORTAR_XML`.
16. Compatibilidad obligatoria entre tipo de Compra y bodega de Recepción.

## 30. Pruebas automatizadas

Existen pruebas para:

- parser XML seguro, formatos, firma, impuestos, pagos y cancelación;
- reglas de receptor y cuadre;
- consulta y mapeo de autorización SRI;
- códigos de barras GS1;
- similitud y descomposición de descripciones;
- estados y comportamiento de líneas XML/manual/recepción;
- almacenamiento, SHA-256, traversal y cancelación sin archivos parciales;
- sesiones temporales, reemplazo, expiración y copia defensiva;
- autorización temprana de importación;
- modelo contable y trazabilidad visual básica;
- reglas de lote, series, factores y cantidades.

Última ejecución al cerrar la auditoría descrita:

- build Release: **0 errores, 0 advertencias**;
- suite: **354 superadas, 5 omitidas, 0 fallidas, 359 totales**;
- las 5 omitidas son pruebas PostgreSQL optativas de Terceros que requieren configuración aislada;
- `git diff --check`: correcto.

## 31. Limitaciones y trabajo futuro

- Importación XML limitada a Factura electrónica.
- Sin descarga masiva ni automática desde el SRI.
- Sin OCR de PDF.
- Sin XAdES criptográfico completo local; se contrasta el comprobante autorizado consultado.
- Si el SRI no está disponible, se conserva advertencia de validación local.
- Sin notas de crédito/débito recibidas ni retenciones dentro de este flujo.
- Sin devoluciones y ajustes de compra operativos en UI.
- Sin pagos, abonos y aplicaciones operativas completas; la CxP queda preparada.
- Sin cuotas complejas u órdenes de compra.
- `ACTIVO` es clasificación contable; no existe todavía gestión de activos fijos.
- El filesystem compartido debe configurarse expresamente en topología Cliente/Servidor.
- Falta una prueba visual manual final en ambos temas y todas las resoluciones objetivo.
- Las pruebas relacionales PostgreSQL específicas de Compras deberán ampliarse en una base aislada cuando se habilite ese entorno.

## 32. Checklist manual recomendado

1. Abrir catálogo y probar cada KPI/filtro.
2. Importar XML autorizado con proveedor existente.
3. Importar XML con proveedor nuevo y verificar creación automática.
4. Probar servicio SRI no disponible y revisar advertencia.
5. Probar clave duplicada y XML duplicado.
6. Relacionar producto automático, sugerido y manual.
7. Limpiar una relación con bonificación marcada.
8. Crear producto con empaque mayorista y revisar factor/costo.
9. Clasificar una línea como Gasto.
10. Guardar XML sin recepción inmediata.
11. Guardar XML con recepción inmediata normal, por lote y por serie.
12. Crear una compra manual respaldada por factura y comprobar que no existe la opción `SIN_FACTURA`.
13. Abrir `Sin sustento` y registrar por separado un gasto y una adquisición de inventario.
14. Corregir una compra manual elegible.
15. Recibir parcialmente y completar con una segunda Recepción.
16. Revertir una Recepción y comprobar stock/Kardex/costos.
17. Anular una Compra sin recepciones activas.
18. Cambiar de empresa con un formulario abierto.
19. Repetir en Light/Dark y resoluciones objetivo.

## 33. Archivos de referencia

- `KONTAXPRO.Desktop/Views/Compras/ComprasView.xaml`
- `KONTAXPRO.Desktop/ViewModels/Compras/ComprasViewModel.cs`
- `KONTAXPRO.Application/Interfaces/ICompraService.cs`
- `KONTAXPRO.Application/Interfaces/ICompraImportacionService.cs`
- `KONTAXPRO.Application/Interfaces/ICompraProductoResolverService.cs`
- `KONTAXPRO.Application/Interfaces/ICompraRecepcionService.cs`
- `KONTAXPRO.Application/Models/Compras/`
- `KONTAXPRO.Application/Security/ComprasSecurity.cs`
- `KONTAXPRO.Domain/Entities/Compras/DocumentosCompra.cs`
- `KONTAXPRO.Domain/Entities/Compras/RecepcionesCompra.cs`
- `KONTAXPRO.Infrastructure/Compras/`
- `KONTAXPRO.Infrastructure/Persistence/Configurations/ComprasConfiguration.cs`
- `KONTAXPRO.Infrastructure/Persistence/Configurations/RecepcionesCompraConfiguration.cs`
- `KONTAXPRO.Tests/Compras/`

## 34. Regla de mantenimiento

Actualizar este documento cuando cambie alguno de estos contratos:

- estados o transiciones de Compra/Recepción;
- formato o validación de XML;
- política de proveedor automático;
- reconocimiento o equivalencias;
- composición de costos;
- reglas de lotes/series;
- CxP o asiento automático;
- permisos;
- tablas, restricciones o migraciones;
- estructura visual del catálogo, asistente o formularios.

No declarar una capacidad como validada si solo fue inspeccionada estáticamente. Registrar por separado la ejecución de build, pruebas automáticas y prueba visual manual.
