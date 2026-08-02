PROMPT — Revisión funcional y visual del editor de Productos después de InitialCreate

Lee AGENTS.md completo antes de modificar código.

Contexto:

La reconstrucción de la BD Development mediante InitialCreate + StructuralSeeder + DemoSeeder ya funciona.

Resultado confirmado:
- Build: 0 errores y 0 advertencias.
- EF Core: sin cambios pendientes.
- Login demo funciona.
- Selección entre 2 empresas funciona.
- El módulo Productos abre y permite crear/editar.
- Se crea automáticamente la presentación BASE.
- Se pueden agregar presentaciones adicionales.
- Al editar el producto las presentaciones adicionales se conservan correctamente.
- El aislamiento multiempresa funciona hasta las pruebas realizadas.

Durante las pruebas funcionales del editor de Producto detecté los siguientes problemas que necesito corregir SIN rediseñar nuevamente la base aprobada.

============================================================
1. IVA / IMPUESTOS: COMBO VACÍO
============================================================

En la UI, el ComboBox "IVA / impuesto" aparece vacío.

Revisa primero la causa completa:

- StructuralSeeder.
- entidades `impuestos`.
- entidades `tarifas_impuesto`.
- relaciones.
- `productos_impuestos`.
- servicio que carga catálogos del editor.
- filtros por estado/vigencia.
- DTOs/ViewModel.
- binding XAML.

El StructuralSeeder debe crear los impuestos/tarifas estructurales que KONTAXPRO necesita para desarrollo y funcionamiento inicial.

Para el IVA usado actualmente por el proyecto deben existir como mínimo las tarifas con las que venimos trabajando:

- IVA 0 %
- IVA 5 %
- IVA 15 %

Respeta el diseño:
- impuesto general en `impuestos`;
- tarifa en `tarifas_impuesto`;
- Producto NO tiene `tarifa_impuesto_id`;
- relación Producto ↔ Tarifa mediante `productos_impuestos`.

No reintroduzcas `Producto.TarifaImpuestoId`.

Comprueba además que el ComboBox muestre nombres comprensibles, por ejemplo:

IVA 0 %
IVA 5 %
IVA 15 %

y que al editar un producto existente seleccione correctamente su impuesto.

El seeder debe ser idempotente.

NO generes otra InitialCreate salvo que exista una modificación estructural realmente imprescindible. Para corregir datos de seed, modifica el seeder correspondiente.

============================================================
2. LISTAS DE PRECIO
============================================================

Actualmente el ComboBox solo muestra:

LISTA BASE

El modelo aprobado soporta múltiples listas.

Para las dos empresas creadas por DemoSeeder necesito disponer de datos que permitan probar realmente esa funcionalidad.

Modifica DemoSeeder para que CADA empresa demo tenga, como mínimo:

- Lista A: lista base / precio normal.
- Lista B: lista preferencial.
- Lista C: lista preferencial.

Usa códigos estables, por ejemplo:

A
B
C

La Lista A debe ser `es_lista_base = true`.

B y C deben ser no base.

Puedes configurar descuentos predeterminados de demo razonables para B/C únicamente como datos Development.

No agregues estas listas empresariales al StructuralSeeder porque son configuraciones por empresa.

Revisa que el editor de Producto cargue todas las listas activas de la empresa actual y nunca mezcle listas de otra empresa.

============================================================
3. MEJORAR LA UI DE PRECIO
============================================================

Actualmente aparece directamente una caja de texto:

"Precio fijo"

Esto no es suficientemente intuitivo y además solo representa una de las estrategias del modelo.

Revisa el modelo aprobado de:

`productos_presentaciones_precios`

Para la lista base deben soportarse:

- PORCENTAJE_COSTO
- PRECIO_FIJO

Para listas preferenciales:

- DESCUENTO_PORCENTAJE
- PRECIO_FIJO

La UI debe explicarlo de forma simple al usuario.

Para LISTA A / BASE:

Mostrar algo conceptualmente similar a:

Método de cálculo
[ Porcentaje sobre costo | Precio fijo ]

Si selecciona Porcentaje sobre costo:
- mostrar campo `% utilidad`
- ocultar/deshabilitar precio fijo
- si ya existe costo, mostrar opcionalmente el precio resultante calculado como información

Si selecciona Precio fijo:
- mostrar `Precio de venta`
- ocultar/deshabilitar porcentaje

Para LISTA B/C:

Mostrar:

Método
[ Descuento sobre Lista A | Precio fijo ]

Si usa descuento:
- mostrar porcentaje de descuento
- permitir utilizar el descuento predeterminado de la lista cuando no exista override específico.

Si usa precio fijo:
- mostrar precio.

NO hagas que el usuario tenga que entender códigos técnicos como:
PORCENTAJE_COSTO
DESCUENTO_PORCENTAJE.

Usa textos amigables en UI y mantén los códigos internos en Domain.

Debe quedar claro visualmente qué lista se está configurando.

============================================================
4. COSTOS: ACTUALMENTE NO SE VEN
============================================================

El modelo ya tiene:

`productos_costos`

con:
- ultimo_precio_compra
- ultimo_costo_efectivo
- costo_promedio

Necesito una sección visual dentro del editor de Producto:

"Costo"

Debe mostrar como mínimo:

- Último precio de compra
- Último costo efectivo
- Costo promedio

Por ahora estos valores deben ser principalmente INFORMATIVOS / SOLO LECTURA desde el editor normal del producto.

No permitas editar libremente el costo promedio desde Productos.

Estos costos deben modificarse posteriormente mediante compras/movimientos/política de costos.

Si el producto todavía no tiene compras:
- mostrar 0,00 o un estado vacío amigable.

Usa formato monetario consistente.

============================================================
5. EXISTENCIAS POR BODEGA: ACTUALMENTE NO SE VEN
============================================================

El modelo ya tiene:

`productos_existencias`

Quiero una sección:

"Existencias por bodega"

Mostrar una fila/tarjeta por bodega de la empresa/establecimiento correspondiente con:

- Bodega
- Stock actual
- Stock reservado
- Disponible, si es útil mostrarlo calculado
- Stock mínimo
- Ubicación

Reglas:

SOLO LECTURA:
- stock_actual
- stock_reservado
- disponible

EDITABLE desde este editor:
- stock_minimo
- ubicacion

Nunca permitir editar manualmente `stock_actual`.

El stock real solo cambia mediante movimientos de inventario.

Si todavía no existe `productos_existencias` para una bodega, maneja el caso correctamente sin inventar stock. Si la estrategia aprobada del servicio crea la fila al configurar mínimo/ubicación, hazlo de forma transaccional.

Respeta aislamiento multiempresa.

============================================================
6. PRESENTACIONES ADICIONALES — REDISEÑO VISUAL
============================================================

La funcionalidad ya trabaja, pero la grilla actual usa la apariencia estándar de WPF y rompe completamente el diseño moderno de KONTAXPRO.

Rediseña SOLO la presentación visual, conservando la funcionalidad.

La sección debe mantener el estilo futurista/moderno existente:

- fondo adaptado a Light/Dark Theme;
- bordes redondeados;
- cabecera moderna;
- filas con buen espaciado;
- tipografía y colores obtenidos de recursos del tema;
- hover coherente;
- selección coherente;
- sin fondo blanco estándar fijo;
- sin controles Win32/WPF visualmente antiguos;
- scrollbar consistente con el resto de KONTAXPRO.

Columnas:

Código
Nombre
Factor
Código de barras
Compra
Venta
Acciones

Para Compra/Venta:
- usar CheckBox con el estilo morado/MaterialDesign que ya usa KONTAXPRO.

Para eliminar:
- reemplazar el botón textual "Quitar"
- usar botón compacto con icono MaterialDesign de papelera/eliminar
- hover rojo
- tooltip "Quitar presentación"
- no usar un botón rectangular gris estándar.

Ejemplo conceptual:

[trash icon]

No sacrifiques accesibilidad ni Command binding.

Si `DataGrid` dificulta conseguir el look & feel, evalúa:
- DataGrid completamente estilizado, o
- ItemsControl/ListView con template moderno.

Elige la alternativa que mejor se integre con la arquitectura existente SIN introducir complejidad innecesaria.

============================================================
7. FACTOR DE CONVERSIÓN
============================================================

Revisa también la presentación adicional mostrada en la prueba:

CAJA X12
Factor = 1.000000

Eso parece sospechoso para una caja x12 si la unidad base es Unidad.

NO cambies automáticamente datos existentes sin entender la lógica.

Verifica que:

factor_conversion

represente correctamente cuántas unidades base contiene la presentación.

Ejemplo:

Producto base:
FRASCO 20 ML
unidad base = UNIDAD

Presentación:
CAJA X12

debería conceptualmente tener:

factor_conversion = 12

si una caja contiene 12 unidades base.

Asegúrate de que la UI permita capturar claramente este valor y muestre una ayuda como:

"Indica cuántas unidades base contiene esta presentación."

La BASE siempre debe conservar:

factor_conversion = 1

============================================================
8. CÓDIGO DE BARRAS
============================================================

Mantén la lógica existente:

- código de barras del fabricante opcional;
- opción "No posee código de barras del fabricante";
- generación futura/interna KPX según diseño aprobado.

Verifica que códigos de barras adicionales respeten:

UNIQUE(empresa_id, codigo_barras)
WHERE codigo_barras IS NOT NULL

y que los errores se presenten al usuario de forma amigable.

============================================================
9. UX GENERAL
============================================================

No quiero simplemente agregar controles uno debajo de otro.

Reorganiza el editor para que siga siendo limpio pese al aumento de información.

Sugiero secciones visuales:

1. Información general
2. Clasificación e impuestos
3. Presentación base
4. Presentaciones adicionales
5. Precios
6. Costos
7. Existencias por bodega

Puedes usar Cards/Expander únicamente si mejoran la experiencia.

La información avanzada (costos/existencias/precios) puede ir en secciones claramente delimitadas para evitar saturar la pantalla.

Mantén:
- botones redondeados;
- colores actuales;
- iconos MaterialDesign;
- Dark/Light theme;
- Cancelar rojo;
- Guardar verde;
- panel responsive al tamaño disponible.

No modifiques arbitrariamente el look & feel general que ya aprobamos.

============================================================
10. REVISIÓN DE SEEDERS Y BASE DEVELOPMENT
============================================================

Después de corregir StructuralSeeder y DemoSeeder:

1. NO regeneres InitialCreate si el esquema no cambió.
2. Si únicamente cambian datos de seed, reutiliza la InitialCreate existente.
3. Como estamos en Development, realiza un reset controlado de la BD SOLO si es necesario para probar los nuevos seeders.
4. Antes de borrar confirma:
   - DOTNET_ENVIRONMENT=Development
   - host=localhost
   - Database=kontax_desktop
5. Si alguna condición no se cumple, aborta.

Después:
- InitialCreate existente
- StructuralSeeder
- DemoSeeder

Comprueba mediante consulta que existen:

Impuestos/tarifas:
- IVA 0
- IVA 5
- IVA 15

Para cada Empresa Demo:
- Lista A base
- Lista B
- Lista C

============================================================
11. PRUEBAS FUNCIONALES
============================================================

Realiza o deja verificables estas pruebas:

A. Impuesto
- abrir producto;
- Combo IVA contiene valores;
- elegir tarifa;
- guardar;
- cerrar/reabrir;
- selección se conserva.

B. Listas
- aparecen A/B/C;
- cambiar entre listas;
- cada una carga su configuración correspondiente;
- no mezcla empresas.

C. Precios
- PORCENTAJE_COSTO muestra porcentaje;
- PRECIO_FIJO muestra precio;
- listas preferenciales soportan descuento/precio fijo;
- datos persisten.

D. Costos
- sección visible;
- valores cargados desde productos_costos;
- no editables directamente.

E. Existencias
- sección visible;
- stock actual/reservado solo lectura;
- mínimo/ubicación editables;
- no modifica stock actual.

F. Presentaciones
- BASE permanece intacta;
- agregar CAJA X12 con factor 12;
- guardar;
- reabrir;
- factor sigue 12;
- editar producto no elimina la presentación;
- eliminar mediante icono papelera funciona.

G. Tema
- revisar Light;
- revisar Dark;
- ninguna tabla/grilla queda blanca o con estilos estándar incompatibles.

H. Multiempresa
- datos de Producto/Precios/Existencias/Listas de Empresa 1 no aparecen en Empresa 2.

============================================================
12. VALIDACIÓN TÉCNICA FINAL
============================================================

Ejecuta:

dotnet restore
dotnet build KONTAXPRO.slnx
git diff --check

Y verifica EF Core:

- no cambios pendientes en el modelo;
- no se generó una migración innecesaria.

Al terminar informa explícitamente:

1. causa de que IVA apareciera vacío;
2. qué se agregó al StructuralSeeder;
3. listas creadas por DemoSeeder;
4. comportamiento final de precios;
5. sección de costos implementada;
6. sección de existencias implementada;
7. archivos XAML/estilos modificados;
8. resultado Light/Dark;
9. resultado del build;
10. estado de EF Core/migraciones.

No hagas commit ni push salvo instrucción explícita.