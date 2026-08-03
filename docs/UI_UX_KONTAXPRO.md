# UI/UX KONTAXPRO Desktop — Estándar visual

`ProductsView` y `ProductFormView` son el primer módulo donde estos patrones quedaron consolidados. Los módulos nuevos deben seguirlos adaptándolos a su contexto, sin copiar XAML ciegamente.

## 1. Principios

La interfaz KONTAXPRO debe ser moderna, limpia, fácil de recorrer y consistente. El aspecto puede ser contemporáneo o futurista de forma discreta; nunca debe ocultar la tarea del usuario. El principio rector es **robustez por dentro, simplicidad por fuera**.

Prioridades:

- Jerarquía visual clara.
- Acciones principales fáciles de identificar.
- Información densa pero legible.
- Respuesta visible ante carga, éxito, advertencia y error.
- Controles cómodos con mouse y teclado.
- Misma semántica en Light y Dark.

## 2. Temas Light y Dark

Los colores se consumen mediante `DynamicResource`. `ThemeService` intercambia los diccionarios `Themes/LightTheme.xaml` y `DarkTheme.xaml` y persiste la preferencia local.

No se deben fijar colores de texto o superficies que solo funcionen en un tema. Antes de aprobar una vista se revisan: fondo general, cards, texto primario/secundario, bordes, campos, selección, hover, disabled, tablas, popups y scrollbars en ambos temas.

## 3. Paleta semántica

La marca usa verde como acción positiva y foco. La implementación actual ofrece recursos semánticos para superficie, texto, borde, primario, información, advertencia, peligro y variantes suaves.

- Verde: acción principal, alta, confirmación y foco.
- Azul: información y acciones neutrales destacadas.
- Ámbar: advertencia o pendiente.
- Rojo: error, eliminación, cancelación destructiva y asterisco obligatorio.
- Morado: acento secundario cuando el contexto lo requiera.

Usar los brushes existentes antes de crear colores. Un estado no debe comunicarse únicamente por color: agregar texto, icono o tooltip cuando sea necesario.

## 4. Formularios

El patrón del editor de Productos es:

1. Encabezado fijo con icono, título, contexto y botón cerrar.
2. Cuerpo desplazable organizado por secciones y cards.
3. Pie fijo con Cancelar y Guardar/Confirmar siempre visibles.

Nuevo y Editar comparten View y ViewModel cuando el dominio y las validaciones son comunes. La pantalla adapta textos, visibilidad y operaciones al modo. Al ocultarse, su `ScrollViewer` vuelve al inicio para que una reapertura no conserve la posición anterior.

Las secciones se agrupan por intención, no por entidad técnica. Evitar campos apilados innecesariamente: distribuir el ancho disponible sin perder alineación ni forzar scroll horizontal.

## 5. Cierre mediante X

El botón X se ubica en la esquina superior derecha, usa icono `Close`, tooltip y un estilo neutral coherente. Debe ejecutar el mismo flujo seguro que Cancelar: cerrar diálogos auxiliares, limpiar estado temporal y ocultar la vista. No debe saltarse confirmaciones por cambios pendientes cuando estas existan.

## 6. Botones

Reutilizar los estilos globales:

- `PrimaryButtonStyle`: guardar, confirmar, crear y acción principal.
- `NeutralButtonStyle`: cancelar no destructivo, ver o acción secundaria.
- `DangerButtonStyle`: eliminar, anular o cancelar una operación activa.
- `CatalogAddButtonStyle`: botón sólido verde para agregar catálogos o filas.

Los botones relacionados y ubicados en el mismo pie deben conservar ancho y altura uniformes. Incluir un icono Material Design relacionado, texto breve y tooltip cuando la intención no sea evidente. Los iconos sobre botones sólidos son blancos.

Los estados hover y pressed deben ser suaves. Disabled debe seguir siendo legible y claramente inactivo.

## 7. Campos y etiquetas

Cada control mantiene separación estándar respecto de su etiqueta y del campo siguiente. El asterisco obligatorio es rojo, como en el formulario principal de Productos. Los textos de ayuda aparecen debajo con color secundario y no compiten con la etiqueta.

TextBox, ComboBox, DatePicker y editores de DataGrid deben compartir:

- altura cómoda y uniforme;
- esquinas redondeadas;
- padding que no recorte caracteres al obtener foco;
- borde normal discreto;
- borde o foco verde;
- foreground y background por recursos del tema.

Los placeholders no sustituyen una etiqueta. Para números que inician en cero, puede limpiarse visualmente al entrar y restaurarse cero al salir vacío, sin perder la validación del ViewModel.

## 8. Mayúsculas

Los códigos, lotes, series y otros identificadores definidos por el dominio se capturan en mayúsculas mediante `UppercaseInputBehavior`, estilos de edición o normalización equivalente. Infrastructure vuelve a normalizar los valores importantes.

No convertir automáticamente descripciones libres salvo que la regla funcional lo pida. La búsqueda puede normalizar para comparar sin cambiar el dato almacenado.

## 9. Tablas y listas editables

Usar `KontaxDataGridStyle` y sus estilos relacionados. La cabecera debe permanecer legible, las filas tener altura suficiente para todas sus líneas y los editores mantener altura uniforme en cada columna.

Reglas:

- Encabezados breves y en mayúsculas cuando siga el patrón actual.
- Columna final de botones denominada `ACCIONES`.
- Iconos de acción completos, centrados y con tooltip.
- Texto multilínea solo cuando la información lo justifica; cada elemento lógico ocupa su propia línea.
- Una opción de sugerencia se muestra en una sola línea y sin superponerse con otro template.
- Las listas crecen verticalmente cuando el diálogo dispone de espacio; el formulario principal conserva su scroll.
- La rueda del mouse debe desplazar el contenedor útil aun cuando el puntero esté sobre una lista que no necesita scroll propio.

## 10. Ordenamiento

Cuando una columna es ordenable, el ViewModel controla campo y dirección y la consulta ordena en servidor. El indicador visual debe reflejar el orden real. No simular orden local sobre una sola página si el conjunto está paginado.

## 11. KPIs

Los KPIs son filtros-resumen, no decoración. Se presentan en cards compactas con icono blanco sobre color sólido, título, cifra y estado seleccionado reconocible. La cantidad y semántica dependen del módulo; Productos usa Todos, Stock bajo, Sin stock y Por caducar.

La cifra debe provenir de la misma definición funcional que el filtro. Un KPI no debe contar entidades distintas de las que lista la pantalla.

## 12. Búsqueda

El campo de búsqueda contiene icono, texto de ayuda y una X al final para limpiar. Limpiar restaura la consulta inicial. Las búsquedas remotas usan debounce, cancelación y protección frente a respuestas atrasadas.

La coincidencia y los campos buscables se definen en Application/Infrastructure. La View no debe implementar una segunda semántica distinta.

## 13. Paginación

Mostrar total, página actual, tamaño de página y navegación anterior/siguiente. Para muchas páginas, usar una secuencia compacta con elipsis. Cambiar filtro, búsqueda, KPI o tamaño regresa a la primera página.

Los controles de paginación permanecen fuera del área de filas y no deben confundirse con el scrollbar horizontal.

## 14. Badges

Los badges representan estados breves: activo, inactivo, existente, nuevo, disponible, advertencia o pendiente. Deben ser compactos, redondeados y legibles en ambos temas. Cuando una palabra redundante ocupa espacio sin aportar, puede usarse color de texto o icono, siempre que la distinción siga siendo accesible.

## 15. Mensajes, notificaciones y carga

No usar `MessageBox` nativo. Los mecanismos globales son:

| Necesidad | Servicio | Comportamiento |
|---|---|---|
| Confirmación, error o advertencia bloqueante | `IMessageDialogService` | `MessageDialogWindow` modal con look KONTAXPRO. |
| Éxito o información temporal | `INotificationService` | Toast no bloqueante con duración controlada. |
| Trabajo asíncrono que debe impedir duplicados | `ILoadingService` | Ventana KONTAXPRO animada que deshabilita al propietario. |

El loading debe aparecer para latencias perceptibles, no para cada operación instantánea. Una animación local de listado puede indicar carga sin bloquear; debe ser visible, elegante y compatible Light/Dark.

Los mensajes de validación largos se organizan por dato en líneas separadas. El usuario debe entender qué corregir sin inspeccionar detalles técnicos.

## 16. Diálogos auxiliares

Los diálogos de ajuste, conversión, corrección y Kardex usan icono relacionado, encabezado claro, cuerpo organizado y pie fijo. Cancelar y confirmar permanecen visibles. Los mensajes de éxito pueden ser temporales dentro del diálogo cuando no requieren decisión.

La capa visual puede usar code-behind únicamente para mecánica de foco, scroll, tamaño o interacción que WPF no expresa limpiamente. La validación y persistencia siguen en ViewModel y servicios.

## 17. Iconografía

Reutilizar `MaterialDesignThemes.Wpf.PackIcon`. El icono comunica la acción o entidad, no rellena espacio. Mantener proporciones consistentes: iconos de botón habitualmente de 18 px y tamaños mayores solo en títulos o KPIs.

Ejemplos consolidados: plus para agregar, lápiz para editar/corregir, ojo para consultar, refresh para convertir, tune para ajustar, paquete para inventario y close para cerrar/cancelar.

## 18. Adaptación al espacio WPF

WPF Desktop no es una web responsive, pero debe adaptarse al tamaño disponible:

- Usar `Grid` y columnas `Auto`/`*` antes que posiciones absolutas.
- Definir `MinWidth`/`MinHeight` donde evita cortes, no para fijar toda la pantalla.
- Permitir que formularios extensos usen scroll vertical y que tablas amplias usen scroll horizontal.
- Mantener footer y acciones críticas visibles.
- Ampliar diálogos cuando reduce apilamiento, sin exceder el área de trabajo.
- Restablecer posición de scroll al ocultar/reabrir.

## 19. Scrollbars

El estilo global está en `Styles/ScrollBarStyles.xaml`. Debe ofrecer pista discreta y Thumb continuo, visible y arrastrable. Los `RepeatButton` internos del Track son transparentes, sin borde ni hover visible; hacer clic en la pista conserva el desplazamiento por página.

Solo el Thumb reacciona con hover sutil. La pista no muestra bloques celestes. El patrón aplica horizontal y verticalmente y funciona en Light/Dark. No crear un scrollbar local distinto si el global resuelve el caso.

## 20. Normalización visual de números

La presentación visual no cambia la precisión interna:

- Cantidades enteras se muestran sin decimales.
- Cantidades con fracción se muestran con hasta dos decimales en resúmenes cuando así está definido.
- Costos pueden mostrar mínimo dos y máximo seis decimales.
- Valores monetarios respetan la precisión persistida y el formato del contexto.

Aplicar converters o `StringFormat` ya existentes. No redondear el valor del ViewModel solo para mejorar su apariencia.

## 21. Lista de comprobación visual

Antes de cerrar una nueva vista:

- probar Light y Dark;
- recorrer con mouse y teclado;
- comprobar foco, hover, pressed y disabled;
- revisar labels, asteriscos y ayudas;
- validar scroll vertical/horizontal y restauración de posición;
- probar filas vacías, una fila y muchas filas;
- comprobar textos largos y escalado de ventana;
- verificar mensajes, loading y acciones del footer.
