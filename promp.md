PROMPT — Crear sistema global y reutilizable de mensajes, confirmaciones y notificaciones para KONTAXPRO

Lee AGENTS.md completo antes de modificar código.

CONTEXTO

KONTAXPRO Desktop ya posee un look & feel moderno, consistente, Light/Dark y basado en los estilos visuales propios del sistema.

Actualmente todavía existen mensajes mostrados mediante MessageBox nativo de Windows.

Ejemplo actual:

Título:
Cancelar nuevo producto

Mensaje:
Hay información ingresada que todavía no se ha guardado.
¿Desea cancelar el registro?

Botones:
Sí / No

Ese MessageBox rompe completamente el diseño visual de KONTAXPRO.

La finalidad de esta tarea es crear una infraestructura GLOBAL y REUTILIZABLE para:

- ERROR
- WARNING
- INFO
- SUCCESS
- CONFIRMATION

y reemplazar progresivamente los MessageBox nativos sin modificar las reglas de negocio existentes.

IMPORTANTE:

- NO rediseñar los formularios existentes.
- NO modificar reglas de negocio.
- NO cambiar validaciones.
- NO alterar comandos ni flujos salvo lo necesario para sustituir MessageBox.
- NO generar migraciones.
- NO modificar base de datos.
- NO hacer commit ni push.

============================================================
1. OBJETIVO GENERAL
============================================================

Crear un sistema global para que cualquier ViewModel pueda solicitar:

- mensaje de error;
- advertencia;
- información;
- éxito;
- confirmación;

sin conocer detalles de WPF ni crear ventanas directamente.

La solución debe respetar MVVM y DI.

Ejemplo conceptual esperado desde ViewModel:

await _messageService.ShowErrorAsync(
    "No se pudo guardar",
    "Ocurrió un problema al registrar el producto.");

await _messageService.ShowSuccessAsync(
    "Producto registrado",
    "El producto se creó correctamente.");

bool confirmar = await _messageService.ConfirmAsync(
    "Cancelar nuevo producto",
    "Hay información ingresada que todavía no se ha guardado. ¿Desea cancelar el registro?");

El ViewModel NO debe crear:

new Window()
MessageBox.Show()

============================================================
2. NO USAR MÁS MESSAGEBOX NATIVO
============================================================

Crear la infraestructura para eliminar progresivamente:

System.Windows.MessageBox
MessageBox.Show(...)

No dejar nuevos MessageBox nativos.

Auditar inicialmente el proyecto Desktop para identificar dónde se utilizan.

No reemplazar ciegamente cada llamada antes de entender su intención.

Clasificar cada mensaje como:

ERROR
WARNING
INFO
SUCCESS
CONFIRMATION

============================================================
3. SERVICIO GLOBAL
============================================================

Crear una abstracción apropiada, por ejemplo:

IMessageDialogService

o un nombre acorde a las convenciones actuales del proyecto.

Debe pertenecer a la capa adecuada.

La implementación concreta debe estar en Desktop porque depende de WPF.

Registrar mediante DI en App.xaml.cs/composition root.

No usar Service Locator.

============================================================
4. API SENCILLA PARA VIEWMODELS
============================================================

Proporcionar métodos claros.

Como mínimo:

ShowErrorAsync(...)
ShowWarningAsync(...)
ShowInfoAsync(...)
ShowSuccessAsync(...)
ConfirmAsync(...)

Evitar que cada ViewModel tenga que construir objetos visuales complejos.

Si internamente se utiliza un request/model común, está bien.

============================================================
5. TIPOS DE MENSAJE
============================================================

Crear un enum/modelo equivalente:

Error
Warning
Info
Success
Confirmation

No utilizar strings mágicos para determinar el tipo.

============================================================
6. DIÁLOGO GLOBAL KONTAXPRO
============================================================

Crear un diálogo personalizado reutilizable.

Debe seguir el look & feel actual.

Características:

- fondo según tema Light/Dark;
- borde redondeado;
- sombra suave;
- encabezado limpio;
- icono representativo;
- título;
- descripción;
- botones uniformes;
- tipografía y tamaños actuales;
- no usar chrome visual antiguo de Windows.

Debe sentirse parte natural de KONTAXPRO.

============================================================
7. ICONOS Y COLORES
============================================================

ERROR

Icono MaterialDesign apropiado:
AlertCircle / CloseCircle o equivalente.

Color visual:
rojo KONTAXPRO.

------------------------------------------------------------

WARNING

Icono:
Alert / AlertOutline o equivalente.

Color:
ámbar/naranja.

------------------------------------------------------------

INFO

Icono:
Information / InformationOutline.

Color:
azul.

------------------------------------------------------------

SUCCESS

Icono:
CheckCircle / CheckCircleOutline.

Color:
verde KONTAXPRO.

------------------------------------------------------------

CONFIRMATION

Icono:
HelpCircle / MessageQuestion / equivalente disponible.

Usar un color coherente con la paleta KONTAXPRO.

No inventar iconos externos.

Usar MaterialDesign PackIcon.

============================================================
8. ESTRUCTURA VISUAL
============================================================

Diseño conceptual:

┌─────────────────────────────────────────────┐
│  [ICONO]  TÍTULO                        [X] │
│                                             │
│           Mensaje principal                 │
│           texto adicional si existe         │
│                                             │
│                    [Cancelar] [Confirmar]    │
└─────────────────────────────────────────────┘

El diseño debe ser elegante y compacto.

No crear un diálogo gigantesco para mensajes pequeños.

============================================================
9. BOTÓN X SUPERIOR
============================================================

Usar el mismo lenguaje visual aprobado:

- botón rojo;
- icono Close blanco;
- esquina superior derecha;
- borde redondeado;
- hover coherente.

Su comportamiento depende del tipo.

Mensaje informativo:
equivale a cerrar.

Confirmación:
equivale a Cancelar/No.

Nunca debe interpretarse como Confirmar.

============================================================
10. BOTONES
============================================================

Botones con:

- mismo alto;
- ancho uniforme cuando haya dos;
- iconos;
- bordes redondeados;
- estilos globales.

Ejemplos:

ERROR:
[ ✓ Entendido ]

INFO:
[ ✓ Entendido ]

WARNING informativa:
[ ✓ Entendido ]

CONFIRMATION:
[ X Cancelar ] [ ✓ Confirmar ]

Cuando semánticamente corresponda:

[ No ] [ Sí ]

pero preferir textos que indiquen claramente la acción.

Ejemplo:

[ Seguir editando ] [ Cancelar registro ]

es mejor que:

[ No ] [ Sí ]

cuando el contexto lo permita.

No cambiar arbitrariamente textos actuales durante esta tarea.
Mantener intención original.

============================================================
11. CONFIRMACIONES
============================================================

ConfirmAsync debe devolver:

Task<bool>

o equivalente.

Debe poder utilizarse así:

if (!await _messageService.ConfirmAsync(...))
    return;

No bloquear utilizando hacks de UI.

============================================================
12. OWNER Y CENTRADO
============================================================

El diálogo debe:

- mostrarse centrado respecto de la ventana/modal que lo invoca;
- permanecer encima de su Owner;
- no aparecer detrás de otras ventanas;
- bloquear correctamente la ventana origen mientras sea modal.

No centrar exclusivamente respecto del monitor si existe Owner válido.

============================================================
13. OVERLAY
============================================================

Cuando se muestre un diálogo modal:

- aplicar overlay tenue sobre el contenido de fondo si es coherente con la arquitectura actual;
- mantener visible el contexto;
- no permitir interacción accidental con el formulario detrás.

El overlay debe adaptarse a Light/Dark.

No usar efectos excesivos.

============================================================
14. LIGHT / DARK
============================================================

El diálogo debe reaccionar al ThemeService existente.

En Dark:

- fondo oscuro;
- texto claro;
- bordes sutiles.

En Light:

- fondo claro;
- texto oscuro.

Los colores semánticos:

rojo
verde
azul
ámbar

deben conservar suficiente contraste en ambos temas.

No hardcodear fondos que destruyan el tema.

Preferir DynamicResource cuando corresponda.

============================================================
15. MENSAJES DE ÉXITO
============================================================

No todos los SUCCESS necesitan un modal que obligue al usuario a pulsar Aceptar.

Crear también una notificación no bloqueante reutilizable:

Snackbar / Toast KONTAXPRO.

Ejemplo:

✓ Producto registrado correctamente.

Debe:

- aparecer brevemente;
- no bloquear;
- desaparecer automáticamente;
- mantener look & feel;
- poder cerrarse manualmente cuando corresponda.

Utilizar SUCCESS preferentemente mediante notificación breve cuando no se requiere decisión.

============================================================
16. INFORMACIÓN NO CRÍTICA
============================================================

INFO también debe poder mostrarse como Snackbar/Toast cuando no requiere interacción.

Ejemplo:

“La lista de productos fue actualizada.”

No abrir un modal por cada información trivial.

============================================================
17. ERRORES
============================================================

Los errores que impiden continuar deben utilizar diálogo modal.

Ejemplo:

NO:

“Error: DbUpdateException FK_Producto...”

Sí:

“No se pudo registrar el producto.”

y mensaje comprensible.

Mantener detalles técnicos únicamente en logging/debug.

No mostrar:

- stack traces;
- nombres de tablas;
- SQL;
- excepciones EF;
- connection strings;
- paths internos;

al usuario final.

============================================================
18. ADVERTENCIAS
============================================================

WARNING debe servir para condiciones importantes pero no necesariamente errores.

Ejemplo:

“El precio de venta está por debajo del costo actual.”

Puede ser:

- modal si requiere decisión;
- notificación si únicamente informa.

La infraestructura debe permitir ambos usos sin duplicar componentes.

============================================================
19. VALIDACIONES DE FORMULARIO
============================================================

NO reemplazar automáticamente todas las validaciones inline por diálogos.

Errores como:

“Nombre obligatorio”

“Cantidad requerida”

“Caducidad inválida”

deben seguir apareciendo junto al campo cuando esa sea la UX actual.

El sistema global se usa para:

- errores de operación;
- confirmaciones;
- advertencias generales;
- éxito;
- información global.

No convertir cada validación de campo en popup.

============================================================
20. MENSAJE ACTUAL DE CANCELAR PRODUCTO
============================================================

Reemplazar específicamente el MessageBox mostrado actualmente al cerrar/cancelar Nuevo Producto.

Actual:

Título:
Cancelar nuevo producto

Mensaje:
Hay información ingresada que todavía no se ha guardado. ¿Desea cancelar el registro?

Debe utilizar el nuevo diálogo KONTAXPRO.

Mantener exactamente la lógica actual:

Confirmar cancelación
→ cerrar/cancelar.

Cancelar la confirmación
→ permanecer en el formulario.

No modificar el comportamiento del formulario Nuevo.

============================================================
21. TEXTOS Y MAYÚSCULAS
============================================================

Los mensajes de UI NO deben forzarse completamente a MAYÚSCULAS.

Mantener escritura natural:

“Producto registrado correctamente.”

No:

“PRODUCTO REGISTRADO CORRECTAMENTE.”

Los datos comerciales siguen las reglas de mayúsculas ya existentes.

============================================================
22. ACCESIBILIDAD
============================================================

El diálogo debe admitir:

- Enter para acción principal cuando sea seguro;
- Escape para Cancelar/Cerrar;
- navegación mediante Tab;
- foco inicial apropiado;
- contraste legible;
- texto multilínea;
- TextWrapping.

En una confirmación destructiva:

NO hacer que Enter confirme accidentalmente una operación peligrosa si la UX actual no lo hace.

Preferir foco inicial en la opción segura.

Ejemplo:

Cancelar registro:

foco inicial:
Seguir editando

no:
Cancelar registro.

============================================================
23. EVITAR DOBLE DIÁLOGO
============================================================

La infraestructura debe evitar abrir accidentalmente múltiples instancias del mismo diálogo por doble click.

Deshabilitar o proteger comandos mientras una confirmación esté abierta si es necesario.

No crear una solución global excesivamente compleja.

============================================================
24. MENSAJES LARGOS
============================================================

Soportar:

- títulos cortos;
- mensajes de varias líneas;
- detalle secundario opcional.

Aplicar:

TextWrapping
MaxWidth razonable
Scroll solo para textos excepcionalmente largos.

No hacer crecer indefinidamente el diálogo.

============================================================
25. RESULTADOS MÁS FLEXIBLES
============================================================

Aunque inicialmente ConfirmAsync necesite bool, diseñar internamente de manera que pueda soportarse eventualmente:

Primary
Secondary
Cancel

sin reescribir toda la infraestructura.

No sobrediseñar una API enorme ahora.

============================================================
26. SERVICIO DE NOTIFICACIONES
============================================================

Si arquitectónicamente queda más limpio, separar:

IMessageDialogService
INotificationService

donde:

MessageDialogService:
- Error
- Warning
- Confirmation
- Info modal

NotificationService:
- Success
- Info no bloqueante
- Warning no bloqueante

Si eso añade duplicación innecesaria, puede existir una fachada común.

Priorizar claridad.

============================================================
27. NO ACOPLAR VIEWMODEL A WPF
============================================================

Los ViewModels no deben conocer:

Window
MessageBox
PackIcon
Brush
DialogHost
Snackbar

Solo deben solicitar una intención semántica.

Ejemplo:

_messageService.ShowErrorAsync(...)

La implementación Desktop decide cómo renderizar.

============================================================
28. REGISTRO EN DI
============================================================

Registrar servicios siguiendo la estrategia actual de App.xaml.cs.

Usar los lifetimes apropiados.

No crear singletons con referencias permanentes a ventanas si eso puede provocar fugas.

============================================================
29. ESTILOS GLOBALES
============================================================

Crear recursos reutilizables para:

- contenedor del diálogo;
- iconos;
- header;
- botones;
- overlay;
- Snackbar/Toast.

No duplicar colores y estilos en cada ventana.

Integrar con:

Colors.xaml
LightTheme.xaml
DarkTheme.xaml
ButtonStyles.xaml

o recursos equivalentes reales.

No mover recursos arbitrariamente si la estructura actual es distinta.

============================================================
30. PRIMERA FASE DE MIGRACIÓN DE MENSAJES
============================================================

Después de construir la infraestructura:

buscar usos actuales de:

MessageBox.Show
System.Windows.MessageBox

Clasificarlos.

Reemplazar prioritariamente los que pertenezcan a:

- Login
- Selección empresa
- ProductsView
- ProductForm
- Inventario
- formularios ya construidos y probados.

No modificar lógica durante la sustitución.

============================================================
31. NO CAMBIAR TEXTOS SIN NECESIDAD
============================================================

En esta primera migración:

mantener los textos actuales siempre que sean comprensibles.

Si detectas un mensaje:

- técnico;
- confuso;
- contradictorio;

informarlo, pero no reescribir masivamente contenido sin necesidad.

============================================================
32. API DE EJEMPLO
============================================================

La API final debería permitir algo conceptualmente similar:

await _messageService.ErrorAsync(
    "No se pudo guardar",
    "Revise los datos e intente nuevamente.");

await _messageService.WarningAsync(
    "Stock insuficiente",
    "No existe suficiente existencia para completar la operación.");

await _notificationService.SuccessAsync(
    "Producto registrado correctamente.");

await _notificationService.InfoAsync(
    "Información actualizada.");

var confirmado = await _messageService.ConfirmAsync(
    "Cancelar nuevo producto",
    "Hay información ingresada que todavía no se ha guardado. ¿Desea cancelar el registro?");

Los nombres exactos deben seguir las convenciones del proyecto.

============================================================
33. EJEMPLOS VISUALES A VERIFICAR
============================================================

ERROR

Icono rojo
Título:
No se pudo registrar

Texto:
Ocurrió un problema al guardar la información.

Botón:
Entendido

------------------------------------------------------------

WARNING

Icono ámbar
Título:
Advertencia

Texto:
El precio configurado está por debajo del costo actual.

Botones si requiere decisión:
Cancelar
Continuar

------------------------------------------------------------

INFO

Icono azul
Título:
Información

Texto:
No existen movimientos para el período seleccionado.

------------------------------------------------------------

SUCCESS

Notificación verde:

✓ Producto registrado correctamente.

------------------------------------------------------------

CONFIRMATION

Título:
Cancelar nuevo producto

Texto:
Hay información ingresada que todavía no se ha guardado.
¿Desea cancelar el registro?

Botones:

Seguir editando
Cancelar registro

La opción segura debe quedar visualmente clara.

============================================================
34. PRUEBAS
============================================================

Probar:

1. Error modal.
2. Warning modal.
3. Info modal.
4. Success Snackbar.
5. Info Snackbar.
6. Confirmation = aceptar.
7. Confirmation = cancelar.
8. Cerrar confirmation con X.
9. Escape.
10. Enter.
11. Light.
12. Dark.
13. Mensaje largo.
14. Owner correcto.
15. Abrir desde ProductForm.
16. Abrir desde ventana auxiliar de Inventario.
17. Cambiar tema y volver a abrir.
18. Confirmación Cancelar nuevo producto.

============================================================
35. BUSCAR MESSAGEBOX RESIDUALES
============================================================

Al finalizar ejecutar búsqueda global por:

MessageBox.Show
System.Windows.MessageBox
MessageBoxResult
MessageBoxButton

Informar cuáles fueron reemplazados y cuáles permanecen.

Si alguno permanece:

explicar por qué.

El objetivo final es no depender de MessageBox nativo para interacción normal de KONTAXPRO.

============================================================
36. VALIDACIÓN TÉCNICA
============================================================

Ejecutar:

dotnet restore
dotnet build KONTAXPRO.slnx
dotnet test
git diff --check

Verificar:

- 0 errores;
- 0 advertencias si es posible;
- no cambios EF;
- no migraciones;
- no cambios en base.

============================================================
37. INFORME FINAL
============================================================

Informar:

1. Arquitectura elegida.
2. Interfaces creadas.
3. Implementaciones creadas.
4. Archivos XAML/recursos creados.
5. Tipos de mensajes soportados.
6. Cómo funciona Confirmation.
7. Cómo funcionan Toast/Snackbar.
8. Cómo se resuelve Light/Dark.
9. Cómo se determina Owner.
10. Qué MessageBox fueron sustituidos.
11. Confirmar específicamente que “Cancelar nuevo producto” usa el nuevo diálogo.
12. MessageBox residuales.
13. Pruebas realizadas.
14. Build.
15. Tests.
16. Pendientes, si existen.

Confirmar expresamente:

“No se modificaron reglas de negocio.”

“No se modificó la base de datos.”

“No se generaron migraciones.”

No hacer commit ni push.