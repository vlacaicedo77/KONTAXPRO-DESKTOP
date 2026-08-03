PROMPT — Cierre formal de PRODUCTOS V1, documentación permanente del módulo y definición de patrones base para futuros módulos

Lee AGENTS.md completo antes de realizar cualquier acción.

CONTEXTO

La primera versión funcional completa del módulo PRODUCTOS de KONTAXPRO Desktop ha sido terminada.

Este módulo ha servido para definir y consolidar gran parte de:

- arquitectura;
- reglas funcionales;
- interacción MVVM;
- servicios;
- transacciones;
- Inventario;
- presentaciones;
- precios;
- costos;
- lotes;
- series;
- Kardex;
- operaciones auxiliares;
- mensajes;
- tablas;
- formularios;
- Light/Dark;
- look & feel;
- búsqueda;
- filtros;
- KPIs;
- paginación;
- estilos reutilizables.

A partir de ahora PRODUCTOS V1 será una REFERENCIA FUNCIONAL, VISUAL Y TÉCNICA para construir otros módulos de KONTAXPRO.

IMPORTANTE:

Esta tarea es fundamentalmente de:

AUDITORÍA
+
DOCUMENTACIÓN
+
MEMORIA DEL PROYECTO

NO es una tarea de rediseño.

NO modifiques comportamiento funcional de Productos.
NO cambies XAML.
NO cambies ViewModels.
NO cambies servicios.
NO cambies entidades.
NO cambies base de datos.
NO regeneres migraciones.
NO ejecutes reset de Development.
NO hagas refactors generales.
NO elimines código.
NO hagas commit ni push.

Solo pueden modificarse:

- archivos .md nuevos;
- AGENTS.md.

Si durante la auditoría encuentras un problema real de código:

NO lo corrijas en esta tarea.

Documéntalo en el informe final como pendiente.

============================================================
1. FUENTE DE VERDAD
============================================================

La fuente de verdad para esta documentación es:

EL CÓDIGO ACTUAL DEL REPOSITORIO.

No documentes simplemente lo que indiquen prompts anteriores.

Antes de escribir documentación, inspecciona la implementación real.

Revisar como mínimo:

- KONTAXPRO.Desktop
- KONTAXPRO.Application
- KONTAXPRO.Domain
- KONTAXPRO.Infrastructure

y específicamente todo lo relacionado con Productos e Inventario utilizado por Productos.

Identificar nombres REALES de:

- Views;
- ViewModels;
- Services;
- Interfaces;
- DTOs;
- Requests;
- Entities;
- EF configurations;
- converters;
- controls;
- styles;
- dialogs;
- message services;
- tests;
- seeders;
- migrations.

NO inventar nombres de clases o rutas.

============================================================
2. OBJETIVO DE LA DOCUMENTACIÓN
============================================================

La documentación debe permitir que una futura sesión de Codex pueda leer:

AGENTS.md
+
docs/

y comprender:

1. Cómo funciona PRODUCTOS V1.
2. Qué reglas ya están aprobadas.
3. Qué no debe romperse.
4. Qué patrones de UI deben reutilizarse.
5. Qué patrones técnicos deben reutilizarse.
6. Qué elementos son específicos de Productos y NO deben copiarse ciegamente.
7. Cómo construir nuevos módulos manteniendo coherencia.
8. Qué decisiones arquitectónicas ya están cerradas.

============================================================
3. CREAR CARPETA docs SI NO EXISTE
============================================================

Si:

docs/

no existe:

crearla.

No mover documentación existente arbitrariamente.

Si ya existe documentación:

conservarla.

============================================================
4. CREAR docs/README.md
============================================================

Crear:

docs/README.md

Debe ser un índice corto de la documentación del proyecto.

Incluir como mínimo:

# Documentación KONTAXPRO Desktop

## Documentos principales

- PRODUCTOS_V1.md
- UI_UX_KONTAXPRO.md
- PATRONES_DESARROLLO_KONTAXPRO.md

Explicar en una o dos líneas para qué sirve cada uno.

Agregar una regla:

Antes de modificar un módulo existente, consultar primero su documentación específica.

Antes de construir UI nueva, consultar UI_UX_KONTAXPRO.md.

Antes de crear arquitectura nueva de módulos, consultar PATRONES_DESARROLLO_KONTAXPRO.md.

No convertir README en documentación duplicada.

============================================================
5. CREAR docs/PRODUCTOS_V1.md
============================================================

Este documento debe ser la memoria funcional y técnica completa de PRODUCTOS V1.

Título:

# PRODUCTOS V1 — KONTAXPRO Desktop

Debe indicar claramente:

ESTADO:
FINALIZADO COMO PRIMERA VERSIÓN FUNCIONAL

y:

PRODUCTOS V1 queda congelado como referencia.
Los cambios posteriores deben responder a requerimientos concretos o integración con otros módulos, no a rediseños arbitrarios.

============================================================
6. PRODUCTOS_V1 — RESUMEN DEL MÓDULO
============================================================

Documentar:

- propósito;
- alcance;
- responsabilidades;
- qué problemas resuelve;
- relación Producto/Inventario;
- límites del módulo.

Explicar especialmente:

Producto define información comercial y comportamiento.

Inventario registra hechos físicos.

No confundir ambas responsabilidades.

============================================================
7. PRODUCTOS_V1 — ARQUITECTURA REAL
============================================================

Documentar proyectos/capas reales utilizadas.

Ejemplo conceptual:

Desktop
Application
Domain
Infrastructure

Pero utilizar nombres reales.

Crear una tabla:

Componente | Archivo/Ruta | Responsabilidad

Incluir:

Views
ViewModels
Interfaces
Services
DTOs
Entities
Configurations
Tests

No listar archivos irrelevantes.

============================================================
8. PRODUCTOS_V1 — MODELO PRODUCTO
============================================================

Documentar la entidad Producto ACTUAL.

Explicar campos funcionales como:

- empresa;
- categoría;
- marca;
- unidad base;
- código;
- nombre;
- descripción;
- modelo/especificación;
- tipo de producto;
- manejo de inventario;
- lotes;
- series;
- caducidad;
- observación;
- estado;
- UUID si existe;
- timestamps.

Usar nombres reales de propiedades/columnas.

Explicar qué información NO debe estar directamente en Producto:

- stock;
- costo;
- precio;
- código de barras;
- datos físicos de lotes/series;

si eso corresponde al modelo real actual.

============================================================
9. PRODUCTOS_V1 — PRESENTACIONES
============================================================

Documentar completamente:

- presentación BASE;
- presentaciones adicionales;
- factor de conversión;
- unidad base;
- código;
- UUID;
- código de barras;
- presentación activa;
- reglas de compra/venta si continúan existiendo;
- restricciones.

Explicar:

stock siempre se normaliza a unidad base.

No existe stock independiente por presentación.

============================================================
10. PRODUCTOS_V1 — CÓDIGOS DE BARRAS
============================================================

Documentar:

- códigos del fabricante;
- productos sin código del fabricante;
- generación automática KONTAXPRO;
- formato real implementado;
- relación con establecimiento/prefijo si aplica;
- secuencial;
- unicidad;
- momento en que se genera;
- comportamiento de presentación BASE/adicional.

No inventar formato: verificar código actual.

============================================================
11. PRODUCTOS_V1 — IMPUESTOS
============================================================

Documentar cómo Producto se relaciona actualmente con:

productos_impuestos
impuestos
tarifas_impuesto

o entidades equivalentes reales.

No indicar tarifa directa en Producto si ya no existe.

Documentar selección desde ProductForm.

============================================================
12. PRODUCTOS_V1 — PRECIOS
============================================================

Documentar estructura real.

Incluir:

Lista A
Lista B
Lista C

y nombres actuales seed.

Explicar reglas aprobadas:

LISTA A:
- PORCENTAJE_COSTO / margen sobre costo;
- o PRECIO_FIJO.

LISTAS B/C:
- DESCUENTO_PORCENTAJE sobre Lista A;
- o PRECIO_FIJO.

Explicar:

- precios por presentación;
- no por producto genérico;
- cálculo;
- almacenamiento;
- snapshots donde corresponda;
- costo usado como referencia;
- forma en que ProductForm configura precios.

Usar enums/códigos reales.

============================================================
13. PRODUCTOS_V1 — COSTOS
============================================================

Documentar:

- último precio de compra;
- último costo efectivo;
- costo promedio;
- precisión;
- dónde se almacenan;
- cómo se actualizan;
- cómo se presentan en UI.

Explicar claramente:

el costo contable se mantiene normalizado a unidad base.

El costo de una presentación se deriva mediante factor cuando corresponda.

No existe costo permanente independiente por presentación salvo que la implementación real indique otra cosa.

============================================================
14. PRODUCTOS_V1 — EXISTENCIAS
============================================================

Documentar:

- producto;
- bodega;
- stock actual;
- reservado;
- disponible;
- stock mínimo;
- ubicación;
- reglas de modificación.

Regla fundamental:

STOCK ACTUAL NO SE EDITA DIRECTAMENTE.

Las cantidades cambian mediante operaciones trazables.

============================================================
15. PRODUCTOS_V1 — BODEGAS
============================================================

Documentar únicamente lo relevante para Productos:

- selección de bodega;
- facturable/no facturable;
- existencias;
- stock mínimo;
- ubicación;
- impacto en operaciones.

No documentar todo el futuro módulo Bodegas si no existe.

============================================================
16. PRODUCTOS_V1 — TIPOS DE CONTROL
============================================================

Documentar los tipos realmente existentes:

NORMAL
LOTE
SERIE
LOTE_Y_SERIE

Explicar comportamiento de cada uno.

Incluir manejo de:

maneja_lotes
maneja_series
maneja_fecha_caducidad

o propiedades equivalentes.

============================================================
17. PRODUCTOS_V1 — LOTES
============================================================

Documentar:

productos_lotes
productos_lotes_existencias

o nombres reales.

Incluir:

- número de lote;
- elaboración;
- caducidad;
- fechas opcionales/obligatorias;
- stock por bodega;
- reutilización de lote existente;
- lote nuevo;
- prevención de duplicados;
- búsqueda flexible;
- correcciones controladas.

Regla actual:

Elaboración es opcional.

Caducidad solo se exige cuando el producto controla fecha de caducidad.

============================================================
18. PRODUCTOS_V1 — SERIES
============================================================

Documentar:

- producto;
- lote opcional;
- bodega;
- número serie;
- estado;
- ubicación;
- reglas de unicidad;
- entradas;
- salidas;
- relación LOTE_Y_SERIE.

Explicar:

una serie representa una unidad física identificable.

============================================================
19. PRODUCTOS_V1 — NUEVO PRODUCTO
============================================================

Documentar paso a paso el flujo NUEVO actual.

Debe cubrir:

- búsqueda previa;
- posibles duplicados;
- información general;
- clasificación;
- impuesto;
- presentación base;
- presentaciones adicionales;
- configuración de inventario;
- control de caducidad;
- registrar inventario inicial opcional;
- lotes;
- series;
- precios;
- observaciones;
- validación;
- transacción;
- guardar;
- mensajes.

No cambiar comportamiento.

============================================================
20. PRODUCTOS_V1 — INVENTARIO INICIAL
============================================================

Documentar:

qué significa.

Explicar:

NO es editar stock directamente.

Es una entrada real trazable.

Documentar:

- bodega;
- presentación;
- cantidad;
- factor;
- cantidad base;
- costo presentación;
- costo base;
- stock mínimo;
- ubicación;
- lotes;
- series;
- Kardex;
- costo promedio.

============================================================
21. PRODUCTOS_V1 — EDITAR PRODUCTO
============================================================

Documentar claramente qué puede modificarse.

Incluir:

- información general;
- clasificación;
- presentaciones;
- precios;
- configuración de control;
- stock mínimo;
- ubicación;

según implementación real.

Explicar qué NO se modifica directamente:

- movimientos históricos;
- stock actual;
- cantidades confirmadas.

============================================================
22. PRODUCTOS_V1 — OPERACIONES DE INVENTARIO DEL EDITOR
============================================================

Documentar las operaciones auxiliares finales existentes.

Como mínimo, verificar e incluir:

- Agregar entrada inicial / completar inventario inicial;
- Registrar ajuste;
- Ver Kardex;
- Convertir tipo de control;
- Corregir lotes / series;

y cualquier otra operación real presente.

Usar nombres exactos actuales.

============================================================
23. PRODUCTOS_V1 — AJUSTES
============================================================

Documentar:

AJUSTE_ENTRADA
AJUSTE_SALIDA

y comportamiento para:

NORMAL
LOTE
SERIE
LOTE_Y_SERIE.

Incluir reglas finales:

ENTRADA LOTE:
- puede utilizar lote existente o nuevo;
- puede usar múltiples lotes.

SALIDA LOTE:
- lotes existentes con stock.

ENTRADA SERIE:
- series nuevas.

SALIDA SERIE:
- series disponibles.

ENTRADA LOTE_Y_SERIE:
- lotes primero;
- series asociadas a lotes.

SALIDA LOTE_Y_SERIE:
- selección de series;
- lote derivado automáticamente desde la serie.

Verificar que esto corresponda al código real.

============================================================
24. PRODUCTOS_V1 — CATÁLOGO DE MOTIVOS
============================================================

Documentar la implementación final de:

motivos_operacion_inventario

o nombre real.

Incluir:

- motivos globales;
- motivos por empresa;
- filtro por operación;
- botón +;
- permiso;
- snapshot histórico;
- ausencia intencional de opción OTRO.

Explicar decisión:

No existe OTRO para evitar dispersión y preservar calidad de reportes.

============================================================
25. PRODUCTOS_V1 — CONVERSIÓN DE CONTROL
============================================================

Documentar transiciones permitidas actualmente.

Incluir:

- motivo;
- stock existente;
- clasificación de stock;
- lotes;
- series;
- regularización si sigue existiendo;
- validaciones;
- auditoría;
- qué transiciones están bloqueadas.

No inventar mapa.
Extraerlo de código/tests actuales.

============================================================
26. PRODUCTOS_V1 — CORRECCIÓN CONTROLADA
============================================================

Documentar:

- qué datos pueden corregirse;
- lote;
- elaboración;
- caducidad;
- serie;
- motivo;
- permisos;
- auditoría;
- qué cantidades/estados históricos NO pueden modificarse.

============================================================
27. PRODUCTOS_V1 — KARDEX
============================================================

Documentar:

- propósito;
- campos principales;
- costo;
- stock anterior;
- stock nuevo;
- promedio anterior;
- promedio nuevo;
- usuario;
- observación;
- origen/movimiento;
- consulta histórica.

Regla:

Kardex es histórico y no modifica existencias.

============================================================
28. PRODUCTOS_V1 — MOVIMIENTOS
============================================================

Documentar estructura real de:

movimientos_inventario
movimientos_inventario_detalles
detalles_lotes
detalles_series

Explicar principio:

La operación explica POR QUÉ.
El movimiento/Kardex explica CÓMO cambió el stock.

============================================================
29. PRODUCTOS_V1 — TRANSACCIONES
============================================================

Documentar operaciones que requieren transacción atómica.

Explicar patrón:

BEGIN
operación
movimiento
detalles
lotes/series
existencias
costos
COMMIT

Error:
ROLLBACK

Usar implementación real.

============================================================
30. PRODUCTOS_V1 — PRODUCTS VIEW
============================================================

Documentar el listado final actual.

Incluir:

- encabezado;
- Nuevo producto;
- KPIs;
- búsqueda;
- filtro estado;
- refrescar;
- tabla;
- paginación;
- sort;
- acciones.

============================================================
31. PRODUCTOS_V1 — KPIs
============================================================

Documentar definiciones reales actuales:

PRODUCTOS
STOCK BAJO
SIN STOCK
POR CADUCAR

Explicar:

- cálculo;
- interacción clicable;
- filtro;
- nivel producto/bodega;
- lotes por caducar con stock;
- no doble conteo si corresponde.

Usar código actual.

============================================================
32. PRODUCTOS_V1 — BÚSQUEDA
============================================================

Documentar comportamiento definitivo:

usuario escribe
→ tabla se filtra.

NO autocomplete visual tipo Google.

Incluir campos reales buscables:

- código;
- nombre;
- modelo;
- marca;
- categoría;
- presentación;
- barcode;

según implementación.

Incluir debounce/paginación si existen.

============================================================
33. PRODUCTOS_V1 — PAGINACIÓN
============================================================

Documentar:

- server-side o implementación real;
- PageSize;
- opciones 25/50/100;
- total;
- páginas;
- interacción con filtros;
- sort.

============================================================
34. PRODUCTOS_V1 — COLUMNA PRODUCTO
============================================================

Documentar patrón visual definitivo:

Primera línea:

NOMBRE COMERCIAL · MARCA

Ejemplo:

IVERMÍN 100 ML · FARBIOVET

Segunda:

MODELO / ESPECIFICACIÓN

Tercera:

PRESENTACIONES COMERCIALES

Ejemplo:

FRASCO 100 ML · CAJA X12 · CAJA X6

Si más de 3:
documentar comportamiento real +N MÁS si fue implementado.

============================================================
35. PRODUCTOS_V1 — ESTADOS Y ACCIONES
============================================================

Documentar:

ACTIVO:
badge verde sólido.

INACTIVO:
badge sólido según estilo final.

Editar:
azul sólido.

Inactivar:
rojo sólido.

Activar:
verde sólido.

Iconos blancos.

Tooltips.

Confirmaciones mediante sistema global de mensajes.

============================================================
36. PRODUCTOS_V1 — REGLAS QUE NO DEBEN ROMPERSE
============================================================

Crear una sección destacada:

## Reglas invariantes de PRODUCTOS V1

Incluir únicamente reglas confirmadas en código/diseño.

Ejemplos:

- multiempresa siempre filtrada;
- stock no se edita directamente;
- presentación BASE única;
- factor BASE = 1;
- barcode único según regla real;
- costos normalizados;
- precios por presentación;
- lotes/series trazables;
- operaciones históricas no se reescriben;
- correcciones se registran;
- movimientos transaccionales;
- UI no accede directamente a DbContext;
- ViewModels no usan MessageBox nativo;
- etc.

============================================================
37. PRODUCTOS_V1 — TABLAS INVOLUCRADAS
============================================================

Crear tabla de referencia:

Tabla | Propósito | Escritura desde qué operación

Incluir únicamente tablas reales relevantes.

Ejemplos posibles:

productos
productos_presentaciones
productos_impuestos
productos_presentaciones_precios
productos_costos
productos_existencias
productos_lotes
productos_lotes_existencias
productos_series
movimientos_inventario
movimientos_inventario_detalles
...

Usar nombres reales.

============================================================
38. PRODUCTOS_V1 — SERVICIOS
============================================================

Documentar interfaces/servicios reales.

Para cada uno:

- responsabilidad;
- proyecto;
- dependencias;
- patrón IDbContextFactory;
- transacciones si aplica.

No documentar APIs inexistentes.

============================================================
39. PRODUCTOS_V1 — PRUEBAS
============================================================

Documentar suite real de tests relacionada.

No copiar cada test completo.

Crear tabla:

Área | Casos cubiertos

Ejemplo:

Presentaciones
Lotes
Series
Ajustes
Conversión
Kardex
Búsqueda
Paginación
etc.

============================================================
40. PRODUCTOS_V1 — PENDIENTES FUERA DE V1
============================================================

Crear:

## Fuera de alcance de PRODUCTOS V1

Solo incluir funcionalidades conscientemente postergadas.

No inventar backlog.

Ejemplo si corresponde:

- mejoras futuras relacionadas con recetas/prescripciones;
- funcionalidades no necesarias para primera venta;
- optimizaciones futuras.

Separar claramente:

FINALIZADO
vs.
FUTURO

============================================================
41. CREAR docs/UI_UX_KONTAXPRO.md
============================================================

Este documento será la referencia visual global para TODOS los nuevos módulos.

Título:

# UI/UX KONTAXPRO Desktop — Estándar visual

Indicar:

ProductsView/ProductForm fueron el primer módulo donde estos patrones quedaron consolidados.

Nuevos módulos deben seguir estos patrones, adaptándolos al contexto y NO copiando ciegamente XAML.

============================================================
42. UI_UX — PRINCIPIOS
============================================================

Documentar:

- moderno;
- futurista sin exceso;
- limpio;
- amigable;
- fácil;
- intuitivo;
- consistente;
- robustez interna/simplicidad externa.

No escribir marketing.

Traducir estos principios a reglas concretas de interfaz.

============================================================
43. UI_UX — LIGHT/DARK
============================================================

Documentar:

- ThemeService;
- DynamicResource;
- colores semánticos;
- contraste;
- no hardcodear fondos si existe recurso;
- controles deben funcionar en ambos temas.

Referenciar archivos reales de themes/styles.

============================================================
44. UI_UX — PALETA
============================================================

Extraer del código actual los colores reales.

Documentar semántica:

- verde primario;
- azul;
- morado;
- warning;
- danger;
- fondos;
- textos;
- bordes.

NO inventar hex si el código actual usa otros.

============================================================
45. UI_UX — FORMULARIOS
============================================================

Documentar patrón actual:

Header
Contenido scrollable
Footer fijo

y:

- icono;
- título;
- subtítulo;
- botón X;
- cards;
- campos;
- labels;
- ayudas;
- validación;
- footer.

============================================================
46. UI_UX — BOTÓN X
============================================================

Documentar:

- rojo;
- icono Close blanco;
- esquina superior derecha;
- acción segura;
- comparte lógica con Cancelar/Cerrar;
- confirmación si hay cambios.

============================================================
47. UI_UX — BOTONES
============================================================

Documentar estilos reales:

Primary
Secondary
Danger
Add
Edit
Activate
Close/Cancel

Incluir:

- colores;
- iconos;
- hover;
- alto;
- ancho uniforme cuando son pareja;
- ToolTips.

No copiar números si no son globalmente estables; referenciar recursos reales cuando sea mejor.

============================================================
48. UI_UX — CAMPOS
============================================================

Documentar:

- TextBox;
- ComboBox;
- TextArea;
- PasswordBox;
- focus verde;
- bordes;
- radios;
- placeholder;
- texto de ayuda;
- obligatorio "*".

============================================================
49. UI_UX — MAYÚSCULAS
============================================================

Documentar regla global:

Datos comerciales capturados por usuario deben normalizarse a MAYÚSCULAS cuando corresponda.

Ejemplos:

- nombres;
- marcas;
- modelos;
- descripciones breves;
- códigos;
- lotes;
- ubicaciones;
- motivos.

NO transformar datos sensibles a mayúsculas/minúsculas:

- correo;
- contraseña;
- URL;
- token;
- hash;
- claves técnicas;
- etc.

Distinguir:

datos
vs.
textos normales de UI.

Mensajes de UI no se fuerzan a mayúsculas.

============================================================
50. UI_UX — TABLAS
============================================================

Documentar estándar global de DataGrid/ListView:

- header MAYÚSCULA;
- negrita;
- vertical center;
- fondos por tema;
- hover;
- selección verde;
- texto Light/Dark;
- scrollbars;
- alturas;
- acciones;
- iconos blancos sobre fondos sólidos;
- evitar apariencia WPF predeterminada.

Referenciar estilo global real.

============================================================
51. UI_UX — SORT
============================================================

Documentar:

- encabezado KONTAXPRO;
- hover personalizado;
- no usar celeste estándar Windows;
- ASC/DESC;
- iconografía;
- server-side cuando el volumen lo requiera.

============================================================
52. UI_UX — KPIs
============================================================

Documentar patrón:

- cards;
- icono;
- número;
- texto;
- color semántico;
- clic como filtro cuando aplica;
- estado activo visible;
- cursor Hand.

No convertir todos los cards futuros automáticamente en filtros; aplicar cuando tenga sentido.

============================================================
53. UI_UX — BÚSQUEDA
============================================================

Documentar dos patrones distintos.

A. LISTADOS

Como ProductsView:

escribir
→ filtrar tabla

con debounce.

No desplegable Google por defecto.

B. CAMPOS DE REFERENCIA/RELACIÓN

Ejemplo lote existente/nuevo:

TextBox + autocomplete/sugerencias.

No confundir ambos patrones.

Esta distinción es importante.

============================================================
54. UI_UX — PAGINACIÓN
============================================================

Documentar patrón:

- server-side en listados grandes;
- 25 default;
- 25/50/100;
- total;
- página;
- anterior/siguiente;
- página activa verde;
- filtros vuelven a página 1.

============================================================
55. UI_UX — BADGES
============================================================

Documentar:

- colores sólidos;
- texto blanco;
- mayúsculas para estados;
- ACTIVO verde;
- INACTIVO color final;
- warning;
- danger;
- tamaño compacto.

============================================================
56. UI_UX — MENSAJES
============================================================

Documentar sistema global final de:

ERROR
WARNING
INFO
SUCCESS
CONFIRMATION

Incluir:

- dialogs;
- toast/snackbar;
- iconos;
- colores;
- cuándo usar modal;
- cuándo usar notificación;
- prohibición de MessageBox nativo;
- ViewModels llaman servicio semántico.

Usar nombres reales de servicios/clases.

============================================================
57. UI_UX — DIÁLOGOS AUXILIARES
============================================================

Documentar el shell común aplicado a:

- Ajuste;
- Kardex;
- Conversión;
- Corrección;

o equivalentes.

Incluir:

- dimensiones;
- resize;
- header;
- X;
- scroll;
- footer;
- botones.

No asumir que todos los futuros diálogos tendrán exactamente igual tamaño; documentar el principio y recurso común.

============================================================
58. UI_UX — ICONOGRAFÍA
============================================================

Documentar:

MaterialDesign PackIcon.

Reglas:

- icono acorde a acción;
- blanco sobre botón sólido;
- Plus para agregar;
- Close;
- Edit;
- Refresh;
- etc.

No utilizar emojis o caracteres Unicode como sustituto si existe PackIcon apropiado.

============================================================
59. UI_UX — RESPONSIVE EN WPF
============================================================

Documentar:

- Grid;
- Width="*";
- MinWidth;
- MinHeight;
- ScrollViewer;
- columnas;
- evitar Width rígidos innecesarios;
- footer/header fijos;
- contenido adaptable.

============================================================
60. UI_UX — NORMALIZACIÓN VISUAL DE NÚMEROS
============================================================

Documentar:

- costos/precios visuales;
- mínimo/máximo de decimales donde corresponda;
- stock;
- moneda;
- no mostrar 6 ceros inútiles;
- precisión interna no se pierde.

Usar implementación/converters reales.

============================================================
61. CREAR docs/PATRONES_DESARROLLO_KONTAXPRO.md
============================================================

Este documento contiene patrones TÉCNICOS para construir nuevos módulos.

NO debe duplicar UI_UX.

Título:

# Patrones de desarrollo KONTAXPRO Desktop

============================================================
62. PATRONES — ARQUITECTURA
============================================================

Documentar proyectos/capas actuales y dependencias permitidas.

Explicar responsabilidades reales de:

Domain
Application
Infrastructure
Desktop

No inventar Clean Architecture teórica si el repo difiere.

============================================================
63. PATRONES — MVVM
============================================================

Documentar:

View
ViewModel
commands
ObservableProperty
CommunityToolkit.Mvvm

Reglas:

- ViewModel sin acceso directo a Window;
- ViewModel sin MessageBox;
- lógica de negocio en servicios;
- UI logic mínima.

============================================================
64. PATRONES — DI
============================================================

Documentar:

- App.xaml.cs como composition root;
- registrar interfaces;
- resolver ViewModels;
- no `new Service()` dentro de VMs;
- lifetimes reales.

============================================================
65. PATRONES — EF CORE
============================================================

Documentar patrón consolidado:

IDbContextFactory<KontaxDbContext>

contexto por operación.

Para operaciones complejas:

una única instancia de contexto
+
una transacción.

No DbContext largo por ViewModel.

============================================================
66. PATRONES — MULTIEMPRESA
============================================================

Regla crítica:

Toda información empresarial se filtra por:

CurrentSession.EmpresaId

o mecanismo real equivalente.

Nunca confiar solo en filtro visual.

Validar en servicio.

============================================================
67. PATRONES — CURRENTSESSION
============================================================

Documentar propiedades actuales relevantes:

Usuario
Empresa
Establecimiento
Punto emisión
Bodega
Caja si ya existe
Roles
Permisos

Usar nombres reales.

Explicar:

preferencias != autorización.

============================================================
68. PATRONES — SERVICIOS
============================================================

Documentar:

ViewModel
→ Application interface
→ Infrastructure service
→ EF Core

Los nuevos módulos deben evitar consultas directas desde View.

============================================================
69. PATRONES — DTOs
============================================================

Documentar separación:

DTO de listado
DTO detalle
Request guardar
Results

No reutilizar un DTO gigante para todas las pantallas si objetivos difieren.

ProductsView debe usarse como ejemplo real.

============================================================
70. PATRONES — LISTADOS
============================================================

Documentar patrón consolidado:

- DTO ligero;
- query server-side;
- búsqueda;
- filtros;
- sort;
- paginación;
- KPIs;
- async;
- evitar N+1.

Usar ProductsView como referencia.

============================================================
71. PATRONES — FORMULARIOS NUEVO/EDITAR
============================================================

Documentar:

- mismo UserControl/ViewModel puede soportar modos si arquitectura actual lo hace;
- distinguir claramente IsNew/IsEdit o mecanismo real;
- no mezclar stock histórico con edición de maestro;
- confirmación cambios sin guardar;
- validación.

============================================================
72. PATRONES — MODALES
============================================================

Documentar:

- no Window arbitraria si existe shell;
- header;
- footer;
- owner;
- message service;
- resize;
- scroll.

============================================================
73. PATRONES — OPERACIONES HISTÓRICAS
============================================================

Regla general aprendida en Inventario:

No editar un hecho histórico para corregir el presente.

Registrar:

- movimiento;
- reverso;
- ajuste;
- corrección auditada;

según dominio.

Esta regla debe utilizarse en futuros módulos cuando aplique.

============================================================
74. PATRONES — SNAPSHOTS
============================================================

Documentar concepto:

Si un valor es necesario para preservar historia:

guardar snapshot en la operación.

Ejemplos actuales:

- factor;
- precio;
- impuestos;
- costo;
- motivo;

según código real.

No depender exclusivamente de maestros mutables.

============================================================
75. PATRONES — SECUENCIALES
============================================================

Documentar reglas existentes:

- no MAX+1;
- row lock/sequence;
- secuenciales fiscales;
- internos;
- establecimiento/empresa según tipo.

No copiar reglas específicas de Factura a documentos que no correspondan.

============================================================
76. PATRONES — TRANSACCIONES
============================================================

Documentar:

una operación funcional debe ser atómica cuando produce:

cabecera
detalles
movimientos
stock
costos
CxC/CxP
etc.

No SaveChanges parciales que dejen estado inconsistente.

============================================================
77. PATRONES — AUDITORÍA
============================================================

Documentar diferencia entre:

Historia operacional
vs.
Auditoría de seguridad/administrativa.

No utilizar `auditoria` como sustituto de tablas operativas.

============================================================
78. PATRONES — PERMISOS
============================================================

Documentar:

- ocultar/deshabilitar UI;
- pero validar también en servicio;
- permisos granulares;
- no confiar únicamente en botón invisible.

============================================================
79. PATRONES — CÓDIGO MUERTO
============================================================

Regla:

No dejar implementaciones fallidas ocultas.

Ejemplo:

autocomplete descartado de ProductsView.

Antes de agregar una alternativa, eliminar código muerto confirmado.

No eliminar código únicamente porque parezca sin uso sin verificar referencias.

============================================================
80. PATRONES — PERFORMANCE
============================================================

Documentar:

- async;
- paginación;
- debounce;
- CancellationToken si existe;
- evitar N+1;
- proyecciones;
- no cargar entidades completas innecesariamente.

============================================================
81. PATRONES — ERRORES
============================================================

Documentar:

- mensajes amigables;
- detalles técnicos al log;
- no stack traces al usuario;
- sistema global de mensajes;
- errores de campo inline.

============================================================
82. PATRONES — ARCHIVOS Y RUTAS
============================================================

Documentar brevemente convención real de creación:

cuando se cree un archivo nuevo:

- nombre;
- proyecto;
- carpeta;
- tipo;
- responsabilidad.

Esto ayuda a futuras tareas Codex.

============================================================
83. PATRONES — QUÉ REUTILIZAR DE PRODUCTOS
============================================================

Crear sección:

## Productos como módulo de referencia

Listar patrones reutilizables:

- layout de listado;
- KPIs;
- búsqueda;
- paginación;
- DataGrid;
- formulario;
- cards;
- mensajes;
- diálogos;
- servicios;
- DTOs;
- transacciones;
- validaciones;
- permisos.

============================================================
84. PATRONES — QUÉ NO COPIAR CIEGAMENTE
============================================================

Muy importante.

Productos es REFERENCIA, no plantilla rígida.

No copiar ciegamente:

- campos;
- columnas;
- número de KPIs;
- tamaños específicos;
- lógica de stock;
- operaciones de inventario;
- estructura exacta del formulario.

Cada módulo debe adaptar el patrón a su dominio.

============================================================
85. ACTUALIZAR AGENTS.md
============================================================

Después de crear los documentos:

actualizar AGENTS.md.

NO copiar todo el contenido de los nuevos documentos dentro de AGENTS.md.

Mantenerlo compacto.

Agregar una sección apropiada, por ejemplo:

## Documentación de referencia obligatoria

Con reglas como:

- Antes de modificar PRODUCTOS leer docs/PRODUCTOS_V1.md.
- Antes de crear o rediseñar UI leer docs/UI_UX_KONTAXPRO.md.
- Antes de crear un nuevo módulo leer docs/PATRONES_DESARROLLO_KONTAXPRO.md.
- docs/README.md es el índice de documentación.
- El código actual es siempre la fuente de verdad si la documentación queda desactualizada.
- Si una tarea modifica una regla documentada, actualizar el .md correspondiente en la misma tarea.

============================================================
86. AGENTS — PRODUCTOS V1 CONGELADO
============================================================

Agregar una regla breve:

PRODUCTOS V1 está considerado módulo funcionalmente cerrado.

No realizar refactors, rediseños o cambios de comportamiento en Productos salvo:

- requerimiento explícito;
- bug confirmado;
- integración necesaria con otro módulo.

Esto evita que futuros agentes “mejoren” arbitrariamente lo ya aprobado.

============================================================
87. AGENTS — PRODUCTOS COMO REFERENCIA
============================================================

Agregar:

ProductsView/ProductForm y sus componentes asociados son la primera referencia madura de UX y arquitectura del proyecto.

Los nuevos módulos deben reutilizar:

- estilos;
- controles;
- servicios;
- patrones;

cuando corresponda.

No duplicar recursos ya existentes.

No copiar XAML ciegamente.

============================================================
88. AGENTS — UI
============================================================

Agregar referencia corta:

Toda nueva UI debe cumplir:

docs/UI_UX_KONTAXPRO.md

Especialmente:

- Light/Dark;
- estilos globales;
- DataGrid;
- combos;
- mensajes;
- botones;
- mayúsculas;
- tablas;
- diálogos.

============================================================
89. AGENTS — DOCUMENTACIÓN VIVA
============================================================

Agregar regla:

La documentación es viva.

Cuando una tarea cambie de forma aprobada:

- reglas funcionales;
- arquitectura;
- UI global;
- patrón técnico;

Codex debe actualizar el .md correspondiente antes de terminar la tarea.

No actualizar documentos por cambios triviales que no afecten comportamiento/patrones.

============================================================
90. AGENTS — NO INVENTAR DESDE DOCUMENTACIÓN OBSOLETA
============================================================

Agregar:

Si existe contradicción entre:

documentación
y
código actual

NO modificar automáticamente el código para ajustarlo al .md.

Primero considerar el código fuente como estado real.

Informar la discrepancia.

Actualizar documentación únicamente si el código representa la decisión aprobada actual.

============================================================
91. AUDITORÍA FINAL DE PRODUCTOS — SOLO LECTURA
============================================================

Después de generar documentación realizar una auditoría final SIN CAMBIAR CÓDIGO.

Revisar:

BUILD
TESTS
EF
MULTIEMPRESA
INVENTARIO
COSTOS
LOTES
SERIES
PRESENTACIONES
PRECIOS
TRANSACCIONES
PERMISOS
UI
MESSAGEBOX
N+1
CÓDIGO MUERTO
SEEDERS

============================================================
92. BUILD
============================================================

Ejecutar:

dotnet restore
dotnet build KONTAXPRO.slnx

Informar:

errores
warnings.

NO corregirlos en esta tarea.

============================================================
93. TESTS
============================================================

Ejecutar:

dotnet test

Informar:

total
aprobados
fallidos
omitidos.

Si existe un fallo:

NO modificar código.

Documentarlo.

============================================================
94. EF CORE
============================================================

Verificar:

- InitialCreate actual;
- modelo;
- migraciones;
- cambios pendientes.

No generar migración.

Informar si EF detecta diferencias.

============================================================
95. MULTIEMPRESA
============================================================

Auditar consultas de Productos.

Buscar posibles accesos sin:

EmpresaId

cuando deberían estar aislados.

No corregir.

Reportar archivo/método si existe riesgo.

============================================================
96. INVENTARIO
============================================================

Auditar que no exista una ruta normal de aplicación que haga:

productoExistencia.StockActual = ...

como edición arbitraria fuera de operación trazable.

Distinguir actualización interna dentro de InventoryService de edición directa incorrecta.

============================================================
97. LOTES
============================================================

Revisar coherencia conceptual:

stock producto/bodega
vs.
stock lotes/bodega

para operaciones aplicables.

Si existen tests de reconciliación:

ejecutarlos.

Si no existen:

informar como recomendación futura.

NO agregar tests nuevos en esta tarea porque solo se autorizan .md/AGENTS.

============================================================
98. SERIES
============================================================

Revisar reglas:

- unicidad;
- estado;
- bodega;
- lote;
- cantidad.

Informar inconsistencias potenciales.

============================================================
99. PRESENTACIÓN BASE
============================================================

Auditar:

- una BASE por producto;
- factor = 1;
- no eliminación indebida;
- relación de unidad.

Informar.

============================================================
100. PRECIOS
============================================================

Auditar:

- presentación/lista;
- A/B/C;
- métodos;
- lista base;
- falta de hardcodes indebidos de IDs.

Informar.

============================================================
101. TRANSACCIONES
============================================================

Revisar operaciones complejas:

- inventario inicial;
- ajuste;
- conversión;
- corrección;

y confirmar si utilizan transacciones adecuadas.

No modificar.

============================================================
102. MESSAGEBOX
============================================================

Buscar:

MessageBox.Show
System.Windows.MessageBox

Informar cualquier uso residual relacionado con Productos/Inventario.

No modificarlo en esta tarea.

============================================================
103. N+1
============================================================

Auditar especialmente ProductsView.

Verificar que listado paginado no dispare consulta individual por producto para:

- presentación;
- stock;
- precio;
- marca;
- etc.

No realizar refactor.

Informar.

============================================================
104. SEEDERS
============================================================

Revisar:

StructuralSeeder
DemoSeeder

solo respecto de dependencias necesarias para Productos.

Confirmar:

- idempotencia aparente;
- catálogos requeridos;
- demo.

No ejecutar cambios estructurales.

============================================================
105. DOCUMENTAR RESULTADO DE AUDITORÍA
============================================================

Al final de:

docs/PRODUCTOS_V1.md

crear:

## Estado de auditoría de cierre

Con:

Fecha
Build
Tests
EF
Observaciones

NO incluir secretos ni connection strings.

Si existe algún pendiente real:

listarlo como:

PENDIENTE DE CORRECCIÓN

No declarar FINAL VALIDADO si existen errores críticos.

============================================================
106. NO DOCUMENTAR SECRETOS
============================================================

NUNCA escribir en los .md:

- contraseñas PostgreSQL;
- contraseñas usuarios demo;
- tokens;
- hashes;
- secrets;
- connection strings con password;
- certificados;
- claves P12.

Puede documentarse:

“las credenciales se obtienen mediante configuración segura”

sin valores.

============================================================
107. NO DOCUMENTAR DATOS PERSONALES INNECESARIOS
============================================================

No incluir:

- cédulas reales;
- nombres personales;
- correos personales;

como parte de ejemplos permanentes.

Usar ejemplos ficticios cuando sea necesario.

============================================================
108. FORMATO DE LOS .md
============================================================

Usar Markdown limpio:

# Títulos
## Secciones
tablas cuando aporten claridad
listas cortas
bloques de código solo cuando realmente ayuden.

No crear documentos llenos de párrafos redundantes.

Priorizar:

claridad
precisión
navegabilidad.

============================================================
109. LINKS ENTRE DOCUMENTOS
============================================================

Agregar referencias relativas.

Por ejemplo:

PRODUCTOS_V1.md:

Para patrones visuales globales consultar:
[UI/UX KONTAXPRO](UI_UX_KONTAXPRO.md)

Para patrones arquitectónicos:
[Patrones de desarrollo](PATRONES_DESARROLLO_KONTAXPRO.md)

Y viceversa cuando aporte.

============================================================
110. NO DUPLICAR
============================================================

Regla:

PRODUCTOS_V1.md
= QUÉ hace Productos y cómo está implementado.

UI_UX_KONTAXPRO.md
= CÓMO debe verse/comportarse la UI global.

PATRONES_DESARROLLO_KONTAXPRO.md
= CÓMO estructurar técnicamente nuevos módulos.

README.md
= índice.

AGENTS.md
= reglas ejecutivas y referencias.

Evitar repetir la misma explicación completa en los cuatro documentos.

============================================================
111. VALIDACIÓN DE DOCUMENTOS
============================================================

Antes de finalizar:

verificar que todas las rutas/clases/tablas mencionadas existan realmente.

Buscar posibles referencias inventadas.

No dejar:

TODO
TBD
“probablemente”
“se supone”

salvo que realmente sea un pendiente identificado.

============================================================
112. git diff
============================================================

Al terminar ejecutar:

git diff --check

y:

git status --short

Confirmar que los únicos archivos modificados/creados por ESTA tarea sean:

AGENTS.md
docs/*.md

Si existe otro archivo modificado previamente por el usuario/Codex:

NO revertirlo.

Solo informar que era un cambio preexistente.

============================================================
113. INFORME FINAL
============================================================

Al terminar informar:

1. Documentos creados.
2. Secciones principales de PRODUCTOS_V1.md.
3. Qué patrones quedaron documentados en UI_UX_KONTAXPRO.md.
4. Qué patrones quedaron en PATRONES_DESARROLLO_KONTAXPRO.md.
5. Qué se agregó a AGENTS.md.
6. Confirmar que Productos quedó marcado como V1 cerrado/referencia.
7. Resultado build.
8. Resultado tests.
9. Estado EF.
10. Resultado auditoría multiempresa.
11. Inventario.
12. Lotes.
13. Series.
14. Presentaciones.
15. Precios.
16. Transacciones.
17. MessageBox residuales.
18. Riesgo N+1.
19. Seeders.
20. Pendientes encontrados.
21. `git diff --check`.
22. Archivos modificados.

Confirmar expresamente:

“No se modificó código funcional durante esta tarea.”

“No se modificó la base de datos.”

“No se generaron migraciones.”

“No se hicieron cambios de diseño.”

“No se realizó commit ni push.”

============================================================
114. RESULTADO ESPERADO
============================================================

Al finalizar debe quedar:

AGENTS.md

docs/
├── README.md
├── PRODUCTOS_V1.md
├── UI_UX_KONTAXPRO.md
└── PATRONES_DESARROLLO_KONTAXPRO.md

Estos archivos pasan a ser memoria permanente del repositorio y referencia obligatoria para los siguientes módulos de KONTAXPRO.