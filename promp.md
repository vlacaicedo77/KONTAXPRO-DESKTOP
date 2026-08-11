# KONTAXPRO Desktop — Implementar INVENTARIO / KARDEX V1

Quiero que continuemos con el desarrollo de **KONTAXPRO Desktop**.

A esta altura ya están desarrollados y funcionales, entre otros, los módulos de:

* Productos.
* Clientes.
* Proveedores.
* Compras.
* Operaciones sin comprobante.
* Las estructuras y relaciones que hemos ido preparando para Contabilidad.
* Configuración, seguridad, empresas, establecimientos, bodegas y demás componentes que ya existen en el proyecto.

Ahora quiero implementar **INVENTARIO / KARDEX V1**.

Este módulo es especialmente importante porque debe convertirse en el **motor central de movimientos y existencias de KONTAXPRO**, que posteriormente será utilizado también por Ventas, devoluciones, transferencias, ajustes, notas de crédito y otros procesos.

La filosofía sigue siendo:

> **Robustez por dentro, simplicidad por fuera.**

No quiero crear simplemente una pantalla para consultar stock. Quiero construir correctamente el subsistema de inventario sobre el cual se apoyarán Compras y, posteriormente, Ventas.

---

# 1. PRIMERO: ANALIZA EL ESTADO ACTUAL DEL PROYECTO

Antes de modificar código:

1. Lee el `AGENTS.md` de la raíz.
2. Lee cualquier `AGENTS.md` adicional que corresponda a las carpetas/proyectos que vas a modificar.
3. Revisa `/docs` y especialmente la documentación actual de:

   * Productos.
   * Compras.
   * Clientes.
   * Proveedores.
   * Operaciones sin comprobante.
   * Contabilidad.
   * Base de datos.
   * Arquitectura.
4. Revisa las migraciones actuales.
5. Revisa las entidades actuales.
6. Revisa DTOs, servicios, interfaces, repositorios, ViewModels, Views y pruebas existentes.
7. Revisa cómo se están manejando actualmente:

   * Empresa.
   * Establecimiento.
   * Punto de emisión.
   * Bodega.
   * Producto.
   * Presentaciones.
   * Existencias.
   * Costos.
   * Lotes.
   * Series.
   * Compras.
   * Estados de documentos.
   * Operaciones sin comprobante.
   * Auditoría.
   * Contabilidad.
8. Revisa específicamente si ya existen:

   * tablas de movimientos de inventario;
   * cabecera/detalle de movimientos;
   * existencias;
   * existencias por lote;
   * series;
   * costos;
   * tipos de movimiento;
   * servicios relacionados con stock;
   * lógica de actualización de inventario desde Compras.

**El código actual del repositorio es la fuente principal de verdad.**

No quiero que regreses a una estructura antigua que haya quedado obsoleta en documentos previos.

Si algo ya existe y está correctamente diseñado, **reutilízalo y complétalo**.

No crees estructuras paralelas con el mismo propósito.

---

# 2. VERIFICA EL ESTADO ACTUAL ANTES DE EMPEZAR

Ejecuta:

```powershell
dotnet build
dotnet test
```

Confirma el estado inicial.

Si existe algún error previo no relacionado con Inventario/Kardex, identifícalo antes de realizar cambios para no confundirlo posteriormente con una regresión.

---

# 3. OBJETIVO PRINCIPAL

Quiero que Inventario/Kardex sea el único mecanismo autorizado para producir cambios de existencias.

Conceptualmente:

```text
                   PRODUCTOS
                       │
                       ▼
                INVENTARIO / KARDEX
             ┌─────────┼─────────┐
             │         │         │
          COMPRAS    VENTAS    AJUSTES
             │         │         │
          ENTRADA     SALIDA    + / -
             │         │         │
             └─────────┼─────────┘
                       ▼
                 EXISTENCIAS
                 COSTOS
                 LOTES
                 SERIES
                 HISTORIAL
```

Compras, Ventas u otros módulos **no deberían modificar el stock directamente**.

Deben solicitar al motor de Inventario que registre la operación correspondiente.

---

# 4. PRINCIPIO FUNDAMENTAL: MOVIMIENTOS INMUTABLES

El Kardex debe representar hechos ocurridos.

Una vez aplicado un movimiento al inventario:

* no debe editarse silenciosamente;
* no debe eliminarse físicamente para “corregir” stock;
* no debe modificarse la cantidad histórica;
* no debe modificarse el costo histórico.

Si una operación necesita ser revertida:

```text
MOVIMIENTO ORIGINAL
        +
MOVIMIENTO INVERSO / REVERSO
```

deben dejar el historial consistente.

El movimiento original debe permanecer como evidencia.

Si ya existe una política equivalente en el proyecto, utiliza esa implementación.

---

# 5. TODO MOVIMIENTO DE INVENTARIO DEBE TENER ORIGEN

Cada movimiento debe poder indicar de dónde provino.

Por ejemplo:

```text
COMPRA
VENTA
AJUSTE
TRANSFERENCIA
DEVOLUCION_COMPRA
DEVOLUCION_VENTA
SALDO_INICIAL
OPERACION_SIN_COMPROBANTE
CONVERSION
OTRO
```

No necesariamente debes crear exactamente estos valores si ya existe un catálogo o enumeración equivalente.

Adáptate a la arquitectura existente.

Debe existir conceptualmente:

```text
OrigenTipo
OrigenId
```

de modo que sea posible ir desde un movimiento de Kardex hasta el documento u operación que lo generó.

---

# 6. MULTIEMPRESA

KONTAXPRO es multiempresa.

Todo cálculo y consulta de inventario debe respetar el ámbito de la empresa actual.

Nunca debe ser posible:

* consultar stock de otra empresa accidentalmente;
* afectar bodegas de otra empresa;
* mezclar movimientos;
* mezclar costos;
* mezclar lotes;
* mezclar series.

Utiliza los mecanismos actuales de `CurrentSession`, EmpresaId y autorización existentes.

No inventes un segundo mecanismo de sesión.

---

# 7. MULTIBODEGA

El inventario debe funcionar por bodega.

La existencia real debe poder conocerse como mínimo por:

```text
Producto + Bodega
```

y cuando corresponda:

```text
Producto + Lote + Bodega
```

y:

```text
Producto + Serie + Bodega
```

Una empresa puede tener:

```text
Empresa
 ├── Establecimiento matriz
 │    ├── Bodega principal
 │    └── Otra bodega
 │
 └── Sucursal
      └── Bodega sucursal
```

No asumir una sola bodega por empresa.

---

# 8. UNIDAD BASE Y PRESENTACIONES

Este punto es crítico.

KONTAXPRO ya maneja productos con diferentes presentaciones y `factor_conversion`.

El inventario debe mantenerse internamente en **cantidad base**.

Ejemplo:

```text
Producto:
IVERMEC 100 ML

Presentación base:
FRASCO
Factor = 1

Presentación:
CAJA X 10
Factor = 10
```

Si ingreso:

```text
2 CAJAS X 10
```

el motor debe registrar:

```text
Cantidad comercial: 2
Presentación: CAJA X 10
Factor: 10
Cantidad base: 20
```

La existencia debe aumentar en:

```text
20 unidades base
```

La información comercial puede conservarse para trazabilidad, pero el stock debe ser consistente en unidad base.

Lo mismo aplicará posteriormente para Ventas.

---

# 9. NO REDONDEAR INDEBIDAMENTE CANTIDADES

Existen productos que pueden manejar cantidades decimales.

No uses `int` para cantidades de inventario.

Respeta los tipos `decimal`/`numeric` definidos en la base.

No introduzcas `double` o `float` para cantidades ni valores monetarios.

Revisa las precisiones actuales de PostgreSQL antes de decidir cualquier cambio.

---

# 10. EXISTENCIA ACTUAL

La existencia debería representar conceptualmente:

```text
StockActual
StockReservado
StockDisponible
StockMinimo
```

donde:

```text
StockDisponible = StockActual - StockReservado
```

No necesariamente almacenes `StockDisponible` si puede calcularse.

Revisa primero el modelo existente.

No dupliques información derivada innecesariamente.

---

# 11. PRODUCTOS SIN INVENTARIO

Respeta:

```text
maneja_inventario
```

o la propiedad equivalente existente.

Un servicio:

```text
MANEJA_INVENTARIO = false
```

no debe generar movimientos de Kardex ni afectar existencias.

Una operación puede contener simultáneamente:

```text
BIEN
SERVICIO
```

y solamente los ítems que manejan inventario deben llegar al motor.

---

# 12. TIPO DE CONTROL DE INVENTARIO

Respeta todo lo que ya desarrollamos en Productos respecto a:

* control normal;
* control por lotes;
* control por series;
* control de caducidad;
* fecha de elaboración;
* fecha de caducidad;
* días de anticipación;
* conversión de tipo de control.

No dupliques reglas.

Busca primero dónde están implementadas actualmente.

El motor de inventario debe validar estas mismas reglas.

---

# 13. LOTES

Cuando un producto maneje lotes:

cada entrada debe poder indicar:

```text
Número de lote
Fecha elaboración, si corresponde
Fecha caducidad, si corresponde
Cantidad
Bodega
Costo
```

Las cantidades por lote deben cuadrar con la existencia global del producto en la bodega.

Debe cumplirse conceptualmente:

```text
SUM(stock lotes activos de producto/bodega)
=
stock del producto/bodega
```

para productos controlados por lote.

No permitas cantidades huérfanas fuera de lote cuando el producto obligue a utilizarlo.

---

# 14. CADUCIDAD

Si el producto tiene habilitado control de caducidad:

debe conservarse:

```text
fecha_elaboracion
fecha_caducidad
```

según las reglas actuales de Productos.

Debe ser posible posteriormente consultar:

```text
Por caducar
Caducado
Vigente
```

utilizando los días de anticipación configurados.

No almacenes estados que puedan derivarse fácilmente de fechas salvo que la arquitectura actual justifique hacerlo.

---

# 15. SERIES

Cuando un producto maneja números de serie:

una unidad serializada debe corresponder a una serie individual.

Debe impedirse:

* duplicar una serie activa para el mismo producto;
* ingresar dos veces la misma serie incorrectamente;
* sacar una serie que no existe;
* sacar una serie de otra bodega;
* vender posteriormente una serie ya utilizada;
* tener más series activas que stock.

Debe conservarse trazabilidad completa de la serie.

Una serie debería permitir saber posteriormente:

```text
cómo ingresó;
en qué compra;
en qué fecha;
en qué bodega;
si fue transferida;
si salió;
en qué venta salió.
```

No desarrolles todavía Ventas, pero deja el modelo preparado para ello.

---

# 16. COSTOS

Revisa cuidadosamente el modelo actual de costos.

KONTAXPRO contempla conceptualmente:

```text
ultimo_precio_compra
ultimo_costo_efectivo
costo_promedio
```

No sustituyas estas propiedades sin revisar antes lo implementado.

---

# 17. COSTO PROMEDIO PONDERADO

La entrada por compra debe actualizar correctamente el costo promedio.

Para existencia previa mayor que cero:

```text
NuevoCostoPromedio =
(
    StockAnterior * CostoPromedioAnterior
    +
    CantidadEntrada * CostoEntrada
)
/
(
    StockAnterior + CantidadEntrada
)
```

Debe trabajarse siempre en cantidad base.

Ejemplo:

```text
Stock:
10 unidades
Costo promedio:
$5

Nueva compra:
20 unidades
Costo efectivo:
$7
```

Resultado:

```text
(10 × 5) + (20 × 7)
--------------------
      10 + 20

= 6.333333...
```

Respeta la precisión existente en base de datos.

No redondees anticipadamente.

---

# 18. CUANDO NO EXISTE STOCK ANTERIOR

Si antes de la entrada:

```text
StockActual = 0
```

el nuevo costo promedio debe tomar el costo efectivo de la nueva entrada.

No realizar divisiones innecesarias.

---

# 19. SALIDAS

Las salidas futuras, incluyendo Ventas, no deben recalcular el costo promedio.

Deben utilizar el costo promedio vigente en el momento de la salida como costo del movimiento.

Ejemplo:

```text
Stock = 100
CostoPromedio = 4.25

Venta = 5
```

Salida:

```text
CantidadBase = 5
CostoUnitario = 4.25
CostoTotal = 21.25
```

Después:

```text
Stock = 95
CostoPromedio = 4.25
```

El costo promedio cambiará cuando exista una nueva entrada que deba afectarlo.

---

# 20. COSTO DE ENTRADA DESDE COMPRAS

No asumas que el precio de compra bruto siempre es igual al costo de inventario.

Revisa cómo Compras V1 maneja actualmente:

* descuento;
* bonificación;
* impuestos;
* presentación;
* cantidad;
* factor de conversión;
* costo efectivo;
* valores que forman o no forman parte del costo.

Si ya existe una regla de `costo_efectivo`, úsala.

No implementes una segunda fórmula diferente en Inventario.

Inventario debe recibir de Compras el costo que corresponda según las reglas actuales.

---

# 21. BONIFICACIONES

KONTAXPRO debe soportar compras como:

```text
100 + 10 de bonificación
```

La bonificación aumenta la cantidad disponible.

El costo efectivo debe distribuirse correctamente sobre las unidades realmente recibidas según la lógica existente en Compras.

Ejemplo conceptual:

```text
Cantidad pagada = 100
Bonificación = 10
Cantidad recibida = 110
```

El motor debe registrar correctamente las 110 unidades base correspondientes.

Revisa primero cómo Compras V1 ya representa estos valores.

---

# 22. COMPRAS V1 DEBE INTEGRARSE CON INVENTARIO

Esta será la primera integración real del motor.

Analiza el flujo actual de Compras.

Identifica claramente en qué estado del proceso una compra pasa a ser definitiva y debe afectar inventario.

No asumas nombres de estados; utiliza los actuales.

Cuando corresponda:

```text
COMPRA CONFIRMADA
        ↓
INVENTARIO
        ↓
MOVIMIENTO ENTRADA COMPRA
        ↓
EXISTENCIAS
        ↓
LOTES / SERIES
        ↓
COSTOS
```

---

# 23. UNA COMPRA NO PUEDE AFECTAR INVENTARIO DOS VECES

Este requisito es crítico.

Si por cualquier motivo:

* se hace doble clic;
* se repite una petición;
* se vuelve a abrir la operación;
* se reinicia la aplicación;
* ocurre un timeout;
* se reintenta el proceso;

la misma compra **no debe duplicar la entrada de inventario**.

Debe existir idempotencia.

Utiliza:

```text
OrigenTipo + OrigenId
```

o la estrategia equivalente más apropiada según el modelo actual.

Idealmente también debe existir protección a nivel de base de datos cuando corresponda.

---

# 24. MODIFICACIÓN DE UNA COMPRA YA APLICADA

Revisa las reglas actuales de Compras.

No permitas que editar una compra ya aplicada al inventario cambie silenciosamente:

```text
cantidad
producto
costo
lote
serie
bodega
```

Si la arquitectura permite modificar una operación ya confirmada, debe existir una estrategia explícita:

```text
REVERTIR MOVIMIENTO ANTERIOR
+
APLICAR NUEVO MOVIMIENTO
```

o la regla que corresponda al diseño actual.

No destruyas el historial.

---

# 25. ANULACIÓN / REVERSO DE COMPRA

Cuando una compra que ya afectó inventario deba anularse:

no debes borrar la entrada original.

Debe producirse un reverso controlado.

Antes del reverso valida que sea posible.

Ejemplo:

```text
Compra ingresó 10 unidades.
Posteriormente ya se vendieron 8.
Stock actual = 2.
```

No puede restarse automáticamente 10 dejando:

```text
Stock = -8
```

sin aplicar las reglas correspondientes.

La operación debe detectar esta condición y devolver un resultado de negocio claro.

Para lotes y series la validación debe ser todavía más estricta.

---

# 26. OPERACIONES SIN COMPROBANTE

El proyecto ya tiene el módulo de **Operaciones sin comprobante**.

Analízalo antes de realizar cambios.

Determina cuáles de esas operaciones:

```text
afectan inventario
```

y cuáles:

```text
no afectan inventario
```

No asumas que todas deben producir Kardex.

Las operaciones que realmente tengan impacto físico deben utilizar **el mismo motor de inventario**.

No implementes actualizaciones directas de stock dentro de ese módulo.

---

# 27. RELACIÓN CON CONTABILIDAD

Ya hemos ido preparando estructuras y relaciones con Contabilidad.

No rompas ese diseño.

Inventario debe mantener una separación clara entre:

```text
movimiento físico de inventario
```

y:

```text
asiento contable
```

Una compra puede provocar ambos efectos, pero eso no significa que Inventario deba duplicar el registro contable que ya gestione Compras/Contabilidad.

Revisa el diseño actual.

No generes asientos duplicados.

Deja claramente identificado el origen del movimiento para que posteriormente Contabilidad pueda relacionar:

```text
Documento
MovimientoInventario
AsientoContable
```

cuando corresponda.

---

# 28. MOVIMIENTOS MANUALES

Implementa la infraestructura necesaria para movimientos manuales.

Como mínimo necesitamos poder soportar:

```text
AJUSTE POSITIVO
AJUSTE NEGATIVO
```

Un ajuste debe requerir:

```text
Bodega
Producto
Cantidad
Motivo
Observación
Usuario
Fecha
```

y cuando corresponda:

```text
Lote
Serie
Costo
```

No permitir ajustes sin una justificación.

---

# 29. SALDO INICIAL

Debe existir una forma controlada de registrar saldos iniciales cuando sea necesario.

No confundir:

```text
saldo inicial
```

con:

```text
ajuste
```

Debe quedar claramente identificado en Kardex.

Si el proyecto ya contempla migración de inventario desde KONTAX antiguo, deja esta operación preparada para ser usada posteriormente en ese proceso.

---

# 30. TRANSFERENCIAS ENTRE BODEGAS

Implementa la infraestructura de transferencia entre bodegas.

Una transferencia debe representar una única operación de negocio, pero con impacto consistente en ambos lados:

```text
BODEGA ORIGEN
    - cantidad
        ↓
TRANSFERENCIA
        ↓
BODEGA DESTINO
    + cantidad
```

Debe ejecutarse de forma atómica.

Nunca debe ocurrir:

```text
salió del origen
pero no ingresó al destino
```

por un error parcial.

Utiliza una transacción de base de datos.

---

# 31. TRANSFERENCIAS Y COSTO

Una transferencia no debe recalcular el costo promedio empresarial como si fuera una nueva compra.

Debe conservar el costo correspondiente del inventario transferido según el modelo actual.

No generar utilidad ni costo de compra ficticio.

---

# 32. TRANSFERENCIAS CON LOTES

Si el producto maneja lotes:

debe transferirse explícitamente el lote.

Ejemplo:

```text
Producto: X
Lote: L24001
Origen: Matriz
Destino: Sucursal
Cantidad: 5
```

Debe disminuir:

```text
Lote L24001 / Matriz
```

y aumentar:

```text
Lote L24001 / Sucursal
```

sin crear un lote diferente artificialmente.

---

# 33. TRANSFERENCIAS CON SERIES

Si maneja series:

deben seleccionarse las series concretas.

No aceptar únicamente:

```text
Cantidad = 5
```

sin identificar qué cinco series se trasladaron.

Cada serie debe cambiar de bodega conservando su trazabilidad.

---

# 34. VENTA SIN STOCK

El proyecto contempla una configuración similar a:

```text
Permitir venta sin stock
```

No desarrolles Ventas todavía.

Pero el motor debe quedar preparado para consultar esta política posteriormente.

No hardcodees:

```text
stock nunca puede ser negativo
```

si el diseño de KONTAXPRO permite configurarlo.

Sin embargo, movimientos manuales, transferencias y series/lotes deben aplicar sus propias reglas de integridad aunque exista esa configuración.

Una serie inexistente nunca puede “venderse con stock negativo”.

---

# 35. STOCK RESERVADO

Deja preparada correctamente la distinción:

```text
StockActual
StockReservado
StockDisponible
```

Ventas podrá utilizar reservas posteriormente si decidimos implementarlas.

No es obligatorio construir todo el sistema de reservas ahora si no existe todavía en la arquitectura.

Pero evita una implementación que haga imposible añadirlo después.

---

# 36. CONCURRENCIA

Este punto es crítico porque KONTAXPRO funcionará en red y puede tener varios puntos de facturación.

Dos usuarios pueden operar el mismo producto simultáneamente.

No confíes únicamente en:

```text
leer stock
if stock >= cantidad
actualizar stock
```

porque existe condición de carrera.

Utiliza las capacidades transaccionales de PostgreSQL/EF Core y el patrón actual del proyecto para garantizar consistencia.

Analiza cuál estrategia encaja mejor:

* actualización atómica;
* bloqueo adecuado;
* control optimista;
* concurrency token;
* transacción con nivel apropiado.

No introduzcas una estrategia compleja sin necesidad, pero **el stock no puede depender únicamente de validaciones en memoria**.

---

# 37. TRANSACCIONES

Una operación que afecte:

```text
movimiento
detalle
existencia
costo
lotes
series
```

debe confirmarse como una sola unidad de trabajo.

Si falla una parte:

```text
ROLLBACK COMPLETO
```

No deben quedar:

```text
movimiento sin existencia;
existencia sin movimiento;
serie sin movimiento;
lote descuadrado;
costo actualizado sin stock.
```

---

# 38. DISEÑO DEL MOVIMIENTO

Revisa primero las tablas existentes.

Conceptualmente necesitamos una cabecera y detalle.

La cabecera debería poder expresar algo equivalente a:

```text
Id
EmpresaId
TipoMovimiento
Fecha
OrigenTipo
OrigenId
BodegaOrigenId nullable
BodegaDestinoId nullable
Estado
Observacion
UsuarioId
FechaCreacion
MovimientoReversadoId nullable
```

El detalle conceptualmente:

```text
Id
MovimientoId
ProductoId
PresentacionId nullable
CantidadComercial
FactorConversion
CantidadBase
TipoEntradaSalida
CostoUnitarioBase
CostoTotal
LoteId nullable
SerieId nullable
```

**NO crees automáticamente estas columnas.**

Primero compara esto con las entidades/tablas actuales y utiliza la estructura existente siempre que sea válida.

Modifica el esquema solamente cuando exista una necesidad real.

---

# 39. EL MOVIMIENTO DEBE CONSERVAR INFORMACIÓN HISTÓRICA

No dependas exclusivamente del valor actual de la presentación o producto para reconstruir un movimiento antiguo.

Ejemplo:

si posteriormente cambia:

```text
FactorConversión CAJA X 10
```

un movimiento histórico no puede cambiar de significado.

Por ello, guarda en el movimiento los valores históricos necesarios, como:

```text
cantidad comercial;
factor aplicado;
cantidad base;
costo aplicado.
```

Adapta esta regla a la estructura actual.

---

# 40. KARDEX

El Kardex debe construirse sobre los movimientos reales registrados.

Debe poder consultar por producto y bodega.

Como mínimo mostrar:

```text
Fecha
Tipo movimiento
Origen / documento
Bodega
Entrada cantidad
Entrada costo unitario
Entrada valor
Salida cantidad
Salida costo unitario
Salida valor
Saldo cantidad
Costo promedio / costo vigente
Saldo valorado
Usuario
```

Cuando corresponda:

```text
Lote
Serie
Presentación
```

---

# 41. SALDO HISTÓRICO

El Kardex debe ser capaz de mostrar saldo progresivo.

Ejemplo:

```text
Fecha      Operación       Entrada   Salida   Saldo
---------------------------------------------------
01/08      Saldo inicial       10               10
02/08      Compra              20               30
03/08      Venta                         5       25
04/08      Ajuste +             2               27
```

El saldo histórico no debe confundirse con el stock actual.

Debe poder reconstruirse correctamente desde movimientos.

---

# 42. KARDEX VALORADO

Cuando existan costos suficientes, mostrar además:

```text
Costo unitario
Valor entrada
Valor salida
Costo promedio
Saldo valorado
```

No recalcules valores históricos utilizando el costo actual.

Cada movimiento debe conservar el costo utilizado en el momento de producirse.

---

# 43. FILTROS DEL KARDEX

Implementar filtros útiles:

```text
Producto
Bodega
Rango de fechas
Tipo de movimiento
Entrada / Salida
Lote
Serie
Origen
```

No es necesario mostrar todos simultáneamente si perjudica la interfaz.

Diseña una experiencia sencilla.

---

# 44. NUEVO MÓDULO VISUAL INVENTARIO

Crear el módulo visual siguiendo exactamente el Look & Feel actual de KONTAXPRO.

No diseñar con base en KONTAX antiguo.

Tomar como referencia directa:

* ProductsView.
* Compras.
* Clientes.
* Proveedores.
* componentes visuales recientes.
* estilos actuales.
* Light/Dark.
* controles actuales.
* scrolls personalizados.
* tablas.
* botones.
* tarjetas KPI.
* drawers/paneles laterales si ya se están utilizando.

---

# 45. PANTALLA PRINCIPAL DE INVENTARIO

Quiero una pantalla moderna y práctica.

Propongo conceptualmente:

```text
INVENTARIO
────────────────────────────────────────────────────────

[ Valor Inventario ] [ Productos sin stock ]
[ Stock bajo       ] [ Por caducar          ]

[Búsqueda........................] [Bodega ▼] [Filtros]

Producto        Bodega       Stock      Disponible   Costo prom.
----------------------------------------------------------------
IVER...         PRINCIPAL     120           120         $5.34
ARROZ...        PRINCIPAL      45            45        $31.20
...
```

Al seleccionar un producto:

```text
Ver Kardex
Ver lotes
Ver series
Ajustar
Transferir
```

según las características del producto.

No copies literalmente este diseño si el patrón visual actual recomienda una variante mejor.

---

# 46. KPI: VALOR DEL INVENTARIO

Calcular conceptualmente:

```text
SUM(
    StockActual × CostoPromedio
)
```

por empresa/bodega según filtro.

Excluir servicios y productos que no manejan inventario.

No mezclar bodegas de otras empresas.

---

# 47. KPI: SIN STOCK

Productos que manejan inventario y cumplen:

```text
StockActual <= 0
```

según el alcance/filtro seleccionado.

Revisa cómo ProductsView ya calcula este KPI para no crear resultados inconsistentes entre módulos.

---

# 48. KPI: STOCK BAJO

Debe utilizar:

```text
StockMinimo
```

según la lógica ya existente.

Idealmente:

```text
StockActual > 0
AND
StockActual <= StockMinimo
```

pero verifica la regla actual de Productos antes de duplicarla.

---

# 49. KPI: POR CADUCAR

Debe reutilizar exactamente las reglas actuales de:

```text
control caducidad
días anticipación
fecha caducidad
```

No inventar otra fórmula distinta de la utilizada en Productos.

---

# 50. DETALLE DE INVENTARIO DEL PRODUCTO

Debe ser posible abrir un detalle con:

```text
Stock total
Stock reservado
Stock disponible
Costo promedio
Último costo
Última compra

Stock por bodega
Stock por lote
Series disponibles
Movimientos recientes
```

Mostrar únicamente las secciones aplicables.

Ejemplo:

Producto normal:

```text
no mostrar Series
no mostrar Lotes
```

Producto por lote:

```text
mostrar Lotes
```

Producto serializado:

```text
mostrar Series
```

---

# 51. VENTANA / PANEL DE KARDEX

Debe permitir revisar cómodamente movimientos.

Mantén el diseño moderno y consistente.

Idealmente mostrar:

```text
PRODUCTO
Código
Descripción
Bodega
Stock actual
Costo promedio
```

y debajo:

```text
Fecha | Movimiento | Documento | Entrada | Salida | Saldo | Costo | Usuario
```

El usuario no debería necesitar conocimientos contables para entenderlo.

---

# 52. AJUSTE DE INVENTARIO

Crear UI para ajuste positivo/negativo.

Debe ser sencilla.

Ejemplo:

```text
Producto: IVERMEC 100 ML
Bodega: Principal

Stock actual: 25

Tipo:
( ) Entrada
( ) Salida

Cantidad:
[      ]

Motivo:
[ Diferencia inventario ▼ ]

Observación:
[                         ]

Nuevo stock:
30
```

Para lote/serie adaptar automáticamente la UI.

---

# 53. TRANSFERENCIA ENTRE BODEGAS

Crear UI específica.

Conceptualmente:

```text
Bodega origen
Bodega destino
Producto
Presentación
Cantidad

Stock disponible origen

Lote / Series cuando corresponda

Observación
```

No permitir:

```text
Origen = Destino
```

---

# 54. VALIDACIONES VISUALES

Mantener la filosofía actual:

* mensajes claros;
* validación cerca del control;
* no mostrar excepciones técnicas al usuario;
* botones deshabilitados cuando la operación no es válida;
* estados de carga;
* evitar doble ejecución;
* feedback al guardar;
* diseño Light/Dark.

---

# 55. NO BLOQUEAR LA UI

Toda operación que involucre base de datos debe utilizar async adecuadamente según los patrones actuales.

No utilizar:

```text
.Result
.Wait()
```

en el hilo UI.

No introducir deadlocks.

---

# 56. MVVM

Mantener estrictamente el patrón utilizado actualmente:

```text
View
ViewModel
Application Service
Infrastructure
```

No colocar lógica de inventario en:

```text
.xaml.cs
```

salvo comportamiento exclusivamente visual que realmente corresponda allí.

No colocar SQL ni EF directamente en ViewModels.

---

# 57. SERVICIO CENTRAL DE INVENTARIO

Después de analizar la arquitectura, crea o consolida una abstracción equivalente a:

```text
IInventoryService
```

o el nombre que encaje con las convenciones actuales.

Debe centralizar las operaciones de negocio.

Conceptualmente debería poder soportar:

```text
RegistrarEntradaCompra
RegistrarSalidaVenta        // preparado para futuro
RegistrarAjuste
RegistrarTransferencia
RevertirMovimiento
ConsultarExistencia
ConsultarKardex
```

No necesitas implementar Venta todavía si no corresponde, pero la arquitectura no debe obligarnos a modificar todo cuando llegue Ventas.

---

# 58. NO CREAR UN "GOD SERVICE"

No quiero:

```text
InventoryService.cs
5000 líneas
```

Si el dominio lo requiere, separa responsabilidades.

Por ejemplo:

```text
InventoryMovementService
InventoryCostService
InventoryQueryService
InventoryTransferService
InventoryReversalService
```

o una estructura equivalente.

Pero tampoco sobrearquitectes creando decenas de interfaces triviales.

Busca equilibrio y sigue el estilo actual.

---

# 59. COMMAND / RESULT

Las operaciones importantes deberían devolver resultados explícitos.

Ejemplo conceptual:

```text
InventoryOperationResult
```

con información como:

```text
Success
MovementId
Warnings
ValidationErrors
```

No utilices `bool` para representar todos los resultados.

Utiliza el patrón de resultados que ya exista en KONTAXPRO si existe.

---

# 60. AUDITORÍA

Cada movimiento debe permitir conocer:

```text
Quién
Cuándo
Qué operación
Qué documento la originó
Qué cantidades cambió
Qué bodega
```

Reutiliza la infraestructura de auditoría existente.

No implementes una segunda auditoría paralela.

---

# 61. FECHA DEL MOVIMIENTO

Diferencia correctamente:

```text
fecha de la operación de negocio
```

de:

```text
fecha/hora de registro
```

si el modelo actual lo requiere.

No uses únicamente `DateTime.Now` repartido por todo el código.

Sigue el patrón actual de tiempo del proyecto si existe.

---

# 62. ELIMINACIÓN

No agregar botones:

```text
Eliminar movimiento
```

para movimientos aplicados.

La corrección debe hacerse mediante:

```text
Reversar
```

cuando corresponda.

---

# 63. RECONCILIACIÓN

Implementa al menos internamente una forma de verificar inconsistencias.

Debe poder detectarse:

```text
Existencia != suma esperada de movimientos
```

o:

```text
Existencia por lote != existencia producto
```

o:

```text
Cantidad de series activas != stock
```

cuando corresponda.

No necesariamente debe ser una pantalla de usuario V1.

Puede ser un servicio/test de integridad.

Esto será muy útil para soporte técnico.

---

# 64. NO CALCULAR TODO EL STOCK DESDE CERO EN CADA CONSULTA

El movimiento es la fuente histórica.

La tabla de existencias debe ser la proyección actual optimizada para consulta.

Por tanto:

```text
MOVIMIENTOS = historial
EXISTENCIAS = estado actual
```

No hagas un `SUM` de millones de movimientos cada vez que abrimos Productos.

Ambos deben mantenerse consistentes transaccionalmente.

---

# 65. ÍNDICES

Revisa los índices actuales de PostgreSQL.

Asegúrate de que consultas habituales tengan índices adecuados:

```text
Empresa
Producto
Bodega
Fecha
OrigenTipo + OrigenId
Lote
Serie
```

No agregues índices indiscriminadamente.

Justifica los nuevos según consultas reales.

---

# 66. CONSTRAINTS

Utiliza constraints de base de datos para reforzar invariantes importantes cuando sea apropiado.

Ejemplos conceptuales:

```text
cantidad > 0
factor_conversion > 0
```

unicidad de series, relaciones válidas, etc.

Pero no dupliques constraints existentes.

---

# 67. MIGRACIONES

Si necesitas modificar la base:

1. revisa migraciones anteriores;
2. crea una migración limpia;
3. no edites migraciones ya aplicadas salvo que exista una razón extraordinaria;
4. revisa el SQL generado;
5. confirma que no elimina información accidentalmente.

Después ejecuta nuevamente pruebas.

---

# 68. INTEGRACIÓN CON PRODUCTS VIEW

ProductsView ya muestra información de stock.

Después de implementar el nuevo motor:

asegúrate de que sus KPI y cantidades provengan de la fuente correcta.

No mantengas dos formas diferentes de calcular stock.

Si se requiere refactorizar ProductsView para usar el nuevo servicio de consultas, hazlo cuidadosamente y cubre con pruebas.

---

# 69. INTEGRACIÓN CON COMPRAS

Después de crear el motor:

modifica Compras para utilizarlo.

No dejes:

```text
CompraService actualizando stock
+
InventoryService actualizando stock
```

simultáneamente.

Debe quedar una única vía.

Realiza pruebas de regresión completas del módulo Compras.

---

# 70. PRUEBAS UNITARIAS DEL MOTOR

Crear pruebas exhaustivas.

Como mínimo:

### Entrada simple

```text
Stock 0
Entrada 10
Resultado 10
```

### Entrada sobre stock existente

```text
Stock 10
Entrada 5
Resultado 15
```

### Presentación

```text
2 cajas × factor 10
Resultado +20 base
```

### Costo promedio

```text
10 @ 5
+
20 @ 7
=
30 @ 6.333333...
```

### Stock cero + primera compra

Costo promedio = costo de entrada.

### Entrada con bonificación

Cantidad final correcta.

### Servicio

No genera inventario.

### Lote

Stock producto = stock lote.

### Serie

Una serie = una unidad.

### Serie duplicada

Rechazada.

### Transferencia

```text
Origen -10
Destino +10
Total empresa sin cambio
```

### Transferencia con lote

Lote correcto en ambas bodegas.

### Transferencia con series

Las mismas series cambian de bodega.

### Ajuste positivo

Aumenta.

### Ajuste negativo

Disminuye.

### Reverso

Restaura correctamente el estado cuando procede.

### Idempotencia

Ejecutar dos veces la misma compra:

```text
NO duplica stock
```

### Rollback

Provocar error durante la operación:

```text
ninguna tabla queda parcialmente modificada
```

### Empresa

Una empresa no afecta a otra.

### Bodega

Una bodega no afecta a otra.

---

# 71. PRUEBAS DE CONCURRENCIA

Crear al menos pruebas de integración relevantes para escenarios concurrentes.

Ejemplo:

```text
Stock = 10

Operación A intenta sacar 8
Operación B intenta sacar 7
```

No debe producirse silenciosamente:

```text
Stock = -5
```

si la política de esa operación no lo permite.

La validación debe ocurrir también a nivel transaccional, no solamente en ViewModel.

---

# 72. PRUEBAS DE COMPRAS + INVENTARIO

Crear pruebas end-to-end/integración para:

```text
Crear/confirmar compra
→ movimiento
→ stock
→ costo
```

incluyendo:

* presentación base;
* presentación con factor;
* lote;
* serie;
* bonificación;
* múltiples productos;
* servicio que no maneja inventario;
* compra repetida;
* reverso/anulación cuando corresponda.

---

# 73. PRUEBAS DE OPERACIONES SIN COMPROBANTE

Para aquellas operaciones que realmente afecten stock:

comprobar:

```text
Operación
→ Movimiento Inventario
→ Existencia
```

y que una operación que no afecta inventario:

```text
NO genere movimiento.
```

---

# 74. DATOS REALES DE PRUEBA

Utiliza los productos/presentaciones de fixtures o datos de pruebas existentes.

No dependas de datos manuales en la base local del desarrollador.

Las pruebas deben poder ejecutarse repetidamente.

---

# 75. NO ROMPER MÓDULOS TERMINADOS

Después de cada etapa ejecuta:

```powershell
dotnet build
dotnet test
```

Especial atención a regresiones en:

```text
Productos
Clientes
Proveedores
Compras
Operaciones sin comprobante
Contabilidad
Seguridad
Multiempresa
```

---

# 76. ORDEN DE IMPLEMENTACIÓN

No intentes hacerlo todo de una sola vez.

Trabaja en este orden.

## FASE 1 — Auditoría técnica

Analiza todo lo existente.

Entrega internamente un mapa de:

```text
qué ya existe;
qué sirve;
qué falta;
qué debe modificarse;
qué NO debe duplicarse.
```

Luego continúa directamente.

No te detengas esperando confirmación salvo que encuentres una decisión funcional verdaderamente imposible de inferir del proyecto.

---

## FASE 2 — Motor base

Implementa:

* movimientos;
* existencias;
* unidad base;
* presentaciones;
* costos;
* transacciones;
* idempotencia;
* concurrencia.

Pruebas.

Build.

---

## FASE 3 — Lotes y series

Implementa:

* lotes;
* caducidad;
* series;
* reglas de integridad;
* existencias por bodega.

Pruebas.

Build.

---

## FASE 4 — Integración Compras

Conecta Compras V1 con el motor.

Elimina cualquier actualización directa redundante de stock.

Pruebas de regresión.

Build.

---

## FASE 5 — Operaciones sin comprobante

Integra exclusivamente las operaciones que realmente deban afectar inventario.

Pruebas.

Build.

---

## FASE 6 — Ajustes y saldos iniciales

Implementa motor + UI.

Pruebas.

Build.

---

## FASE 7 — Transferencias

Implementa:

* motor;
* atomicidad;
* lotes;
* series;
* UI.

Pruebas.

Build.

---

## FASE 8 — Consultas y Kardex

Implementa:

* consultas;
* saldo progresivo;
* valoración;
* filtros;
* detalle.

Optimiza consultas.

Pruebas.

Build.

---

## FASE 9 — UI Inventario

Implementa:

* pantalla principal;
* KPI;
* detalle;
* Kardex;
* lotes;
* series;
* ajustes;
* transferencias.

Mantén consistencia visual.

---

## FASE 10 — Revisión general

Ejecuta:

```powershell
dotnet build
dotnet test
```

Revisa warnings nuevos.

Haz revisión completa de:

```text
concurrencia;
transacciones;
multiempresa;
idempotencia;
costos;
lotes;
series;
auditoría;
Compras.
```

---

# 77. RENDIMIENTO

Evita problemas típicos como:

```text
N+1 queries
Include gigantescos
cargar todo el Kardex en memoria
```

El Kardex debe soportar paginación.

Las búsquedas deben ejecutarse en base de datos.

No cargar miles de movimientos y después filtrar en memoria.

---

# 78. PAGINACIÓN

Implementar paginación real para:

```text
Kardex
movimientos
```

siguiendo el patrón que actualmente utilice el proyecto.

No inventes un segundo componente si ya existe uno.

---

# 79. UI RESPONSIVA

Mantener buena experiencia al menos en resoluciones habituales que ya hemos venido utilizando:

```text
1920 × 1080
1366 × 768
1280 × 1024
```

No fijar anchos/altos innecesarios.

Evitar ventanas que necesiten una resolución grande para ser utilizables.

---

# 80. LIGHT / DARK

Todo control nuevo debe respetar:

```text
Light Theme
Dark Theme
```

No hardcodear colores que rompan el sistema de temas.

Reutiliza recursos existentes.

---

# 81. SCROLLS

Utiliza los estilos de scrollbar ya implementados.

No permitir que aparezcan scrollbars WPF por defecto que rompan el Look & Feel.

---

# 82. BOTONES Y HOVER

Reutiliza estilos actuales.

No crear variantes visuales innecesarias.

Acciones peligrosas:

```text
Reversar
Ajuste negativo
```

deben diferenciarse visualmente de acciones normales sin exagerar.

---

# 83. MENSAJES AL USUARIO

Ejemplos:

En vez de:

```text
InvalidOperationException
```

mostrar:

```text
No es posible realizar la salida porque la bodega dispone de 3 unidades y se solicitaron 5.
```

Para serie:

```text
La serie ABC123 no está disponible en la bodega seleccionada.
```

Para reverso:

```text
No es posible revertir completamente esta entrada porque parte del stock ya fue utilizado.
```

Los detalles técnicos deben quedar en logs.

---

# 84. LOGGING

Registrar los eventos relevantes.

Por ejemplo:

```text
MovimientoId
EmpresaId
ProductoId
Bodega
Origen
Usuario
Cantidad
Resultado
```

No generar logs excesivos por cada consulta visual.

Diferencia operación de negocio de consulta.

---

# 85. DOCUMENTACIÓN

Al finalizar crea:

```text
/docs/INVENTARIO_KARDEX_V1.md
```

Este documento debe describir el estado **real implementado**, no el plan inicial.

Debe incluir:

1. Objetivo.
2. Arquitectura.
3. Entidades/tablas utilizadas.
4. Flujo de movimientos.
5. Existencias.
6. Presentaciones.
7. Costos.
8. Lotes.
9. Series.
10. Ajustes.
11. Transferencias.
12. Integración Compras.
13. Operaciones sin comprobante.
14. Relación con Contabilidad.
15. Concurrencia.
16. Idempotencia.
17. Reversos.
18. UI.
19. Pruebas.
20. Decisiones técnicas.
21. Pendientes reales para Ventas.

Incluye diagramas Mermaid cuando ayuden.

---

# 86. PREPARAR EL CAMINO PARA VENTAS

Inventario V1 debe quedar diseñado de modo que el próximo módulo pueda hacer:

```text
VENTA
   ↓
Validar stock
   ↓
Seleccionar lote/serie
   ↓
Registrar documento
   ↓
InventoryService
   ↓
SALIDA
   ↓
Existencia
   ↓
Kardex
   ↓
Costo de venta
```

No desarrolles Ventas ahora.

Pero evita cualquier decisión que nos obligue a reconstruir Inventario cuando lleguemos a ella.

---

# 87. PREPARAR EL CAMINO PARA DEVOLUCIONES

También debe poder soportar posteriormente:

```text
DEVOLUCIÓN DE VENTA
→ entrada

DEVOLUCIÓN A PROVEEDOR
→ salida
```

sin crear un segundo mecanismo.

---

# 88. PREPARAR EL CAMINO PARA CONTABILIDAD

Inventario debe poder proporcionar posteriormente información como:

```text
Costo de inventario ingresado
Costo de inventario vendido
Costo de ajustes
Transferencias
Saldos valorizados
```

pero no implementes lógica contable duplicada dentro de Inventario.

Respeta la arquitectura actual de Contabilidad.

---

# 89. NO IMPLEMENTAR TODAVÍA

No quiero que esta tarea se expanda innecesariamente a:

* Ventas.
* Facturación electrónica SRI.
* Nota de crédito.
* Cuentas por cobrar.
* Caja.
* Contabilidad completa.
* RIDE.
* Reportes avanzados.
* Migración completa desde KONTAX antiguo.

Solo prepara correctamente los puntos de extensión.

---

# 90. CRITERIOS DE ACEPTACIÓN

Inventario/Kardex V1 estará listo cuando:

* [ ] Existe un único motor responsable de modificar existencias.
* [ ] Compras utiliza ese motor.
* [ ] Una compra no puede duplicar inventario.
* [ ] Se mantiene stock por bodega.
* [ ] Las presentaciones convierten correctamente a cantidad base.
* [ ] El costo promedio ponderado funciona correctamente.
* [ ] Los costos históricos de movimientos permanecen inmutables.
* [ ] Productos sin inventario no generan movimientos.
* [ ] Lotes funcionan por bodega.
* [ ] Caducidades respetan las reglas existentes.
* [ ] Series tienen trazabilidad y unicidad.
* [ ] Ajustes positivos/negativos funcionan.
* [ ] Transferencias son atómicas.
* [ ] Transferencias de lotes funcionan.
* [ ] Transferencias de series funcionan.
* [ ] Los reversos no eliminan el movimiento original.
* [ ] Existe protección contra concurrencia.
* [ ] Existe idempotencia.
* [ ] Multiempresa está protegida.
* [ ] El Kardex muestra entradas, salidas y saldo histórico.
* [ ] Existe Kardex valorado.
* [ ] Existe pantalla principal de Inventario.
* [ ] Los KPI son consistentes con ProductsView.
* [ ] Existe detalle de stock por bodega.
* [ ] Existe consulta de lotes.
* [ ] Existe consulta de series.
* [ ] La UI funciona Light/Dark.
* [ ] No se introdujeron dependencias innecesarias.
* [ ] `dotnet build` finaliza correctamente.
* [ ] `dotnet test` finaliza sin regresiones.
* [ ] Existe `/docs/INVENTARIO_KARDEX_V1.md`.

---

# 91. REVISIÓN DE CALIDAD FINAL

Antes de considerar terminada la tarea, realiza una revisión crítica buscando específicamente:

### Integridad

```text
¿Existe alguna ruta que modifique stock sin crear movimiento?
```

### Duplicidad

```text
¿Una compra puede aplicarse dos veces?
```

### Costos

```text
¿Algún cálculo utiliza costo actual para alterar un movimiento histórico?
```

### Presentaciones

```text
¿Toda operación utiliza correctamente factor_conversion?
```

### Lotes

```text
¿Puede existir stock sin lote en un producto que obliga lote?
```

### Series

```text
¿Puede duplicarse o salir dos veces una serie?
```

### Transacciones

```text
¿Puede quedar una operación parcialmente aplicada?
```

### Concurrencia

```text
¿Dos terminales pueden generar un stock incorrecto simultáneamente?
```

### Multiempresa

```text
¿Existe cualquier consulta o comando sin filtro/validación de empresa?
```

### Reversos

```text
¿Se elimina o modifica información histórica?
```

### Compras

```text
¿Compra e Inventario tienen dos implementaciones distintas de stock/costo?
```

Si encuentras cualquiera de estos problemas, corrígelo antes de dar por finalizado el módulo.

---

# 92. INFORME FINAL DE CODEX

Cuando termines, dame un resumen claro.

## Estado

```text
INVENTARIO / KARDEX V1
COMPLETADO / PARCIAL
```

## Arquitectura resultante

Explica brevemente cómo quedó organizado el motor.

## Archivos creados

```text
Proyecto
Ruta
Responsabilidad
```

## Archivos modificados

```text
Proyecto
Ruta
Cambio
```

## Base de datos

Indica:

```text
tablas creadas;
tablas modificadas;
índices;
constraints;
migraciones.
```

## Integraciones

Confirma explícitamente:

```text
Productos
Compras
Operaciones sin comprobante
Contabilidad
```

y qué relación quedó implementada con cada una.

## Pruebas

Indica:

```text
cantidad de pruebas nuevas;
dotnet build;
dotnet test;
resultado.
```

## Reglas verificadas

Confirma:

```text
unidad base;
presentaciones;
costo promedio;
lotes;
series;
caducidad;
ajustes;
transferencias;
reversos;
idempotencia;
concurrencia;
multiempresa.
```

## Pendientes

Menciona únicamente pendientes reales y justificados.

---

# 93. REGLA FINAL

No quiero simplemente “hacer que funcione”.

Quiero que **Inventario/Kardex sea una base sólida para Ventas**.

Toma como referencia todo lo que ya hemos aprendido y construido en KONTAXPRO durante Productos, Clientes, Proveedores, Compras, Operaciones sin comprobante y Contabilidad.

No reconstruyas lo que ya está bien.

No introduzcas una arquitectura paralela.

No sacrifiques trazabilidad por simplicidad.

No sacrifiques integridad de inventario por comodidad de implementación.

Y mantén siempre el principio de KONTAXPRO:

> **Robustez por dentro, simplicidad por fuera.**
