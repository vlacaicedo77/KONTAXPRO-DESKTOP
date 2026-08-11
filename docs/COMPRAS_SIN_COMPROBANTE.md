# OPERACIONES SIN COMPROBANTE — memoria funcional, técnica y visual

> Estado documentado: implementación vigente al 11 de agosto de 2026.  
> Alcance: catálogo, registro, corrección, anulación, evidencia, inventario, tesorería, contabilidad, seguridad y experiencia de usuario.  
> Fuente de verdad: código actual de Domain, Application, Infrastructure y Desktop. Este documento no sustituye las entidades, configuraciones EF Core ni reglas de `AGENTS.md`.

## 1. Propósito

El módulo **Operaciones sin comprobante** permite registrar salidas reales de dinero del negocio cuando no existe un comprobante tributario válido que sustente una compra.

Ejemplos:

- almuerzos, materiales de limpieza u otros gastos pagados sin factura;
- mercadería adquirida sin factura que debe ingresar a un inventario separado;
- salidas pagadas directamente desde una caja abierta o una cuenta bancaria.

El principio funcional es:

> Registrar la realidad económica y física sin convertirla artificialmente en una compra tributaria.

Por eso, una operación de este módulo:

- no crea una fila en `s_compras.compras`;
- no crea `documentos_recibidos_sri`;
- no genera cuentas por pagar;
- no reconoce crédito tributario de IVA;
- no se considera gasto deducible;
- no se mezcla con compras facturadas ni con sus reportes tributarios.

Aunque la opción se presenta dentro del grupo visual de **Compras** para facilitar su descubrimiento, su administración, persistencia y efectos pertenecen principalmente a Tesorería, Contabilidad e Inventario.

## 2. Modalidades admitidas

El formulario responde primero a **¿Qué ocurrió?** y ofrece dos modalidades excluyentes.

### 2.1. Inventario

Es la opción predeterminada al abrir un registro nuevo. Representa una adquisición sin comprobante que incrementa existencias en una **bodega no facturable**.

Produce, dentro de una sola transacción:

- la operación sin sustento;
- la salida de caja o banco;
- el ingreso físico de inventario;
- actualización de existencias y costo promedio;
- lotes y/o series cuando el producto lo exige;
- el asiento contable automático;
- la auditoría correspondiente.

No produce IVA crédito ni una compra tributaria.

### 2.2. Gasto

Representa un egreso sin comprobante que debe quedar trazado contablemente como **gasto no deducible**.

Produce, dentro de una sola transacción:

- la operación sin sustento;
- la salida de caja o banco;
- el asiento automático contra una cuenta permitida bajo `GASTOS_NO_DEDUCIBLES`;
- la auditoría correspondiente.

No mueve inventario y tampoco crea una compra o una cuenta por pagar.

## 3. Origen del dinero

La segunda decisión responde a **¿De dónde salió el dinero?**. Solo se admite una fuente por operación.

### Caja

- exige una `caja_sesion` abierta;
- la sesión, caja y establecimiento deben corresponder a la empresa activa;
- genera un movimiento de caja `EGRESO`;
- utiliza la cuenta contable configurada en la caja como contrapartida acreedora.

### Banco

- exige una cuenta bancaria activa de la empresa;
- genera un movimiento bancario `RETIRO`;
- utiliza la cuenta contable configurada en la cuenta bancaria como contrapartida acreedora.

La base de datos impide combinaciones ambiguas: una operación de caja no puede conservar una cuenta bancaria y una de banco no puede conservar una sesión de caja.

## 4. Acceso, navegación y permisos

La opción del menú se denomina **Operaciones sin comprobante**, usa el icono `ReceiptTextRemoveOutline` y navega mediante `OperacionesSinComprobante`. Actualmente aparece dentro del grupo de Compras, pero abre un catálogo CRUD propio dentro del contenedor principal de la aplicación.

Permisos estructurales:

| Permiso | Uso |
|---|---|
| `TESORERIA_VER_SIN_SUSTENTO` | Consultar catálogo, detalle y evidencia |
| `TESORERIA_REGISTRAR_SIN_SUSTENTO` | Acceder desde el menú y crear operaciones |
| `TESORERIA_CORREGIR_SIN_SUSTENTO` | Corregir mediante sustitución |
| `TESORERIA_ANULAR_SIN_SUSTENTO` | Anular mediante reversos |
| `PRODUCTOS_CONFIGURAR_PRECIOS` | Aplicar precios definidos al crear un producto contextual |

Infrastructure vuelve a validar los permisos, la empresa y el establecimiento; la visibilidad o habilitación en la interfaz no constituye autorización suficiente.

## 5. Catálogo principal

### 5.1. Encabezado

El catálogo sigue el lenguaje visual de Productos y Compras:

- fondo basado en `PageBackgroundBrush`;
- icono blanco dentro de una tarjeta sólida `PrimaryDarkBrush`;
- título **Operaciones sin comprobante**;
- descripción: “Controla salidas reales sin mezclarlas con compras ni sustento tributario”;
- botón primario **Nueva operación**.

### 5.2. KPIs y filtros rápidos

Se muestran cinco tarjetas:

- **Operaciones**: total general; azul;
- **Gastos**: gastos confirmados; warning/ámbar;
- **Inventario**: ingresos confirmados; morado;
- **Anuladas**: operaciones anuladas; rojo;
- **Salidas confirmadas**: suma monetaria de operaciones confirmadas; verde oscuro.

Las primeras cuatro actúan como filtros. La tarjeta activa cambia fondo y borde sin perder compatibilidad con tema claro u oscuro.

### 5.3. Búsqueda y filtros

La búsqueda consulta:

- número de operación;
- beneficiario;
- motivo;
- referencia.

Incluye una X para limpiar, espera aproximadamente 350 ms antes de consultar y cancela resultados obsoletos. También existen filtros por tipo (`TODOS`, `GASTO`, `INVENTARIO`) y estado (`TODAS`, `CONFIRMADO`, `ANULADO`), más un botón de actualización.

### 5.4. Tabla

Columnas actuales:

- **Fecha**;
- **Operación**: número y tipo;
- **Beneficiario / motivo**;
- **Fondo**: Caja o Banco;
- **Total**;
- **Estado**: badge verde para confirmado y rojo para anulado;
- **Acciones**.

Acciones por fila:

- ver detalle;
- corregir mediante sustitución;
- anular.

Corregir y anular no se muestran para registros anulados y además respetan los permisos de sesión. El estado vacío mantiene el patrón visual del sistema y ofrece **Limpiar filtros**.

### 5.5. Paginación

La paginación replica el patrón de Productos:

- tamaños de 25, 50 y 100 registros;
- 25 por defecto;
- texto “Mostrando X–Y de N operaciones”;
- anterior y siguiente;
- páginas numeradas con ventana cercana a la página actual y separadores `…`;
- la consulta se ejecuta en servidor con `Skip`/`Take`;
- el tamaño efectivo admitido por Application/Infrastructure se limita entre 10 y 200.

Los KPIs se calculan sobre todas las operaciones autorizadas de la empresa/establecimientos, no solo sobre la página visible.

## 6. Formulario Nueva operación

El formulario se abre como espacio de trabajo superpuesto dentro del catálogo, no como una navegación hacia Compras. Tiene contenido desplazable y un pie fijo.

### 6.1. Lenguaje visual

- superficies, bordes y textos usan recursos dinámicos para tema light/dark;
- título con icono sólido y jerarquía equivalente a los formularios de Productos y Compras;
- badge permanente **NO DEDUCIBLE · SIN IVA CRÉDITO**;
- tarjetas compactas, bordes redondeados y elevación moderada;
- opciones seleccionadas de Inventario/Gasto y Caja/Banco permanecen visualmente marcadas;
- controles de altura uniforme;
- iconografía Material Design;
- botón destructivo rojo para cancelar y primario para confirmar;
- el total permanece visible en el pie fijo mediante tarjeta sólida `PrimaryDarkBrush`;
- el desplazamiento maestro continúa funcionando al pasar por combos o listas internas.

### 6.2. Datos principales

Campos y decisiones:

- tipo: Inventario o Gasto;
- fuente: Caja o Banco;
- fecha con máscara `dd/mm/aaaa`;
- beneficiario o responsable;
- motivo;
- sesión de caja o cuenta bancaria, según la fuente.

Si no existen fuentes disponibles, la interfaz lo informa. El catálogo de caja usa únicamente sesiones abiertas y el catálogo bancario usa cuentas activas.

### 6.3. Datos opcionales

- referencia interna, sin efecto contable ni tributario;
- archivo de soporte opcional.

El pie informa que la operación será auditable pero no tendrá sustento tributario. Durante el guardado se bloquea una segunda confirmación y se muestra progreso.

## 7. Detalle de gasto

Cada línea contiene:

- cuenta de gasto;
- descripción;
- valor con dos decimales;
- acción para quitar la línea.

Se pueden agregar varias líneas. El total de la operación es la suma redondeada a dos decimales de sus valores.

Las cuentas disponibles no son todas las cuentas del plan. Deben:

- pertenecer a la empresa;
- estar activas;
- aceptar movimientos;
- ser de naturaleza deudora;
- estar bajo la cuenta configurada como `GASTOS_NO_DEDUCIBLES`.

La cuenta de origen del fondo no puede utilizarse simultáneamente como cuenta de destino del gasto.

## 8. Detalle de inventario

### 8.1. Bodega

La operación exige una bodega activa del establecimiento actual con `permite_venta_facturada = false`. Esta separación evita que existencias adquiridas sin documento tributario se vendan accidentalmente mediante el flujo facturado.

### 8.2. Líneas

Cada línea presenta:

- producto/presentación;
- cantidad de la presentación;
- costo total de la línea;
- estado y acceso a configuración de lotes/series;
- información de costos actual y proyectada;
- acción para eliminar.

La cantidad inicia en cero para evitar ingresos accidentales. Cantidad y costo total deben ser mayores que cero. El costo total admite decimales y de él se derivan:

```text
cantidad_base = cantidad_presentación × factor_conversión
costo_unitario_presentación = costo_total / cantidad_presentación
costo_unitario_base = costo_total / cantidad_base
costo_promedio_proyectado =
    (stock_actual × costo_promedio_actual + costo_total) /
    (stock_actual + cantidad_base)
```

El sistema muestra, con dos decimales:

- costo de compra de la presentación;
- costo unitario base;
- costo promedio actual y proyectado;
- costo proyectado de la presentación cuando no es base;
- flecha/color de subida, bajada o estabilidad;
- advertencia **REVISAR PRECIOS** cuando la variación relevante alcanza el umbral configurado en la interfaz (10 %).

La proyección es informativa; el costo definitivo se calcula nuevamente dentro de la transacción de inventario.

### 8.3. Búsqueda de productos

La caja trabaja en mayúsculas y ofrece coincidencias por:

- código del producto;
- nombre comercial;
- presentación;
- marca;
- código de barras.

Características:

- búsqueda por tokens de al menos dos caracteres;
- todos los tokens deben coincidir en algún campo indexado por la consulta;
- máximo 20 sugerencias visibles;
- eliminación de presentaciones duplicadas;
- prioridad por coincidencia inicial, nombre, presentación base y factor;
- presentaciones base primero;
- presentaciones mayores ordenadas por factor de conversión;
- icono sólido y badge verde para base, azul para presentaciones mayores;
- costo promedio de la presentación, calculado desde el costo base y su factor;
- popup del ancho del campo y con scroll propio.

Al elegir una sugerencia, el foco pasa a **Cantidad**. La X limpia el producto y todos sus derivados: cantidad, costo total, proyecciones, precios pendientes y configuración de control.

### 8.4. Producto inexistente

Cuando no hay coincidencias, el usuario puede crear un producto desde el flujo contextual. Antes de abrirlo deben existir:

- descripción;
- cantidad mayor que cero;
- costo total mayor que cero.

Esto permite que el formulario de Producto Nuevo conozca el costo real de la presentación y proponga precios. El producto contextual:

1. se crea como borrador inactivo;
2. no recibe inventario inicial desde Productos;
3. conserva precios pendientes en memoria;
4. se activa dentro de la misma transacción al confirmar la operación;
5. recibe existencias, costo, lotes/series y precios desde la operación;
6. se elimina si el usuario abandona o limpia un borrador que continúa siendo seguro de descartar.

Los precios pendientes solo se admiten para productos contextuales nuevos. Para modificar precios de un producto existente se utiliza Productos > Editar. Aplicarlos requiere `PRODUCTOS_CONFIGURAR_PRECIOS`.

## 9. Control de lotes y series

Los productos con control `LOTE`, `SERIE` o `LOTE_Y_SERIE` muestran **Control pendiente** hasta completar su distribución. Si la cantidad es cero, se advierte al usuario y la ventana no se abre.

### 9.1. Ventana Configurar control

El editor modal replica el patrón de Confirmar recepción de mercadería:

- título e icono coherentes con el botón de control;
- tarjeta **Cantidad distribuida** con Asignado, Requerido y Pendiente;
- pendiente/exceso en rojo si se sobrepasa lo requerido;
- botón **Agregar lote** o **Agregar serie** según el control;
- pie con **Cancelar** y **Aplicar control**;
- panel lateral **Lotes registrados** con total, bodega, fechas y stock;
- lotes existentes de todas las bodegas como referencia global, sin asumir que el stock de otra bodega pertenece a la seleccionada.

Para lotes se capturan:

- número en mayúsculas;
- cantidad base;
- fecha de elaboración opcional;
- fecha de caducidad opcional;
- autorización explícita para crear un lote nuevo cuando exista uno de número similar.

Para series:

- cada serie debe ser única;
- debe existir exactamente una serie por unidad base requerida;
- cuando el producto también maneja lote, cada serie se asocia a uno de los lotes distribuidos.

Seleccionar un lote existente lo reutiliza y dirige la edición a la cantidad. La configuración solo pasa a la línea al pulsar **Aplicar control** y se mantiene mientras el formulario continúe abierto.

## 10. Evidencia documental

La evidencia es opcional y no convierte la operación en sustento tributario.

Formatos:

- PDF;
- JPG/JPEG;
- PNG.

Límite: 10 MB.

Almacenamiento:

```text
<directorio-documental>/sin-sustento/<empresa>/<año>/<mes>/<guid>.<extensión>
```

La base conserva:

- ruta relativa;
- nombre original;
- huella SHA-256;
- tamaño.

No se aceptan rutas absolutas ni rutas que escapen del directorio autorizado. La escritura usa archivo temporal y movimiento final. Si falla el registro transaccional, el servicio intenta eliminar el archivo recién guardado.

Al exportar una evidencia se comprueban tamaño y SHA-256 antes de entregarla al usuario. Una alteración o ausencia física produce una advertencia de integridad.

## 11. Confirmación y efectos transaccionales

El alta se ejecuta con contexto por operación, transacción PostgreSQL y aislamiento `Serializable`.

Secuencia conceptual:

1. validar reglas de Application;
2. validar usuario, empresa, establecimiento y permiso;
3. validar fondo y su cuenta contable;
4. validar período contable abierto;
5. asignar número interno mediante secuencial transaccional `OPERACION_SIN_SUSTENTO` con prefijo `OSS`;
6. validar gasto o bodega/productos/control;
7. activar productos contextuales cuando corresponda;
8. registrar movimiento de inventario en modalidad Inventario;
9. aplicar precios pendientes autorizados;
10. generar asiento automático balanceado;
11. generar movimiento de caja o banco;
12. persistir cabecera, detalles y vínculos técnicos;
13. registrar auditoría;
14. confirmar la transacción.

Si una etapa falla, la operación funcional no debe quedar confirmada parcialmente.

### 11.1. Contabilización de gasto

```text
Debe:  cuenta(s) de gasto no deducible
Haber: cuenta contable de la caja o banco
```

### 11.2. Contabilización de inventario

```text
Debe:  cuenta configurada de INVENTARIO
Haber: cuenta contable de la caja o banco
```

El asiento es automático, utiliza origen `OPERACION_SIN_SUSTENTO`, debe pertenecer a un período abierto y se valida balance antes del commit.

### 11.3. Movimiento físico

El ingreso usa:

- tipo de movimiento `ADQUISICION_SIN_SUSTENTO` / naturaleza ENTRADA;
- origen `OPERACION_SIN_SUSTENTO`;
- una sola bodega;
- cantidades de presentación y base;
- costo unitario base y costo total;
- stock y costo promedio anterior/nuevo;
- lotes y series cuando corresponda.

Si el mismo producto aparece en varias líneas, cada línea posterior ve el stock y costo acumulados de la anterior dentro de la misma transacción. Esto mantiene correcto el costo promedio.

## 12. Consulta de detalle

La acción **Ver** abre un panel modal con:

- número;
- fecha;
- tipo y fondo;
- total destacado;
- beneficiario;
- motivo;
- líneas con producto, cuenta, descripción, cantidad y total;
- acceso a la evidencia si existe;
- indicación explícita de operación no deducible y sin crédito tributario.

En operaciones de inventario, el servicio reconstruye los lotes y series desde el movimiento confirmado para poder reutilizarlos durante una corrección.

## 13. Corrección por sustitución

Una operación confirmada no se edita en sitio. **Corregir** aplica el patrón de sustitución:

1. bloquea la operación original con `FOR UPDATE`;
2. comprueba que siga confirmada y no tenga sustituta;
3. genera todos los reversos de la original;
4. registra una nueva operación con los datos corregidos;
5. enlaza la nueva mediante `operacion_sustituida_id`;
6. confirma todo en una transacción `Serializable`.

El detalle expone tanto `SustituyeAId` como `SustituidaPorId`. Solo puede existir una sustituta por operación original, garantizado por índice único parcial.

Las series de la operación original pueden reutilizarse durante la sustitución porque primero son revertidas de manera controlada dentro de la misma transacción.

## 14. Anulación y reversos

La anulación exige un motivo de al menos cinco caracteres. No elimina físicamente la operación; cambia su estado a `ANULADO` y genera reversos auditables.

### 14.1. Fondo

- Caja: crea `REVERSO_EGRESO`. Si la sesión original está cerrada, exige una sesión abierta actual de la misma caja para no alterar el cierre histórico.
- Banco: crea `REVERSO_SALIDA`.

Cada movimiento original queda enlazado una sola vez con su reverso.

### 14.2. Contabilidad

- crea un nuevo asiento automático invirtiendo Debe/Haber;
- requiere período contable abierto en la fecha del reverso;
- enlaza `asiento_origen_reversado_id`;
- marca el asiento original como anulado.

### 14.3. Inventario

La reversión es deliberadamente estricta. Se rechaza si:

- existen movimientos posteriores de cualquiera de los productos;
- el stock actual ya no coincide con el stock esperado del movimiento original;
- existe stock reservado;
- cambió el costo promedio;
- cambió o se reservó el stock de un lote;
- una serie dejó de estar disponible.

Si es seguro:

- crea un movimiento `AJUSTE_SALIDA`;
- restaura stock y costo promedio anterior;
- restaura existencias de lotes;
- lleva las series a `BAJA`;
- enlaza el movimiento reverso;
- marca el movimiento original como anulado.

Si existen movimientos posteriores, deben revertirse primero. Esta regla evita reconstrucciones históricas inseguras del costo promedio.

No puede anularse directamente una operación que ya fue sustituida por una corrección.

## 15. Reglas principales de validación

- tipo permitido: `GASTO` o `INVENTARIO`;
- fondo permitido: `CAJA` o `BANCO`;
- exactamente una fuente acorde al fondo;
- beneficiario obligatorio, máximo 250 caracteres;
- motivo obligatorio, máximo 1000 caracteres;
- referencia opcional, máximo 255 caracteres;
- al menos una línea;
- descripción obligatoria en cada línea;
- costo/valor total de cada línea mayor que cero;
- total de cabecera mayor que cero;
- `es_deducible` siempre falso;
- Gasto exige cuenta contable y prohíbe producto/presentación/bodega;
- Inventario exige bodega, producto, presentación y cantidad positiva, y prohíbe cuenta de gasto;
- Inventario valida que presentación y producto pertenezcan a la empresa y correspondan entre sí;
- lotes/series deben completar exactamente la cantidad base;
- los precios pendientes solo corresponden a producto contextual nuevo;
- no se permiten combinaciones duplicadas presentación/lista de precio;
- métodos de precio aceptados: `PORCENTAJE_COSTO`, `DESCUENTO_PORCENTAJE` y `PRECIO_FIJO`, según el tipo de lista;
- estados persistidos: `CONFIRMADO` y `ANULADO`.

Estas reglas se aplican en la interfaz para orientar al usuario, en Application para validar el caso de uso y nuevamente en Infrastructure/EF/PostgreSQL para proteger integridad.

## 16. Persistencia

### 16.1. Cabecera

Entidad `OperacionSinSustento`, tabla:

```text
s_tesoreria.operaciones_sin_sustento
```

Datos relevantes:

- empresa, establecimiento y usuario;
- número de operación;
- tipo y medio de salida;
- sesión de caja o cuenta bancaria;
- bodega cuando es inventario;
- fecha, beneficiario, motivo y referencia;
- metadatos de evidencia;
- total y `es_deducible = false`;
- estado y datos de anulación;
- enlaces a movimientos de caja, banco, inventario y asiento;
- enlace a operación sustituida;
- timestamps.

Integridad destacada:

- número único por empresa;
- un único vínculo para cada movimiento/asiento;
- sustitución única parcial;
- FKs restrictivas;
- checks de tipo, fondo, modalidad, evidencia, estado, total y no deducibilidad.

### 16.2. Detalles

Tabla:

```text
s_tesoreria.operaciones_sin_sustento_detalles
```

Una línea representa exactamente uno de estos destinos:

- cuenta contable de gasto; o
- producto y presentación.

Conserva descripción, cantidades, factor, costo unitario base y costo total. Cantidades/costos usan precisión `NUMERIC(18,6)` y dinero total `NUMERIC(18,2)` según el modelo configurado.

## 17. Multiempresa, establecimiento y seguridad

- toda consulta se filtra por empresa;
- el usuario debe tener relación activa con la empresa;
- solo se consultan establecimientos asignados al usuario;
- caja, banco, bodega, producto, presentación, cuenta y lista de precios se vuelven a validar en servidor;
- la bodega debe pertenecer al establecimiento de la operación;
- relaciones históricas usan `DeleteBehavior.Restrict`;
- el servicio usa `IDbContextFactory<KontaxDbContext>` y contexto por operación;
- altas, correcciones y anulaciones financieras usan transacción compartida;
- se usa bloqueo explícito y aislamiento serializable en corrección/anulación;
- no se emplea `MAX(...) + 1` para numeración.

## 18. Auditoría

Eventos relevantes:

- `REGISTRAR_OPERACION_SIN_SUSTENTO`;
- `ANULAR_OPERACION_SIN_SUSTENTO`;
- `ACTIVAR_PRODUCTO_DESDE_OPERACION_SIN_SUSTENTO`;
- `CONFIGURAR_PRECIO_DESDE_OPERACION_SIN_SUSTENTO`.

La auditoría identifica usuario, empresa, establecimiento, entidad y descripción. Los efectos financieros e históricos no se borran: se revierten.

## 19. Catálogos y configuración estructural requeridos

La instalación debe contener:

- permisos de Tesorería descritos arriba;
- tipo de documento interno `OPERACION_SIN_SUSTENTO`, prefijo `OSS`;
- tipo de movimiento de inventario `ADQUISICION_SIN_SUSTENTO`;
- tipo de origen de inventario `OPERACION_SIN_SUSTENTO`;
- tipo de origen contable `OPERACION_SIN_SUSTENTO`;
- tipos de reverso de caja y banco;
- estados de series `DISPONIBLE` y `BAJA`;
- configuración contable `GASTOS_NO_DEDUCIBLES`;
- configuración contable `INVENTARIO`;
- cuenta contable asociada a cada caja y cuenta bancaria utilizada;
- períodos contables abiertos;
- al menos una bodega no facturable para la modalidad Inventario.

`StructuralSeeder` crea los catálogos estructurales. `DemoSeeder` configura en desarrollo una cuenta de gastos no deducibles para facilitar las pruebas.

## 20. Arquitectura y archivos de referencia

### Domain

- `KONTAXPRO.Domain/Entities/Tesoreria/OperacionSinSustento.cs`

### Application

- `KONTAXPRO.Application/Interfaces/IOperacionSinSustentoService.cs`
- `KONTAXPRO.Application/Interfaces/ISoporteSinSustentoStorage.cs`
- `KONTAXPRO.Application/Models/Tesoreria/OperacionesSinSustentoDtos.cs`
- `KONTAXPRO.Application/Tesoreria/OperacionSinSustentoRules.cs`

### Infrastructure

- `KONTAXPRO.Infrastructure/Tesoreria/OperacionSinSustentoService.cs`
- `KONTAXPRO.Infrastructure/Tesoreria/OperacionSinSustentoAdministration.cs`
- `KONTAXPRO.Infrastructure/Tesoreria/OperacionSinSustentoInventoryReversal.cs`
- `KONTAXPRO.Infrastructure/Tesoreria/SoporteSinSustentoFileStorage.cs`
- `KONTAXPRO.Infrastructure/Persistence/Configurations/OperacionesSinSustentoConfiguration.cs`
- `KONTAXPRO.Infrastructure/Persistence/StructuralSeeder.cs`

### Desktop

- `KONTAXPRO.Desktop/ViewModels/Tesoreria/OperacionesSinSustentoViewModel.cs`
- `KONTAXPRO.Desktop/ViewModels/Tesoreria/OperacionSinSustentoViewModel.cs`
- `KONTAXPRO.Desktop/Views/Tesoreria/OperacionesSinSustentoView.xaml`
- `KONTAXPRO.Desktop/Views/Tesoreria/OperacionSinSustentoWindow.xaml`
- `KONTAXPRO.Desktop/ViewModels/MainViewModel.cs`
- `KONTAXPRO.Desktop/Services/NavigationService.cs`
- `KONTAXPRO.Desktop/App.xaml.cs`

El code-behind del formulario se limita al selector nativo del archivo de evidencia y a suscripciones de la vista; las reglas y orquestación permanecen en ViewModel/Application/Infrastructure.

## 21. Cobertura automática existente

Pruebas unitarias:

- acepta gasto pagado desde caja;
- acepta inventario pagado desde banco;
- rechaza mezclar caja y banco;
- rechaza inventario sin bodega;
- acepta líneas repetidas del mismo producto para costeo secuencial;
- rechaza precios pendientes para producto existente;
- acepta y valida precios pendientes de producto contextual;
- rechaza precio fijo inválido;
- rechaza evidencia mayor de 10 MB;
- verifica lectura íntegra de evidencia;
- detecta evidencia manipulada por SHA-256.

Prueba PostgreSQL:

- confirma que no existan migraciones pendientes en la base de prueba;
- comprueba el mapeo de evidencia;
- verifica constraints financieros de no deducibilidad, fondo e inventario.

Archivos:

- `KONTAXPRO.Tests/Tesoreria/OperacionSinSustentoRulesTests.cs`
- `KONTAXPRO.Tests/Tesoreria/SoporteSinSustentoFileStorageTests.cs`
- `KONTAXPRO.Tests/PostgreSql/OperacionesSinSustentoPostgreSqlTests.cs`

## 22. Guía de prueba manual

### Caso A — Gasto desde caja

1. Abrir una sesión de caja con cuenta contable configurada.
2. Ir a Compras > Operaciones sin comprobante.
3. Crear una operación tipo Gasto y fuente Caja.
4. Ingresar beneficiario, motivo y fecha.
5. Elegir una cuenta bajo `GASTOS_NO_DEDUCIBLES`.
6. Agregar una o más líneas y confirmar.
7. Verificar número `OSS`, total y estado confirmado.
8. Verificar movimiento `EGRESO` y asiento Debe gasto/Haber caja.
9. Confirmar que no exista Compra, CxP ni documento SRI.

### Caso B — Inventario existente desde banco

1. Disponer de cuenta bancaria contable y bodega no facturable.
2. Elegir Inventario y Banco.
3. Buscar un producto/presentación existente.
4. Ingresar cantidad y costo total.
5. Revisar costos actual/proyectado.
6. Configurar lote/serie si corresponde.
7. Confirmar.
8. Verificar retiro bancario, asiento, Kardex, stock y costo promedio.
9. Confirmar que el ingreso se realizó solo en la bodega no facturable.

### Caso C — Producto nuevo contextual

1. Escribir un producto inexistente.
2. Ingresar cantidad y costo total.
3. Usar la acción de creación contextual.
4. Completar producto, presentación y precios.
5. Regresar y configurar control si corresponde.
6. Confirmar la operación.
7. Verificar que el producto quedó activo, con stock/costo y precios, sin inventario inicial duplicado.

### Caso D — Corrección

1. Seleccionar una operación confirmada y pulsar Corregir.
2. Modificar datos y confirmar.
3. Verificar que la original quedó anulada/sustituida.
4. Verificar reversos y una nueva operación confirmada enlazada.

### Caso E — Anulación segura

1. Seleccionar una operación confirmada sin movimientos posteriores.
2. Indicar un motivo de cinco o más caracteres.
3. Confirmar la anulación.
4. Verificar reversos de fondo y contabilidad; para Inventario, también el reverso físico.
5. Repetir con movimientos posteriores y comprobar que el sistema impide una reversión insegura.

### Caso F — Evidencia

1. Adjuntar PDF o imagen menor de 10 MB.
2. Confirmar y abrir el detalle.
3. Exportar la evidencia.
4. Confirmar coincidencia de contenido y SHA-256.

## 23. Límites deliberados de V1

- no reemplaza la compra facturada ni una liquidación de compra;
- no otorga sustento tributario aunque se adjunte evidencia;
- no genera cuenta por pagar: el dinero debe salir de caja o banco al confirmar;
- no permite dividir una misma operación entre varias cajas/bancos;
- no admite una mezcla de líneas de gasto e inventario en la misma operación;
- el inventario se registra inmediatamente, no mediante recepción posterior;
- el inventario queda restringido a bodegas no facturables;
- un producto existente no cambia sus precios desde este formulario;
- una anulación de inventario no reconstruye costos si ya existen movimientos posteriores;
- una operación confirmada no se edita directamente: se corrige por sustitución.

## 24. Regla de mantenimiento

Al extender este módulo debe preservarse la separación entre:

```text
compra con comprobante válido
    → tributación, CxP y compra formal

operación sin comprobante
    → salida inmediata de fondo
    → gasto no deducible o inventario no facturable
    → sin IVA crédito y sin CxP
```

Cualquier cambio que permita vender existencias no facturables, reconocer IVA, generar CxP o declarar deducibilidad desde este flujo exige una decisión funcional y tributaria explícita; no debe surgir como efecto colateral de reutilizar lógica de Compras.
