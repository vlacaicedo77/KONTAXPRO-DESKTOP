PROMPT — Auditoría y mejora definitiva de “Registrar ajuste de inventario”
Lotes múltiples, series múltiples, autocomplete inteligente y reutilización del diseño del formulario principal

Lee AGENTS.md completo antes de modificar código.

CONTEXTO

La ventana “Registrar ajuste de inventario” ya está construida, pero el manejo actual de LOTES y SERIES no quedó conforme a las reglas funcionales y visuales aprobadas.

Existe una implementación mucho más madura en el formulario principal de Producto, específicamente en el flujo de Inventario Inicial.

Esa implementación del formulario principal debe ser la REFERENCIA OBLIGATORIA para:

- estilos;
- tablas;
- filas;
- alturas;
- controles;
- botón Agregar lote;
- botón Agregar serie;
- botón eliminar;
- fechas;
- asociación lote-serie;
- contadores Asignado/Requerido/Pendiente;
- validaciones;
- Light/Dark;
- colores;
- alineación;
- comportamiento de filas.

NO inventes una tercera implementación visual diferente.

IMPORTANTE:

- NO modificar el diseño ni comportamiento aprobado del modo NUEVO de Producto.
- NO rediseñar la ventana completa de Ajustes.
- Modificar exclusivamente lo necesario para que el editor de Ajustes funcione correctamente.
- Si es conveniente, extraer componentes/estilos/lógica reutilizable del formulario principal, pero sin alterar su comportamiento observable.
- No hacer commit ni push.

============================================================
1. OBJETIVO FUNCIONAL
============================================================

Un Ajuste de Inventario representa UNA diferencia física detectada.

Debe poder afectar en una sola operación:

- varios lotes;
- varias series;
- varios lotes con sus respectivas series.

No obligar al usuario a registrar un ajuste separado por cada lote o serie.

La regla definitiva es:

NORMAL

ENTRADA:
- cantidad.

SALIDA:
- cantidad.

LOTE

ENTRADA:
- uno o varios lotes;
- cada lote puede ser existente o nuevo.

SALIDA:
- uno o varios lotes EXISTENTES con stock disponible.

SERIE

ENTRADA:
- una o varias series nuevas.

SALIDA:
- seleccionar una o varias series existentes DISPONIBLES.

LOTE_Y_SERIE

ENTRADA:
- uno o varios lotes;
- series nuevas asociadas a esos lotes.

SALIDA:
- NO seleccionar lote manualmente;
- seleccionar únicamente series existentes;
- KONTAXPRO deriva automáticamente los lotes desde producto_serie.producto_lote_id.

Esta matriz queda como regla definitiva.

============================================================
2. PRIMERO AUDITAR EL FORMULARIO PRINCIPAL
============================================================

Antes de modificar Ajustes, inspecciona:

- ProductFormView.xaml
- ProductFormViewModel
- componentes relacionados con Inventario Inicial;
- modelos temporales de lotes;
- modelos temporales de series;
- converters;
- behaviors;
- estilos DataGrid/ListView;
- controles de fecha;
- contadores;
- comandos Agregar/Quitar;
- validaciones LOTE_Y_SERIE.

Identifica qué puede reutilizarse.

No copies código innecesariamente si puede extraerse una abstracción compartida sin romper el modo NUEVO.

============================================================
3. CORREGIR EL PROBLEMA VISUAL ACTUAL
============================================================

Actualmente las filas de LOTES DEL AJUSTE quedan cortadas.

En la captura:

- el selector/campo del lote no se ve completo;
- la altura de fila es insuficiente;
- aparecen controles parcialmente ocultos;
- existe clipping;
- la tabla tiene una apariencia distinta a la del formulario principal.

Corregir:

- RowHeight;
- MinHeight;
- Padding;
- VerticalContentAlignment;
- tamaño de TextBox/ComboBox;
- altura del DataGrid;
- ScrollViewer interno;
- márgenes;
- clipping;
- alineación.

Cada fila debe visualizarse completa.

Usar como referencia exacta la tabla de lotes del formulario principal.

============================================================
4. CANTIDAD MAESTRA DEL AJUSTE
============================================================

Mantener:

Presentación
Cantidad presentación

Calcular:

cantidad_base =
cantidad_presentacion × factor_conversion

Ejemplo:

CAJA X6
Cantidad presentación = 2

cantidad_base = 12

Esta cantidad base es la cantidad REQUERIDA que debe reconciliarse contra lotes o series.

============================================================
5. CONTADORES
============================================================

Usar el mismo patrón del formulario principal.

Para lotes:

Asignado: X
Requerido: Y
Pendiente: Z

Para series:

Registradas/Seleccionadas: X
Requeridas: Y
Pendientes: Z

No permitir Registrar ajuste cuando:

Pendiente != 0

Usar los mismos colores/estilos ya aprobados.

============================================================
6. ENTRADA — CONTROL NORMAL
============================================================

No mostrar:

- lotes;
- series.

Solo:

- Bodega
- Presentación
- Cantidad
- Costo presentación
- Motivo
- Observación

============================================================
7. ENTRADA — CONTROL LOTE
============================================================

Mostrar:

LOTES DEL AJUSTE

[ + AGREGAR LOTE ]

Tabla basada visualmente en la tabla del formulario principal.

Columnas:

LOTE
CANTIDAD BASE
ELABORACIÓN
CADUCIDAD
ACCIONES

Debe permitir múltiples filas.

Ejemplo:

Cantidad base requerida = 10

LT510     6
LT620     4

Asignado: 10
Requerido: 10
Pendiente: 0

============================================================
8. LOTE EN ENTRADA NO DEBE SER COMBOBOX CERRADO
============================================================

Este es un requisito CRÍTICO.

En ENTRADA puede ocurrir:

A. El lote ya existe.
B. El lote es completamente nuevo.

Por tanto, el usuario debe poder ESCRIBIR el lote.

Implementar un control:

TextBox/autocomplete editable

con comportamiento similar a buscador de Google.

Mientras escribe, mostrar coincidencias de lotes existentes.

El usuario NO debe estar obligado a escoger una sugerencia.

Si no existe coincidencia, puede continuar escribiendo un lote nuevo.

============================================================
9. BÚSQUEDA FLEXIBLE DE LOTES
============================================================

La búsqueda debe funcionar por coincidencia parcial y normalizada.

REQUISITO EXPRESO:

Si existe:

LT510

y el usuario escribe:

510

DEBE aparecer:

LT510

como sugerencia.

También deberían encontrarlo con:

LT
LT5
LT510
lt510
LT 510
LT-510

La búsqueda debe ser case-insensitive.

Para BÚSQUEDA puede generarse una representación normalizada temporal, por ejemplo:

- mayúsculas;
- eliminar espacios;
- eliminar guiones/separadores comunes para buscar;
- comparar mediante Contains.

Ejemplos:

Lote almacenado:
LT510

Clave búsqueda:
LT510

Consulta:
510

Resultado:
LT510

Lote almacenado:
ABC-2026-510

Consulta:
510

También debe poder aparecer como resultado.

IMPORTANTE:

Esta normalización es SOLO para buscar/sugerir.

NO modificar automáticamente el número real almacenado del lote.

============================================================
10. ORDEN DE LOS RESULTADOS DE BÚSQUEDA
============================================================

Priorizar resultados:

1. Coincidencia exacta.
2. Coincidencia exacta normalizada.
3. Comienza con el texto buscado.
4. Contiene el texto buscado.
5. Coincidencia flexible ignorando espacios/guiones.

Ejemplo:

Consulta:
510

Priorizar:

510
LT510
LT-510
ABC-510
ABC-2026-510

según relevancia.

No es necesario implementar un algoritmo complejo de fuzzy search.

============================================================
11. INFORMACIÓN DE LA SUGERENCIA
============================================================

Cada sugerencia debe mostrar algo más que el código.

Ejemplo:

LT510
Stock en FAC: 18
Elaboración: 01/02/2026
Caducidad: 01/02/2028

Otra:

LT620
Stock en FAC: 6
Caducidad: 15/04/2028

La información es referencial.

Filtrar lotes:

- del producto actual;
- de la empresa correcta.

El stock mostrado debe corresponder preferentemente a la bodega seleccionada.

============================================================
12. LOTE EXISTENTE EN ENTRADA
============================================================

Si el usuario selecciona una sugerencia:

- conservar producto_lote_id;
- marcar internamente que es EXISTENTE;
- reutilizar productos_lotes;
- NO crear otro lote;
- mostrar sus datos conocidos;
- cargar elaboración;
- cargar caducidad;
- mostrar stock actual de la bodega.

Las fechas de un lote existente deben ser solo lectura en Ajustes.

Si están equivocadas:

usar:

CORREGIR LOTES / SERIES

No modificar datos históricos del lote desde Ajuste.

============================================================
13. LOTE NUEVO EN ENTRADA
============================================================

Si el texto ingresado no corresponde a un lote existente:

mostrar claramente:

NUEVO LOTE

Habilitar en esa fila:

- Número de lote
- Cantidad base
- Elaboración
- Caducidad
- Eliminar

Elaboración:

- opcional;
- formato DD/MM/AAAA.

Caducidad:

- obligatoria solamente si el producto controla caducidad.

Si ambas están informadas:

fecha_elaboracion <= fecha_caducidad

============================================================
14. DETECCIÓN DE POSIBLES DUPLICADOS
============================================================

Caso:

Ya existe:

LT510

Otro usuario escribe:

LT 510

o:

LT-510

No crear silenciosamente otro lote.

Diferenciar:

A. COINCIDENCIA EXACTA NORMALIZADA BÁSICA

Usar:

Trim
ToUpperInvariant

Si coincide:

reutilizar existente.

B. COINCIDENCIA PROBABLE

Para detectar posibles duplicados pueden ignorarse en una comparación auxiliar:

- espacios;
- guiones;
- separadores simples.

Ejemplo:

LT510
LT 510
LT-510

Mostrar advertencia:

“Existe un lote posiblemente equivalente: LT510.”

Acciones:

[ USAR LT510 ]
[ CREAR NUEVO DE TODAS FORMAS ]

No fusionar automáticamente.

No modificar el código real almacenado.

============================================================
15. ENTRADA MULTILOTE
============================================================

Debe permitir mezclar lotes existentes y nuevos.

Ejemplo real:

Cantidad base requerida = 8

LT510
EXISTENTE
Cantidad = 5

LT620
NUEVO
Cantidad = 3

Resultado:

Asignado = 8
Requerido = 8
Pendiente = 0

Un solo AJUSTE_ENTRADA.

============================================================
16. ENTRADA — CONTROL SERIE
============================================================

No mostrar lotes.

Mostrar:

SERIES NUEVAS

[ + AGREGAR SERIE ]

Usar la misma tabla/estilo del formulario principal.

Cada fila:

SERIE
ACCIONES

Permitir múltiples series.

Validar:

cantidad series =
cantidad_base requerida

No permitir:

- series duplicadas dentro de la captura;
- series ya existentes en BD;
- series vacías;
- serie repetida ignorando Trim/mayúsculas cuando corresponda.

============================================================
17. ENTRADA — LOTE_Y_SERIE
============================================================

Mostrar dos secciones:

A. LOTES DEL AJUSTE

B. SERIES NUEVAS

LOTES:

Mismo comportamiento de autocomplete:

- lote existente;
- lote nuevo;
- múltiples lotes.

SERIES:

Tabla:

SERIE
LOTE ASOCIADO
ACCIONES

El selector LOTE ASOCIADO debe mostrar ÚNICAMENTE los lotes incluidos en el ajuste actual.

Ejemplo:

Cantidad = 5

LT510 = 3
LT620 = 2

Series:

SN001 → LT510
SN002 → LT510
SN003 → LT510
SN004 → LT620
SN005 → LT620

Validar:

SUM(lotes) = 5

total series = 5

series LT510 = 3

series LT620 = 2

============================================================
18. SALIDA — PRINCIPIO GENERAL
============================================================

En SALIDA:

todo debe corresponder a existencia real.

Nunca permitir:

- lote nuevo;
- serie nueva;
- stock negativo;
- lote sin stock;
- serie vendida;
- serie reservada;
- serie dada de baja.

============================================================
19. SALIDA — CONTROL NORMAL
============================================================

No mostrar:

- lotes;
- series.

Validar:

cantidad_base <= stock_disponible_bodega

============================================================
20. SALIDA — CONTROL LOTE
============================================================

Aquí SÍ se seleccionan lotes existentes.

Debe permitir múltiples lotes.

Mostrar:

LOTES DE SALIDA

[ + AGREGAR LOTE ]

Tabla:

LOTE
DISPONIBLE
CANTIDAD BASE
ELABORACIÓN
CADUCIDAD
ACCIONES

Selector de lote:

solo lotes:

- del producto;
- existentes;
- con stock > 0;
- en la bodega seleccionada.

Ejemplo:

Cantidad requerida = 7

LT510
Disponible 10
Salida 4

LT620
Disponible 6
Salida 3

Asignado 7
Requerido 7
Pendiente 0

No permitir:

cantidad lote > disponible lote

============================================================
21. EVITAR LOTE DUPLICADO EN SALIDA
============================================================

Si LT510 ya está agregado a la distribución de salida:

no permitir agregar LT510 en otra fila.

Debe existir una sola fila por producto_lote_id dentro del mismo ajuste.

============================================================
22. SALIDA — CONTROL SERIE
============================================================

No mostrar lotes.

Mostrar:

SERIES DISPONIBLES

Usar selección múltiple.

Cada fila:

Checkbox
Serie
Ubicación opcional

Mostrar únicamente:

- producto actual;
- bodega seleccionada;
- estado DISPONIBLE;
- no reservada.

El usuario debe seleccionar exactamente:

cantidad_base

series.

============================================================
23. SALIDA — LOTE_Y_SERIE
============================================================

REGLA DEFINITIVA Y MUY IMPORTANTE:

NO mostrar selector de lote.

NO solicitar cantidad manual por lote.

NO mostrar la sección LOTES DE SALIDA como entrada editable.

La serie ya conoce su lote mediante:

productos_series.producto_lote_id

Por tanto mostrar directamente:

SERIES DISPONIBLES

Cada fila debe mostrar:

[ ] SERIE
LOTE
UBICACIÓN opcional

Ejemplo:

[ ] JCT-PJB16-260001 · LOTE PJB-2608-A
[ ] JCT-PJB16-260002 · LOTE PJB-2608-A
[ ] JCT-PJB16-260003 · LOTE PJB-2608-B
[ ] JCT-PJB16-260004 · LOTE PJB-2608-A

Permitir seleccionar series pertenecientes a distintos lotes dentro del mismo ajuste.

============================================================
24. DERIVAR LOTES DESDE SERIES
============================================================

En SALIDA LOTE_Y_SERIE:

si selecciona:

SN001 → Lote A
SN002 → Lote A
SN003 → Lote B

KONTAXPRO debe calcular:

Cantidad total seleccionada = 3

Distribución:

Lote A = 2
Lote B = 1

No solicitar esa información otra vez.

============================================================
25. RESUMEN EN SALIDA LOTE_Y_SERIE
============================================================

Mostrar debajo:

Seleccionadas: 3
Requeridas: 3
Pendientes: 0

Distribución automática:

PJB-2608-A → 2
PJB-2608-B → 1

Usar estilo informativo similar al formulario principal.

============================================================
26. GENERACIÓN DE DETALLES EN SALIDA LOTE_Y_SERIE
============================================================

Aunque el usuario solo seleccione series, persistir correctamente:

movimientos_inventario_detalles

y agrupar automáticamente por producto_lote_id para crear:

movimientos_inventario_detalles_lotes

y registrar individualmente:

movimientos_inventario_detalles_series

Ejemplo:

Series seleccionadas:

S1 → L1
S2 → L1
S3 → L2

Crear:

Detalle lote L1 cantidad 2
Detalle lote L2 cantidad 1

Detalle serie S1
Detalle serie S2
Detalle serie S3

============================================================
27. PRESENTACIONES CON FACTOR > 1 Y SERIES
============================================================

Ejemplo:

Presentación:
CAJA X2

Cantidad:
2

Factor:
2

Cantidad base:
4

Debe registrar/seleccionar:

4 series

No 2.

Para productos serializados:

cantidad_base debe resultar entera.

Si cantidad_base es fraccionaria:

bloquear la operación.

============================================================
28. COSTO — ENTRADA
============================================================

Mantener:

Costo presentación

editable.

Mostrar referencia:

1. costo conocido del lote seleccionado, cuando pueda determinarse;
2. último costo efectivo;
3. costo promedio.

Si hay varios lotes en la misma entrada:

el costo corresponde a la presentación/operación completa.

No asignar un costo contable independiente por lote.

Formato visual:

mínimo 2 decimales
máximo 6.

============================================================
29. COSTO — SALIDA
============================================================

No permitir costo manual.

Mostrar:

Costo aplicado

solo lectura.

Calcular desde la política de valoración vigente:

costo promedio base × factor de presentación

No usar un costo diferente por lote.

============================================================
30. COMPORTAMIENTO DE CERO
============================================================

Mantener:

Cantidad = 0
Costo = 0

Al ganar foco:

si valor es cero → dejar vacío.

Al perder foco vacío:

Cantidad → 0
Costo → 0,00

============================================================
31. CAMBIO DE BODEGA
============================================================

Si cambia Bodega:

- refrescar lotes;
- refrescar stock;
- refrescar series;
- limpiar selecciones incompatibles;
- recalcular disponibles;
- advertir si existen datos temporales que se perderán.

Nunca conservar series/lotes de otra bodega.

============================================================
32. CAMBIO DE PRESENTACIÓN
============================================================

Si cambia Presentación:

- recalcular factor;
- recalcular cantidad base;
- volver a validar lotes;
- volver a validar series;
- limpiar información incompatible cuando corresponda.

============================================================
33. CAMBIO ENTRADA / SALIDA
============================================================

Si cambia:

ENTRADA → SALIDA

o:

SALIDA → ENTRADA

limpiar correctamente:

- lotes temporales;
- nuevos lotes;
- series temporales;
- selecciones;
- datos derivados.

Si ya ingresó información:

mostrar confirmación antes de perderla.

============================================================
34. FOOTER
============================================================

Mantener siempre fijo:

Cancelar
Registrar ajuste

No incluir estos botones dentro del ScrollViewer.

============================================================
35. ESTILOS
============================================================

Usar exactamente la línea visual KONTAXPRO existente:

- encabezados negrita;
- fondo acorde al tema;
- selección verde;
- hover;
- botones verdes;
- papelera roja;
- iconos;
- vertical center;
- bordes;
- scrollbars;
- Light/Dark.

No crear controles WPF visualmente estándar/blancos.

============================================================
36. ATOMICIDAD
============================================================

Registrar ajuste debe continuar siendo transaccional.

BEGIN

ajustes_inventario
ajustes_inventario_detalles

movimientos_inventario
movimientos_inventario_detalles

si aplica:
    crear/reutilizar productos_lotes
    actualizar productos_lotes_existencias
    crear movimientos_inventario_detalles_lotes

si aplica:
    crear/actualizar productos_series
    crear movimientos_inventario_detalles_series

actualizar productos_existencias

actualizar productos_costos cuando corresponda

COMMIT

Cualquier error:

ROLLBACK

============================================================
37. PRUEBAS OBLIGATORIAS
============================================================

Prueba 1
ENTRADA NORMAL.

Prueba 2
SALIDA NORMAL.

Prueba 3
ENTRADA LOTE:
existente LT510.

Prueba 4
Buscar:
510

Debe sugerir:
LT510.

Prueba 5
Buscar:
LT5

Debe sugerir:
LT510.

Prueba 6
Buscar:
lt510

Debe sugerir:
LT510.

Prueba 7
Buscar:
LT 510

Debe detectar posible equivalencia con LT510.

Prueba 8
ENTRADA LOTE:
LT510 existente + LT620 nuevo.

Prueba 9
ENTRADA LOTE:
dos lotes nuevos.

Prueba 10
SALIDA LOTE:
dos lotes existentes.

Prueba 11
ENTRADA SERIE:
cinco series nuevas.

Prueba 12
SALIDA SERIE:
cinco series existentes.

Prueba 13
ENTRADA LOTE_Y_SERIE:
dos lotes + cinco series.

Prueba 14
SALIDA LOTE_Y_SERIE:
seleccionar series de dos lotes SIN seleccionar lote manualmente.

Prueba 15
Verificar distribución automática por lote.

Prueba 16
Menos series que cantidad requerida.

Debe bloquear.

Prueba 17
Más series que cantidad requerida.

Debe bloquear.

Prueba 18
Lote de salida sin stock.

No debe aparecer.

Prueba 19
Serie no disponible.

No debe aparecer.

Prueba 20
Cambiar Bodega con datos ingresados.

Prueba 21
Cambiar Presentación con datos ingresados.

Prueba 22
Cambiar ENTRADA ↔ SALIDA.

Prueba 23
Rollback provocado.

============================================================
38. VERIFICACIÓN EN BASE
============================================================

Revisar:

ajustes_inventario
ajustes_inventario_detalles

movimientos_inventario
movimientos_inventario_detalles

movimientos_inventario_detalles_lotes
movimientos_inventario_detalles_series

productos_existencias
productos_costos

productos_lotes
productos_lotes_existencias

productos_series

Validar:

SUM(detalles lotes)
=
cantidad_base del ajuste

series
=
cantidad_base para serializados

series agrupadas por lote
=
cantidades en movimientos_inventario_detalles_lotes

stock producto/bodega correcto

stock lote/bodega correcto

sin lotes duplicados accidentales

sin series duplicadas

sin stock negativo

============================================================
39. VALIDACIÓN TÉCNICA
============================================================

Ejecutar:

dotnet restore
dotnet build KONTAXPRO.slnx
dotnet test
git diff --check

Verificar:

- 0 errores;
- 0 advertencias si es posible;
- EF Core sin cambios pendientes;
- no crear migración si no hay cambio estructural real.

============================================================
40. INFORME FINAL
============================================================

Informar:

1. Qué reutilizaste del formulario principal.
2. Cómo corregiste las filas cortadas.
3. Cómo funciona ENTRADA LOTE.
4. Cómo funciona búsqueda/autocomplete.
5. Confirmar explícitamente que escribir “510” encuentra “LT510”.
6. Cómo diferencias lote existente y nuevo.
7. Cómo previenes duplicados.
8. Cómo funciona entrada multilote.
9. Cómo funciona salida multilote.
10. Cómo funciona SERIE ENTRADA.
11. Cómo funciona SERIE SALIDA.
12. Cómo funciona LOTE_Y_SERIE ENTRADA.
13. Cómo funciona LOTE_Y_SERIE SALIDA sin selector manual de lote.
14. Cómo se deriva automáticamente el lote desde la serie.
15. Cómo se valida contra cantidad base.
16. Cómo quedó costo ENTRADA/SALIDA.
17. Resultado Light/Dark.
18. Tests.
19. Build.
20. EF/migraciones.

Indicar expresamente:

“El modo NUEVO no fue modificado en diseño ni funcionalidad.”

No hacer commit ni push.