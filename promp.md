# KONTAXPRO Desktop — Implementar VENTAS V1

Continuemos con el desarrollo de **KONTAXPRO Desktop**.

Hemos llegado a uno de los módulos centrales del sistema:

> # VENTAS V1

Actualmente ya existen y han evolucionado dentro del proyecto componentes importantes como:

* Productos.
* Clientes.
* Proveedores.
* Compras.
* Operaciones sin comprobante.
* Inventario / Kardex V1.
* Configuración de firma electrónica.
* Motor nativo de Facturación Electrónica SRI.
* Configuración y creación de establecimientos/puntos de emisión según el estado actual del proyecto.
* Seguridad.
* Multiempresa.
* Auditoría.
* Configuración.
* Estructuras y relaciones preparadas para Contabilidad.
* Otros componentes que puedan haberse agregado o modificado durante el desarrollo.

VENTAS debe integrarse con **todo lo que ya existe**.

No quiero crear una implementación paralela de Inventario, Clientes, Productos, precios, impuestos, secuenciales, firma electrónica, SRI, auditoría o Contabilidad.

La filosofía continúa siendo:

> **ROBUSTEZ POR DENTRO, SIMPLICIDAD POR FUERA.**

---

# 1. REGLA OBLIGATORIA: EXPLORA PRIMERO EL PROYECTO ACTUAL

ANTES de diseñar tablas, DTOs, servicios, Views, ViewModels o migraciones:

## EXPLORA EXHAUSTIVAMENTE EL ESTADO ACTUAL DEL REPOSITORIO.

KONTAXPRO ha sufrido numerosos cambios y mejoras desde su arquitectura inicial.

Por tanto:

> **EL CÓDIGO ACTUAL Y LA DOCUMENTACIÓN ACTUAL SON LA FUENTE DE VERDAD.**

No debes asumir que una estructura descrita meses atrás sigue vigente.

Antes de modificar cualquier cosa:

1. Lee `AGENTS.md` de la raíz.
2. Busca otros `AGENTS.md` aplicables.
3. Revisa `/docs`.
4. Lee especialmente la documentación actual de:

   * Productos.
   * Clientes.
   * Proveedores.
   * Compras.
   * Inventario/Kardex.
   * Operaciones sin comprobante.
   * Facturación Electrónica.
   * Firma electrónica.
   * Establecimientos.
   * Puntos de emisión.
   * Secuenciales.
   * Contabilidad.
   * Seguridad.
   * Auditoría.
   * Configuración.
5. Revisa:

   * entidades;
   * DbContext;
   * configuraciones EF;
   * migraciones;
   * interfaces;
   * servicios;
   * repositories, si existen;
   * DTOs;
   * Commands/Results existentes;
   * Views;
   * ViewModels;
   * DI;
   * tests;
   * navegación;
   * permisos.
6. Busca cualquier implementación existente relacionada con:

   * ventas;
   * factura;
   * proforma;
   * cotización;
   * pagos;
   * listas de precios;
   * descuentos;
   * formas de pago;
   * secuenciales;
   * comprobantes electrónicos;
   * operaciones contables;
   * stock;
   * lotes;
   * series.
7. Comprueba cómo trabaja actualmente:

   * `CurrentSession`;
   * EmpresaId;
   * establecimiento;
   * punto de emisión;
   * bodega;
   * usuario;
   * permisos.

Si algo ya existe y está correctamente implementado:

> **REUTILÍZALO.**

No crees una segunda implementación.

---

# 2. AUDITORÍA PREVIA

Antes de escribir código ejecuta:

```powershell
dotnet build
dotnet test
```

Identifica el estado inicial.

No confundas fallos preexistentes con regresiones provocadas por Ventas.

---

# 3. OBJETIVO FUNCIONAL

VENTAS V1 debe permitir completar de forma robusta:

```text
CLIENTE
   ↓
PRODUCTOS / SERVICIOS
   ↓
PRESENTACIONES
   ↓
PRECIOS
   ↓
DESCUENTOS
   ↓
IMPUESTOS
   ↓
FORMA(S) DE PAGO
   ↓
CONFIRMAR VENTA
   ↓
────────────────────────────────
   │              │             │
   ▼              ▼             ▼
INVENTARIO    CONTABILIDAD   FACTURA SRI
   │                            │
   ▼                            ▼
KARDEX                      AUTORIZACIÓN
                                │
                                ▼
                              RIDE
                          cuando exista
```

VENTAS debe ser el **orquestador de la operación comercial**.

No debe reconstruir internamente la lógica de otros módulos.

---

# 4. PRINCIPIO ARQUITECTÓNICO

VENTAS no debe:

* calcular directamente existencias si Inventario ya lo hace;
* modificar stock directamente;
* implementar nuevamente costo promedio;
* crear nuevamente Clientes;
* crear nuevamente Productos;
* implementar nuevamente firma XAdES;
* implementar nuevamente SOAP SRI;
* generar secuenciales con otra lógica;
* crear otro sistema de auditoría;
* duplicar lógica contable;
* duplicar permisos.

Debe consumir los servicios existentes.

Conceptualmente:

```text
SalesService / caso de uso Venta
          │
          ├── Clientes
          ├── Productos
          ├── Precios
          ├── Inventario
          ├── Facturación Electrónica
          ├── Contabilidad
          └── Auditoría
```

Adapta los nombres a la arquitectura ACTUAL.

---

# 5. PRIMERO DETERMINA QUÉ YA EXISTE PARA VENTAS

Antes de crear entidades nuevas busca si ya existen conceptos como:

```text
Venta
VentaDetalle
DocumentoVenta
Comprobante
Transaccion
Pago
FormaPago
Secuencial
DocumentoElectronico
CuentaPorCobrar
```

Si existen:

analiza si forman parte del diseño actual.

No crees:

```text
ventas_v2
facturas_nuevas
ventas_documentos_new
```

para evitar comprender la estructura existente.

---

# 6. ALCANCE DE VENTAS V1

VENTAS V1 debe permitir como mínimo:

* crear una nueva venta;
* seleccionar cliente;
* utilizar consumidor final;
* agregar productos;
* agregar servicios;
* buscar por código;
* buscar por código de barras;
* buscar por descripción;
* seleccionar presentación;
* manejar cantidades;
* manejar precios;
* descuentos;
* IVA/impuestos;
* lotes;
* series;
* disponibilidad de stock;
* subtotales;
* impuestos;
* total;
* formas de pago;
* confirmar;
* generar movimiento de inventario;
* generar factura electrónica;
* enviar al SRI;
* consultar autorización;
* almacenar estado;
* consultar ventas realizadas;
* abrir detalle de venta;
* visualizar estado SRI;
* reintentar/reconciliar comprobantes pendientes de forma segura.

---

# 7. NO CONSTRUIR TODAVÍA TODO EL ERP

VENTAS V1 no debe expandirse innecesariamente a:

* módulo completo de Cuentas por Cobrar;
* Caja avanzada;
* arqueos;
* cierres de caja;
* conciliación bancaria;
* CRM;
* cotizaciones complejas;
* pedidos;
* comisiones;
* promociones avanzadas;
* fidelización;
* notas de crédito completas;
* devoluciones completas;
* RIDE altamente configurable;
* envío por WhatsApp.

Deja puntos de extensión cuando corresponda.

---

# 8. MODELO DE VENTA

Revisa primero las estructuras actuales.

Conceptualmente una venta necesita preservar:

```text
Empresa
Establecimiento
PuntoEmision
Bodega
Cliente
Fecha
Usuario

TipoDocumento

Secuencial
NumeroDocumento
ClaveAcceso

Subtotal
Descuento
Impuestos
Total

EstadoNegocio
EstadoSRI

Observacion
```

y sus detalles.

Pero:

> NO CREES automáticamente estas columnas.

Adapta la solución a lo que ya exista.

---

# 9. DETALLE DE VENTA

Cada línea debe conservar históricamente la información necesaria.

Conceptualmente:

```text
ProductoId
PresentacionId
Codigo
Descripcion

CantidadComercial
FactorConversion
CantidadBase

PrecioUnitario
Descuento
Subtotal

Impuestos
Total

Lote
Serie
```

según corresponda.

---

# 10. SNAPSHOT HISTÓRICO

Esto es muy importante.

Una venta histórica no puede cambiar porque posteriormente cambió:

* nombre del producto;
* descripción;
* código;
* presentación;
* factor de conversión;
* precio;
* tarifa tributaria.

Debe preservarse en la venta la información histórica necesaria.

Ejemplo:

```text
Hoy:
CAJA X 10
Factor = 10
Precio = $25

Dentro de 6 meses:
CAJA X 12
Factor = 12
Precio = $30
```

La factura antigua debe continuar significando:

```text
CAJA X 10
Factor 10
Precio $25
```

No reconstruir documentos históricos leyendo exclusivamente la configuración actual de Producto.

---

# 11. CLIENTE

Reutiliza Clientes V1.

VENTAS debe permitir:

```text
buscar por identificación;
buscar por nombres;
buscar por razón social;
seleccionar cliente;
consumidor final.
```

No crear otro formulario de clientes salvo que exista una operación rápida integrada siguiendo los patrones actuales.

---

# 12. CONSUMIDOR FINAL

Implementar según las reglas tributarias vigentes y según lo que ya haya preparado el motor SRI.

No hardcodees reglas tributarias desde memoria.

Reutiliza la implementación actual del motor de Facturación Electrónica.

---

# 13. PRODUCTOS

Reutiliza Productos V1.

La búsqueda debe ser rápida.

Debe poder localizar por:

```text
Código interno
Código de barras
Descripción
Modelo
```

según las capacidades actuales del módulo.

---

# 14. LECTOR DE CÓDIGO DE BARRAS

VENTAS debe estar preparada para uso real en mostrador.

Cuando el usuario escanee un código:

```text
Código encontrado
    ↓
Agregar producto
```

Si el mismo producto/presentación ya está en la venta y las reglas lo permiten:

```text
incrementar cantidad
```

en lugar de crear líneas innecesariamente duplicadas.

Pero para productos con series/lotes pueden existir reglas distintas.

---

# 15. PRESENTACIONES

Reutiliza exactamente la lógica actual de Productos e Inventario.

Ejemplo:

```text
Producto:
AMOXICILINA

Presentación:
CAJA X 12

Factor:
12

Venta:
2 cajas

CantidadBase:
24
```

Inventario debe recibir la cantidad base correcta.

No vuelva a implementarse la conversión con una fórmula distinta.

---

# 16. PRODUCTOS QUE NO MANEJAN INVENTARIO

Los servicios u otros ítems con:

```text
maneja_inventario = false
```

pueden venderse.

Pero:

```text
NO generan salida de Inventario.
```

---

# 17. LISTAS DE PRECIOS

Revisa el modelo actual.

KONTAXPRO contempla conceptualmente:

```text
Precio A
Precio B
Precio C
```

o listas equivalentes.

Reutiliza la implementación REAL actual.

No recrees listas de precios.

---

# 18. LISTA DE PRECIO DEL CLIENTE

Revisa Clientes y su relación comercial por empresa.

Si actualmente existe una lista predeterminada del cliente:

```text
Cliente → Lista B
```

al seleccionar el cliente debe aplicarse automáticamente según las reglas existentes.

El usuario con permiso podrá cambiarla cuando corresponda.

---

# 19. PRECIO MANUAL

Analiza si actualmente existe política/permisos.

No permitir que cualquier usuario modifique libremente precios si el sistema actual contempla control.

Idealmente debe existir permiso granular equivalente a:

```text
VENTAS_MODIFICAR_PRECIO
```

si corresponde al modelo actual de seguridad.

No hardcodear roles.

---

# 20. DESCUENTOS

Soportar descuentos según las reglas actuales.

Debe ser posible conceptualmente:

```text
descuento por línea
```

y si la arquitectura lo permite:

```text
descuento general
```

pero evita implementar mecanismos duplicados.

---

# 21. DESCUENTO Y PRECIO HISTÓRICO

La venta debe conservar:

```text
precio original;
descuento;
precio efectivo.
```

según el modelo que resulte apropiado.

El total histórico nunca debe depender del precio actual del producto.

---

# 22. IMPUESTOS

Reutiliza catálogos tributarios existentes.

Debe soportarse correctamente la realidad actual del proyecto:

```text
IVA 0
IVA 5
IVA 15
```

y cualquier otra tarifa vigente modelada.

No hardcodees porcentajes en Views/ViewModels.

---

# 23. RIMPE / REGLAS TRIBUTARIAS

Reutiliza las reglas existentes del motor de Facturación Electrónica y Empresa.

No dupliques lógica tributaria dentro de Ventas.

---

# 24. CÁLCULO DE TOTALES

Todos los cálculos deben utilizar:

```text
decimal
```

Nunca:

```text
double
float
```

para dinero.

Revisa las precisiones actuales.

Evita redondeos intermedios incorrectos.

La lógica de XML SRI y la lógica visual deben producir resultados coherentes.

---

# 25. MOTOR ÚNICO DE CÁLCULO

No quiero:

```text
ViewModel calcula total
+
SalesService calcula otro total
+
XML Builder calcula otro total
```

con tres fórmulas diferentes.

Debe existir una única lógica de negocio o una estrategia coherente reutilizable.

La UI solo presenta.

---

# 26. STOCK VISIBLE DURANTE LA VENTA

Al agregar un producto que maneja inventario debe poder conocerse:

```text
StockActual
StockReservado
StockDisponible
```

según lo implementado actualmente.

Mostrar al usuario principalmente:

```text
Disponible
```

si corresponde.

---

# 27. BODEGA

La venta debe utilizar una bodega concreta.

Revisa la relación actual entre:

```text
Empresa
Establecimiento
Punto de emisión
Bodega
Terminal
```

Si actualmente existe una bodega predeterminada por punto/establecimiento:

reutilízala.

No pedir al cajero que seleccione la bodega en cada venta si ya está determinada por configuración.

---

# 28. CONTROL DE STOCK

No hagas únicamente:

```text
if Disponible >= Cantidad
```

en el ViewModel.

La validación definitiva debe ocurrir dentro del motor de Inventario durante la confirmación.

Recuerda que puede haber varias terminales simultáneamente.

---

# 29. CONCURRENCIA

Ejemplo:

```text
Stock disponible = 1

Terminal A vende 1
Terminal B vende 1
```

No pueden confirmarse ambas accidentalmente si la política no permite stock negativo.

Inventario V1 ya debe contener las protecciones.

VENTAS debe utilizar esas APIs correctamente.

No crear un segundo mecanismo de concurrencia.

---

# 30. LOTES

Si un producto maneja lotes:

VENTAS debe respetar Inventario V1.

Debe poder seleccionar el lote correspondiente.

Mostrar información útil:

```text
Lote
Caducidad
Disponible
```

---

# 31. SELECCIÓN DE LOTE

Diseña una UX sencilla.

Cuando un producto requiere lote:

```text
Producto
Cantidad
        ↓
Seleccionar lote(s)
```

Si la cantidad necesita distribuirse entre varios lotes, analiza si Inventario ya soporta esa operación.

Reutiliza su diseño.

---

# 32. FEFO

Analiza si Inventario implementó o preparó una política de:

```text
First Expired, First Out
```

para lotes con caducidad.

Si ya existe:

úsala.

Si no existe y es una extensión natural, puedes sugerir/implementar selección recomendada por menor fecha de caducidad sin ocultar al usuario qué lote se utilizará.

No alteres reglas existentes sin necesidad.

---

# 33. PRODUCTOS CADUCADOS

No permitir salida normal de venta de lotes caducados cuando las reglas de negocio lo prohíban.

La validación final debe vivir en Inventario, no exclusivamente en UI.

---

# 34. SERIES

Para productos serializados:

se debe identificar exactamente cada serie vendida.

Ejemplo:

```text
Cantidad = 2

Series:
SN001
SN007
```

No aceptar únicamente:

```text
Cantidad = 2
```

sin saber qué unidades físicas salieron.

---

# 35. LECTOR PARA SERIES

Si resulta natural con la implementación actual:

permitir escanear las series mediante código de barras.

La cantidad puede derivarse del número de series seleccionadas.

---

# 36. FORMAS DE PAGO

Revisa los catálogos actuales del proyecto y del motor SRI.

Debe poder registrarse el medio/forma de pago requerido.

Ejemplos funcionales pueden incluir:

```text
Efectivo
Transferencia
Tarjeta
Crédito
Otros
```

pero utiliza los códigos y entidades actuales.

---

# 37. PAGOS MIXTOS

Si la arquitectura actual lo permite sin introducir complejidad desproporcionada, soportar:

```text
Total $100

Efectivo     $60
Transferencia $40
```

La suma debe cuadrar con el total.

Esta funcionalidad es muy útil para Ventas.

---

# 38. VENTA A CRÉDITO

Revisa lo que ya exista respecto a:

```text
cuentas por cobrar;
plazos;
pagos;
operaciones financieras.
```

Si ya existe una infraestructura válida:

úsala.

Si todavía no existe Cuentas por Cobrar:

VENTAS debe quedar preparada para una venta a crédito pero **no construir dentro de Ventas un módulo improvisado completo de cartera**.

Documenta claramente la frontera.

---

# 39. NO CONFUNDIR FORMA DE PAGO SRI CON CAJA

La forma de pago tributaria del XML no reemplaza:

```text
movimiento de caja;
cuenta por cobrar;
movimiento bancario.
```

Mantén esas responsabilidades separadas.

---

# 40. ESTABLECIMIENTO Y PUNTO DE EMISIÓN

La venta debe utilizar la configuración existente.

No permitir que una venta tome accidentalmente:

```text
Punto de emisión de otra empresa.
```

Validar en Application/Domain, no solo UI.

---

# 41. SECUENCIAL

No implementes `ultimo + 1`.

Reutiliza el mecanismo atómico que ya creó Facturación Electrónica/Puntos de emisión.

La asignación debe ocurrir en el momento adecuado del ciclo de vida.

---

# 42. NÚMERO DE DOCUMENTO

Debe construirse de la forma correspondiente:

```text
001-001-000000123
```

según:

```text
establecimiento
punto emisión
secuencial
```

No concatenar valores no validados.

---

# 43. CICLO DE VIDA DE LA VENTA

Diseña estados explícitos.

No necesariamente uses estos nombres exactos.

Conceptualmente necesitamos distinguir:

```text
BORRADOR
CONFIRMADA
PENDIENTE_SRI
RECIBIDA_SRI
AUTORIZADA
DEVUELTA
NO_AUTORIZADA
PENDIENTE_RECONCILIACION
ANULADA
```

Revisa qué estructura ya existe.

No crear un segundo catálogo si el proyecto ya dispone de uno adecuado.

---

# 44. ESTADO COMERCIAL VS ESTADO SRI

No mezclar necesariamente:

```text
Venta confirmada
```

con:

```text
Factura autorizada
```

Son hechos distintos.

Ejemplo:

```text
Venta confirmada
Inventario descontado
SRI temporalmente sin conexión
```

La venta sigue existiendo.

El comprobante puede quedar:

```text
PENDIENTE DE ENVÍO / AUTORIZACIÓN
```

---

# 45. SRI ES UN SISTEMA EXTERNO

No intentes envolver:

```text
PostgreSQL + SRI
```

dentro de una transacción ACID ficticia.

Persistir estados intermedios.

La aplicación debe recuperarse después de:

```text
timeout;
pérdida de internet;
cierre;
caída del SRI.
```

---

# 46. FLUJO DE CONFIRMACIÓN

Diseña cuidadosamente el flujo real.

Conceptualmente:

```text
1. Validar venta.
2. Validar cliente.
3. Validar precios/impuestos.
4. Validar inventario.
5. Reservar/asignar secuencial cuando corresponda.
6. Persistir operación comercial.
7. Registrar salida de Inventario.
8. Registrar consecuencias contables que correspondan.
9. Crear comprobante electrónico.
10. Firmar.
11. Enviar.
12. Consultar autorización.
13. Persistir respuesta.
```

Pero:

> NO implementes este orden ciegamente.

Analiza los servicios ya existentes y define el orden correcto según las garantías transaccionales actuales.

Especial cuidado con:

```text
qué ocurre si Inventario fue aplicado pero falla la firma;
qué ocurre si se envió al SRI pero la respuesta se perdió;
qué ocurre si la aplicación se cierra después de RECIBIDA.
```

---

# 47. IDEMPOTENCIA

Confirmar Venta debe ser idempotente.

Doble clic:

```text
NO
```

puede generar dos ventas.

Reintentar SRI:

```text
NO
```

puede generar otro secuencial/comprobante.

Reabrir una venta:

```text
NO
```

debe volver a descontar stock.

Debe existir protección tanto lógica como de persistencia cuando corresponda.

---

# 48. INVENTARIO

La salida debe usar exclusivamente Inventario/Kardex V1.

Conceptualmente:

```text
Venta confirmada
       ↓
InventoryService
       ↓
SALIDA VENTA
       ↓
Kardex
       ↓
Existencia
```

Debe quedar:

```text
OrigenTipo = Venta
OrigenId = VentaId
```

o el equivalente real de la arquitectura actual.

---

# 49. COSTO DE VENTA

VENTAS no debe inventar su propio costo.

Inventario debe proporcionar/registrar el costo vigente correspondiente a la salida.

Este costo será posteriormente necesario para Contabilidad y rentabilidad.

---

# 50. UTILIDAD

Si el proyecto ya tiene permiso para visualizar utilidad:

```text
VENTAS_VER_UTILIDAD
```

o equivalente, reutilízalo.

Conceptualmente:

```text
Utilidad = Venta - Costo
```

pero no expongas información de costo a usuarios sin permiso.

---

# 51. CONTABILIDAD

Revisa cómo ha evolucionado la integración contable.

VENTAS debe usar las estructuras existentes.

Una venta puede conceptualmente provocar:

```text
Cuenta por cobrar / caja
Ingreso
IVA
Costo de venta
Inventario
```

pero:

> NO implementes asientos duplicados.

Reutiliza el motor/reglas actuales.

Si Contabilidad todavía no ejecuta asientos automáticamente, deja el punto de integración correctamente preparado/documentado.

---

# 52. FACTURACIÓN ELECTRÓNICA

Consume el motor SRI ya construido.

VENTAS no debe conocer:

```text
SignedXml
XAdES
SOAP
PKCS12
RSA
XSD
```

Debe enviar al servicio un modelo de comprobante apropiado.

---

# 53. MAPEO VENTA → FACTURA ELECTRÓNICA

Crear una responsabilidad clara equivalente a:

```text
Venta
    ↓
ElectronicInvoiceRequest
```

o utilizar el patrón existente.

Debe mapear:

```text
emisor
cliente
fecha
establecimiento
punto
secuencial
detalles
impuestos
descuentos
pagos
totales
información adicional
```

No duplicar lógica XML.

---

# 54. RESPUESTAS SRI

La interfaz debe poder mostrar:

```text
AUTORIZADA
PENDIENTE
DEVUELTA
NO AUTORIZADA
```

con los mensajes correspondientes.

No mostrar SOAP crudo al usuario.

---

# 55. VENTA AUTORIZADA

Cuando esté autorizada mostrar:

```text
Número de documento
Clave de acceso
Número de autorización
Fecha autorización
```

según corresponda.

---

# 56. VENTA DEVUELTA / NO AUTORIZADA

Debe ser posible abrir los mensajes del SRI.

Ejemplo amigable:

```text
Factura devuelta por el SRI

35 — Documento inválido
Información adicional: ...
```

No perder mensajes secundarios.

---

# 57. RECONCILIACIÓN SRI

VENTAS debe permitir identificar comprobantes:

```text
pendientes;
recibidos sin autorización final;
con comunicación interrumpida.
```

y ejecutar el mecanismo de reconciliación existente en Facturación Electrónica.

No volver a enviar ciegamente.

---

# 58. LISTADO DE VENTAS

Crear una pantalla principal coherente con KONTAXPRO.

Conceptualmente:

```text
VENTAS

[ Ventas hoy ] [ Total hoy ] [ Autorizadas ] [ Pendientes SRI ]

[Buscar................] [Fecha ▼] [Estado ▼] [+ Nueva venta]

Fecha | Documento | Cliente | Total | Pago | Estado SRI
```

Adapta el diseño a las Views actuales.

---

# 59. KPI

Utiliza indicadores útiles pero no sobrecargues.

Podrían incluir:

```text
Ventas hoy
Total vendido
Pendientes SRI
No autorizadas/devueltas
```

Evalúa qué aporta valor real.

No calcular KPI descargando todo a memoria.

---

# 60. BÚSQUEDA

Debe poder buscar ventas por:

```text
Número documento
Cliente
Identificación
Clave acceso
```

según sea razonable.

---

# 61. FILTROS

Como mínimo considera:

```text
Fecha / rango
Estado venta
Estado SRI
Punto emisión
```

según necesidad.

---

# 62. PAGINACIÓN

Implementar paginación real en base de datos.

No cargar todas las ventas históricas en memoria.

Reutiliza el patrón actual.

---

# 63. NUEVA VENTA — UX

Esta será una de las pantallas de uso más frecuente de KONTAXPRO.

Debe ser:

```text
rápida
limpia
moderna
amigable
usable con teclado
usable con lector de código de barras
```

No crear un formulario administrativo lento y lleno de campos.

---

# 64. DISEÑO CONCEPTUAL DE NUEVA VENTA

Podría organizarse:

```text
NUEVA VENTA
────────────────────────────────────────

Cliente:
[ Consumidor Final                         🔎 ]

Buscar producto:
[ Código / código de barras / descripción     ]

────────────────────────────────────────

Producto     Present.   Cant.   Precio   Desc.   Total
------------------------------------------------------
IVER...      FRASCO      2       12.00     0      24.00
ALIM...      SACO        1       28.00     0      28.00

────────────────────────────────────────

Subtotal                             $45.22
Descuento                             $0.00
IVA                                  $6.78
TOTAL                                $52.00

Forma de pago
[ Efectivo ▼ ]

                         [ Cancelar ] [ FACTURAR ]
```

Es una referencia funcional.

No copies ciegamente el layout.

Toma como referencia los componentes visuales actuales del proyecto.

---

# 65. VELOCIDAD DE OPERACIÓN

El flujo ideal para una venta sencilla debería ser:

```text
Abrir Nueva Venta
↓
Escanear productos
↓
F12 / acción equivalente
↓
Seleccionar pago
↓
Facturar
```

No obligar a usar mouse para cada acción.

---

# 66. ATAJOS DE TECLADO

Analiza si KONTAXPRO ya tiene convenciones.

Sería útil preparar atajos para:

```text
buscar cliente;
buscar producto;
finalizar;
cancelar;
cantidad;
eliminar línea.
```

No inventes atajos que entren en conflicto con los existentes.

---

# 67. FOCUS

En nueva venta, el foco inicial debería quedar en el campo de búsqueda/escaneo de producto o cliente según la experiencia elegida.

Después de agregar un producto:

```text
regresar automáticamente al buscador
```

si esto mejora el flujo de mostrador.

---

# 68. AGREGAR PRODUCTO

Cuando se agregue:

mostrar claramente:

```text
Presentación
Stock
Cantidad
Precio
Descuento
IVA
Total
```

sin saturar visualmente.

---

# 69. EDICIÓN DE CANTIDAD

Debe poder modificarse rápidamente.

Cada cambio debe recalcular:

```text
cantidad base
stock requerido
subtotal
impuesto
total
```

utilizando el motor central de cálculo.

---

# 70. ELIMINAR LÍNEA

Mientras la venta está en edición:

permitido.

Después de confirmada:

no modificar directamente la venta histórica.

---

# 71. MODIFICACIÓN DE VENTA CONFIRMADA

Una venta confirmada no debe abrirse como si fuera un formulario editable normal.

No permitir cambiar:

```text
cliente
productos
cantidades
precios
impuestos
```

silenciosamente después de que afectó:

```text
Inventario
SRI
Contabilidad
```

Las correcciones posteriores corresponderán a:

```text
anulación
nota de crédito
devolución
```

según reglas futuras.

---

# 72. BORRADORES

Si el diseño actual admite guardar ventas en borrador:

puede implementarse.

Un borrador:

```text
NO afecta inventario;
NO consume definitivamente un comprobante;
NO se envía al SRI;
NO genera contabilidad.
```

Analiza cuidadosamente los secuenciales.

---

# 73. ANULACIÓN

No implementes una anulación improvisada que borre datos.

Si el alcance actual y SRI permiten una anulación local antes de emisión:

diferénciala claramente.

Para comprobantes ya autorizados:

la corrección futura probablemente requiera Nota de Crédito según el caso.

No desarrolles Nota de Crédito dentro de Ventas V1 salvo infraestructura mínima natural.

---

# 74. PERMISOS

Revisa Seguridad actual.

VENTAS debería contemplar permisos equivalentes según la convención actual:

```text
VENTAS_VER
VENTAS_CREAR
VENTAS_MODIFICAR_PRECIO
VENTAS_APLICAR_DESCUENTO
VENTAS_VER_UTILIDAD
VENTAS_REINTENTAR_SRI
```

No necesariamente uses esos nombres.

No crear permisos duplicados si ya existen.

---

# 75. AUDITORÍA

Auditar operaciones sensibles:

```text
crear venta
confirmar
cambiar precio manual
aplicar descuento especial
reintentar SRI
anular cuando corresponda
```

No duplicar infraestructura.

---

# 76. FECHAS

Diferenciar:

```text
FechaEmision
FechaRegistro
FechaAutorizacion
```

según corresponda.

Utiliza las abstracciones actuales de tiempo.

---

# 77. TRANSACCIONES LOCALES

La parte local de confirmación debe garantizar coherencia entre lo que deba ejecutarse transaccionalmente.

No deben existir estados como:

```text
venta confirmada
pero sin detalles
```

o:

```text
stock descontado
sin referencia de venta
```

por errores parciales.

Reutiliza los mecanismos de Inventario y persistencia existentes.

---

# 78. FALLO DE FIRMA

Si la operación comercial quedó confirmada pero el certificado no puede firmar:

el estado debe permitir comprender y recuperar la situación.

No volver a descontar stock en un reintento.

---

# 79. FALLO DE INTERNET

Si no hay internet:

analiza las capacidades actuales del motor de facturación.

VENTAS debe manejarlo de acuerdo con esa arquitectura.

No perder la venta.

No crear otra factura al reintentar.

---

# 80. DOBLE CLIC EN FACTURAR

Deshabilitar temporalmente el comando en UI mientras procesa.

Pero eso es solo UX.

Debe existir además idempotencia en Application/Infrastructure.

---

# 81. RIDE

Revisa si el motor actual ya implementó RIDE.

Si existe:

integrarlo.

Si todavía no:

dejar la acción preparada sin bloquear Ventas V1.

No construir un nuevo motor PDF si ya hay uno o si forma parte de una etapa posterior.

---

# 82. IMPRESIÓN

Si existe RIDE:

permitir:

```text
Ver RIDE
Imprimir
```

sin acoplar generación PDF al ViewModel.

---

# 83. XML

Desde detalle de una venta autorizada, usuarios con el contexto apropiado deberían poder acceder a:

```text
XML autorizado
```

si ya existe esa funcionalidad/almacenamiento.

No mostrar detalles técnicos innecesarios en la pantalla principal.

---

# 84. VALIDACIONES

Mensajes claros.

Ejemplo:

```text
No hay stock suficiente para IVERMEC 100 ML.
Disponible: 3
Solicitado: 5
```

No:

```text
InvalidOperationException
```

---

# 85. VALIDACIÓN DE SERIES

Ejemplo:

```text
Debe seleccionar 2 números de serie para completar esta venta.
```

---

# 86. VALIDACIÓN DE LOTES

Ejemplo:

```text
La cantidad seleccionada supera el stock disponible del lote L-24011.
```

---

# 87. VALIDACIÓN DE PAGO

Ejemplo:

```text
El valor registrado en las formas de pago debe coincidir con el total de la venta.
```

---

# 88. VALIDACIÓN DE CONFIGURACIÓN SRI

Antes de confirmar debe detectarse oportunamente si:

```text
no existe punto emisión;
certificado caducado;
empresa no está lista para facturar;
```

utilizando el diagnóstico/configuración ya implementado.

No descubrirlo recién después de haber realizado trabajo innecesario.

---

# 89. NO BLOQUEAR UI

Toda operación de base/SRI:

```text
async
```

según patrones actuales.

No utilizar:

```csharp
.Result
.Wait()
```

en UI.

---

# 90. INDICADORES DE PROCESO

Mientras factura:

mostrar estados amigables como:

```text
Validando venta...
Registrando...
Generando comprobante...
Firmando...
Enviando al SRI...
Consultando autorización...
```

No necesariamente cada paso necesita un modal.

Evita que la UI parezca congelada.

---

# 91. LIGHT / DARK

Toda la UI nueva debe funcionar correctamente en ambos temas.

No hardcodear colores innecesariamente.

---

# 92. SCROLLBARS / DATAGRID / HOVER

Reutilizar estilos actuales.

No introducir controles WPF con apariencia por defecto que rompan el Look & Feel.

---

# 93. RESOLUCIONES

Diseñar considerando al menos:

```text
1920x1080
1366x768
1280x1024
```

La pantalla de venta debe seguir siendo práctica en 1366×768.

---

# 94. TESTS — CÁLCULOS

Crear pruebas para:

```text
precio
cantidad
presentación
factor
descuento
IVA
subtotal
total
redondeo
```

---

# 95. TESTS — CLIENTE

Probar:

```text
cliente normal;
RUC;
cédula;
consumidor final;
cliente de otra empresa cuando la relación comercial lo impida.
```

Adapta a las reglas actuales.

---

# 96. TESTS — PRESENTACIONES

Ejemplo:

```text
Venta 2 CAJAS
Factor 10
Salida inventario = 20 base
```

---

# 97. TESTS — PRODUCTO SERVICIO

Un servicio:

```text
entra en factura
NO sale de inventario
```

---

# 98. TESTS — LOTES

Probar:

```text
lote correcto;
lote sin stock;
lote de otra bodega;
lote caducado;
múltiples lotes si está soportado.
```

---

# 99. TESTS — SERIES

Probar:

```text
serie disponible;
serie inexistente;
serie de otra bodega;
serie duplicada;
serie ya vendida.
```

---

# 100. TESTS — INVENTARIO

Venta confirmada:

```text
stock antes = 10
venta = 3
stock después = 7
```

Debe existir:

```text
movimiento de Kardex
Origen = Venta
```

---

# 101. TESTS — IDEMPOTENCIA

Ejecutar dos veces la confirmación de la misma venta.

Resultado:

```text
1 venta
1 movimiento inventario
1 comprobante
```

No:

```text
2 salidas
2 secuenciales
```

---

# 102. TESTS — CONCURRENCIA

Ejemplo:

```text
stock = 5

Terminal A vende 4
Terminal B vende 4
```

El resultado debe respetar la política de Inventario.

No confiar en una validación antigua de UI.

---

# 103. TESTS — SRI

Usar el motor existente con clientes simulados cuando corresponda.

Probar:

```text
RECIBIDA + AUTORIZADO
DEVUELTA
NO AUTORIZADO
PENDIENTE
timeout
```

---

# 104. TESTS — FALLO DESPUÉS DE INVENTARIO

Simula una excepción después de aplicar la operación local cuando sea técnicamente posible.

Comprueba que el estado permite recuperación sin duplicar stock.

---

# 105. TESTS — PAGOS

Probar:

```text
efectivo total;
pago mixto;
monto menor;
monto mayor;
crédito si está soportado.
```

---

# 106. TEST END-TO-END

Debe existir como mínimo un flujo completo simulado:

```text
Seleccionar cliente
↓
Agregar producto
↓
Aplicar precio
↓
Calcular impuestos
↓
Registrar pago
↓
Confirmar
↓
Salida Inventario
↓
Factura electrónica
↓
RECIBIDA
↓
AUTORIZADA
↓
Venta finalizada
```

---

# 107. PRUEBA REAL

Cuando el motor SRI y la configuración actual lo permitan:

realizar una prueba controlada en:

```text
AMBIENTE DE CERTIFICACIÓN
```

con una venta/factura de prueba.

Nunca emitir automáticamente en Producción durante tests.

---

# 108. NO CAMBIAR DIRECTAMENTE A PRODUCCIÓN

La selección de ambiente debe provenir de Configuración.

VENTAS no debe tener un switch independiente:

```text
Pruebas / Producción
```

---

# 109. INTEGRACIÓN CON MENÚ

Agregar Ventas al menú siguiendo la estructura actual.

No asumir que la organización inicial del menú sigue vigente.

Explora cómo Codex lo ha reorganizado.

---

# 110. NAVEGACIÓN

Reutiliza `INavigationService` o la abstracción vigente.

No crear navegación manual innecesaria.

---

# 111. PERFORMANCE

Evitar:

```text
N+1
Include gigantes
cargar todos los productos
cargar todas las ventas
filtrar en memoria
```

Búsquedas y listados deben ser eficientes.

---

# 112. BÚSQUEDA DE PRODUCTOS PARA POS

Debe responder rápido incluso con miles de productos.

Utilizar consulta paginada/limitada.

El escaneo de código de barras exacto debe ser especialmente eficiente.

Revisa índices.

---

# 113. ÍNDICES DE VENTAS

Si se requieren nuevos índices, justificar según consultas reales.

Probables criterios:

```text
Empresa
Fecha
Cliente
NumeroDocumento
ClaveAcceso
Estado
PuntoEmision
```

No agregarlos indiscriminadamente.

---

# 114. CONSTRAINTS

Utilizar restricciones de base cuando refuercen invariantes:

```text
clave acceso única;
secuencial único en su ámbito;
cantidades válidas;
```

solo cuando no existan ya.

---

# 115. MIGRACIONES

Si necesitas modificaciones:

1. no alteres migraciones aplicadas;
2. genera una nueva;
3. inspecciona SQL;
4. comprueba datos existentes;
5. evita eliminaciones destructivas.

---

# 116. INTEGRIDAD MULTIEMPRESA

Audita específicamente que:

```text
Cliente
Producto
Bodega
Punto emisión
Secuencial
Venta
Comprobante
```

pertenezcan al ámbito correcto.

La UI no es frontera de seguridad.

---

# 117. DOCUMENTACIÓN

Al finalizar crear o actualizar:

```text
/docs/VENTAS_V1.md
```

Debe documentar lo que REALMENTE quedó implementado.

Incluir:

1. Objetivo.
2. Arquitectura.
3. Entidades/tablas.
4. Ciclo de vida.
5. Clientes.
6. Productos.
7. Presentaciones.
8. Precios.
9. Descuentos.
10. Impuestos.
11. Lotes.
12. Series.
13. Pagos.
14. Inventario.
15. Facturación electrónica.
16. Contabilidad.
17. Estados.
18. Idempotencia.
19. Concurrencia.
20. Multiempresa.
21. UI.
22. Permisos.
23. Auditoría.
24. Pruebas.
25. Pendientes reales.

Usa diagramas Mermaid donde aporten valor.

---

# 118. FASES DE IMPLEMENTACIÓN

Trabaja incrementalmente.

## FASE 1 — Auditoría del repositorio

Determina:

```text
qué existe;
qué reutilizar;
qué falta;
qué NO debemos duplicar.
```

Continúa automáticamente.

No pidas confirmación salvo que exista una decisión imposible de inferir.

---

## FASE 2 — Dominio / modelo Venta

Completa únicamente lo necesario.

Pruebas.

```powershell
dotnet build
dotnet test
```

---

## FASE 3 — Motor de cálculo

Implementa/reutiliza:

```text
precios
descuentos
impuestos
totales
```

Pruebas.

---

## FASE 4 — Integración Clientes / Productos

Búsquedas y selección.

Pruebas.

---

## FASE 5 — Inventario

Integra:

```text
stock
presentaciones
lotes
series
salida
costo
```

Pruebas.

---

## FASE 6 — Pagos

Implementar según infraestructura actual.

Pruebas.

---

## FASE 7 — Confirmación / persistencia

Implementar:

```text
transacción local
idempotencia
concurrencia
estados
```

Pruebas.

---

## FASE 8 — Facturación electrónica

Mapeo Venta → Factura.

Integrar motor existente.

Pruebas simuladas.

---

## FASE 9 — Contabilidad

Conectar exclusivamente con infraestructura existente.

No duplicar.

Pruebas.

---

## FASE 10 — Listado Ventas

Implementar:

```text
KPI
búsqueda
filtros
paginación
detalle
```

---

## FASE 11 — Nueva Venta

Construir UI POS/venta.

Optimizar para velocidad de uso.

---

## FASE 12 — Recuperación SRI

Integrar pendientes/reconciliación.

---

## FASE 13 — Prueba integral

Ejecutar flujo completo.

---

## FASE 14 — Auditoría final

Revisar:

```text
Integridad
Stock
Lotes
Series
Precios
Impuestos
SRI
Contabilidad
Idempotencia
Concurrencia
Multiempresa
Permisos
Auditoría
Performance
UI
```

---

# 119. BUILD / TEST CONSTANTE

Después de cada fase:

```powershell
dotnet build
dotnet test
```

No acumules cambios durante muchas fases sin compilar.

---

# 120. CRITERIOS DE ACEPTACIÓN

VENTAS V1 estará terminado cuando:

* [ ] Codex exploró primero el repositorio actual.
* [ ] No se duplicaron servicios/entidades existentes.
* [ ] Existe listado de ventas.
* [ ] Existe Nueva Venta.
* [ ] Cliente funciona.
* [ ] Consumidor final funciona.
* [ ] Productos funcionan.
* [ ] Servicios funcionan.
* [ ] Código de barras funciona.
* [ ] Presentaciones funcionan.
* [ ] Factores funcionan.
* [ ] Precios funcionan.
* [ ] Lista del cliente funciona cuando corresponda.
* [ ] Descuentos funcionan.
* [ ] IVA/impuestos funcionan.
* [ ] Totales son consistentes con XML SRI.
* [ ] Stock se consulta correctamente.
* [ ] La confirmación usa Inventario V1.
* [ ] Una venta genera una única salida.
* [ ] Lotes funcionan.
* [ ] Series funcionan.
* [ ] Pagos funcionan.
* [ ] Pago mixto funciona si fue incluido.
* [ ] Punto emisión correcto.
* [ ] Secuencial atómico.
* [ ] Venta → Factura electrónica funciona.
* [ ] Factura se firma usando el motor existente.
* [ ] Recepción SRI funciona.
* [ ] Autorización SRI funciona.
* [ ] Se gestionan pendientes.
* [ ] Se preservan mensajes SRI.
* [ ] La venta puede recuperarse después de fallos.
* [ ] No se duplica stock al reintentar.
* [ ] No se duplica comprobante al reintentar.
* [ ] Multiempresa está protegida.
* [ ] Contabilidad no se duplica.
* [ ] Permisos funcionan.
* [ ] Auditoría funciona.
* [ ] Listados son paginados.
* [ ] UI funciona Light/Dark.
* [ ] UX es adecuada para mostrador.
* [ ] `dotnet build` correcto.
* [ ] `dotnet test` sin regresiones.
* [ ] Existe `/docs/VENTAS_V1.md`.

---

# 121. REVISIÓN CRÍTICA FINAL

Antes de declarar terminado VENTAS V1, comprueba específicamente:

### DUPLICIDAD

```text
¿Un doble clic puede crear dos ventas?
```

Debe ser:

```text
NO
```

### INVENTARIO

```text
¿Existe alguna ruta donde Ventas cambie stock directamente?
```

Debe ser:

```text
NO
```

### PRESENTACIONES

```text
¿Cantidad comercial y cantidad base siempre cuadran?
```

### LOTES

```text
¿Puede venderse stock de un lote inexistente?
```

Debe ser:

```text
NO
```

### SERIES

```text
¿Puede una serie venderse dos veces?
```

Debe ser:

```text
NO
```

### SECUENCIAL

```text
¿Dos terminales pueden recibir el mismo secuencial?
```

Debe ser:

```text
NO
```

### SRI

```text
¿Un timeout puede provocar que reenviemos ciegamente una factura ya recibida?
```

Debe ser:

```text
NO
```

### MULTIEMPRESA

```text
¿Una venta puede utilizar una bodega, punto de emisión o certificado de otra empresa?
```

Debe ser:

```text
NO
```

### HISTÓRICO

```text
¿Cambiar un producto hoy altera una venta de ayer?
```

Debe ser:

```text
NO
```

### CÁLCULOS

```text
¿Pantalla, persistencia y XML utilizan reglas diferentes?
```

Debe ser:

```text
NO
```

### CONTABILIDAD

```text
¿Se genera dos veces el mismo efecto contable?
```

Debe ser:

```text
NO
```

Corrige cualquier problema antes de cerrar el módulo.

---

# 122. INFORME FINAL DE CODEX

Al terminar entrega:

## Estado

```text
VENTAS V1
COMPLETADO / PARCIAL
```

## Exploración inicial

Describe qué piezas existentes encontraste y reutilizaste.

Especialmente:

```text
Productos
Clientes
Inventario
Facturación electrónica
Puntos de emisión
Contabilidad
Seguridad
Auditoría
```

## Arquitectura resultante

Explica cómo quedó el flujo de Venta.

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

Reporta únicamente cambios reales:

```text
tablas
columnas
índices
constraints
migraciones
```

## Integraciones

Describe expresamente:

```text
Clientes
Productos
Inventario/Kardex
Facturación Electrónica
Contabilidad
Operaciones sin comprobante si existe relación
```

## Pruebas

Indicar:

```text
pruebas nuevas;
dotnet build;
dotnet test;
resultado.
```

## Flujo SRI

Confirmar:

```text
Venta
→ XML
→ Firma
→ Recepción
→ Autorización
```

## Concurrencia

Indicar cómo se protege:

```text
stock;
secuenciales;
confirmación.
```

## Idempotencia

Indicar cómo se evita:

```text
doble venta;
doble salida;
doble comprobante.
```

## Pendientes

Solo pendientes reales y justificados.

---

# 123. REGLA FINAL

VENTAS es un módulo orquestador.

No debe reemplazar los motores que ya construimos.

Debe unir correctamente:

```text
CLIENTE
+
PRODUCTO
+
PRECIO
+
INVENTARIO
+
PAGO
+
CONTABILIDAD
+
FACTURACIÓN ELECTRÓNICA
```

y convertirlos en una operación comercial:

```text
TRAZABLE
CONSISTENTE
RECUPERABLE
AUDITABLE
```

Recuerda siempre:

> **Antes de modificar cualquier parte, comprende el estado ACTUAL del proyecto.**

No hagas retroceder decisiones arquitectónicas que hayan sido mejoradas desde las primeras versiones.

No construyas lo que ya existe.

No sacrifiques integridad por rapidez.

Y mantén la filosofía de KONTAXPRO:

> # ROBUSTEZ POR DENTRO, SIMPLICIDAD POR FUERA.
