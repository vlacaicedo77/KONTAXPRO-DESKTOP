PROMPT — Auditar por qué “Registrar ajuste” permanece deshabilitado en ENTRADA LOTE_Y_SERIE

Lee AGENTS.md antes de modificar código.

Trabaja exclusivamente sobre la ventana:

“Registrar ajuste de inventario”

y específicamente sobre:

Tipo de ajuste = ENTRADA
Tipo de control = LOTE_Y_SERIE

NO rediseñes la ventana.
NO modifiques SALIDA.
NO modifiques el modo NUEVO de Producto.
NO cambies las reglas de negocio aprobadas salvo que encuentres una inconsistencia real.
NO generes migraciones.
NO hagas commit ni push.

============================================================
1. PROBLEMA OBSERVADO
============================================================

Existe un caso en el que aparentemente todos los requisitos están completos, pero el botón:

REGISTRAR AJUSTE

permanece deshabilitado.

Caso reproducido:

Tipo ajuste:
ENTRADA

Bodega:
FAC · PRODUCTOS CON FACTURA

Presentación:
UNIDAD

Cantidad presentación:
2

Costo presentación:
330,00

Motivo:
informado y no vacío

Lotes:

PJB-2608-A
Cantidad = 2

Resumen lotes:

Asignado: 2
Requerido: 2
Pendiente: 0

Series:

SA-001 → PJB-2608-A
SA-002 → PJB-2608-A

Resumen series:

Registradas: 2
Requeridas: 2
Pendientes: 0

Observación:
informada, aunque debe seguir siendo opcional.

A pesar de esto:

Registrar ajuste = deshabilitado.

============================================================
2. NO SOLUCIONARLO FORZANDO EL BOTÓN
============================================================

NO hagas simplemente:

IsEnabled = true

ni elimines validaciones para que funcione.

Debes identificar exactamente qué condición interna provoca:

CanRegistrarAjuste == false

o:

RegistrarAjusteCommand.CanExecute == false

y corregir la causa.

============================================================
3. AUDITAR TODAS LAS CONDICIONES DEL CANEXECUTE
============================================================

Localiza:

- RegistrarAjusteCommand;
- CanRegistrarAjuste;
- propiedades calculadas relacionadas;
- validadores;
- métodos EsValido/Validar;
- flags de lotes;
- flags de series;
- errores de ViewModel;
- INotifyDataErrorInfo si existe;
- NotifyCanExecuteChangedFor;
- NotifyPropertyChangedFor.

Enumera antes de corregir TODAS las condiciones necesarias para habilitar el botón.

Ejemplo conceptual:

Tipo válido
&& BodegaId != null
&& PresentacionId != null
&& CantidadPresentacion > 0
&& CantidadBase > 0
&& CostoPresentacion >= 0
&& Motivo no vacío
&& LotesValidos
&& SeriesValidas
&& !IsSaving

No asumir. Revisar la implementación real.

============================================================
4. AÑADIR DIAGNÓSTICO TEMPORAL PARA IDENTIFICAR LA CAUSA
============================================================

Durante la investigación, crea una forma temporal de saber qué condición falla.

Puede ser:

- Debug.WriteLine;
- logging;
- propiedad interna para pruebas;
- método que devuelva las razones de invalidez.

Ejemplo conceptual:

ObtenerRazonesAjusteNoValido()

Resultado esperado:

[
    "Bodega no seleccionada",
    "Serie SA-001 sin lote"
]

No mostrar mensajes técnicos permanentes al usuario.

Eliminar o dejar únicamente logging apropiado después de solucionar el problema.

============================================================
5. VALIDAR BODEGA
============================================================

Confirmar que:

FAC · PRODUCTOS CON FACTURA

tenga realmente:

BodegaId válido

y que el formulario no esté validando accidentalmente:

SelectedBodega != null

cuando únicamente se actualizó BodegaId, o viceversa.

============================================================
6. VALIDAR PRESENTACIÓN
============================================================

Confirmar que:

UNIDAD

tenga:

ProductoPresentacionId válido
FactorConversion = 1

y que:

CantidadBase = 2 × 1 = 2

No debe existir discrepancia entre:

PresentacionSeleccionada
ProductoPresentacionId
FactorConversion

============================================================
7. VALIDAR MOTIVO
============================================================

Motivo debe ser obligatorio.

La validación correcta debe ser:

!string.IsNullOrWhiteSpace(Motivo)

Aplicar Trim.

No exigir Observación.

Observación continúa siendo opcional.

============================================================
8. VALIDAR COSTO
============================================================

Para ENTRADA:

CostoPresentacion debe ser válido según las reglas existentes.

En el caso reproducido:

330,00

es válido.

Verificar que el problema no sea una conversión de cultura:

330.00
vs
330,00

y que el binding no esté dejando internamente:

CostoPresentacion = 0

o un estado de validación incorrecto aunque visualmente muestre 330.00.

Usar decimal.

============================================================
9. VALIDAR LOTE EXISTENTE
============================================================

El lote seleccionado/escrito es:

PJB-2608-A

y ya existe.

La pantalla además está usando este lote como referencia para el costo, por lo que aparentemente el autocomplete ya lo reconoce.

Auditar que la fila tenga internamente:

ProductoLoteId válido
NumeroLote = "PJB-2608-A"
EsLoteExistente = true

o las propiedades equivalentes reales.

Posible error a investigar:

El autocomplete muestra PJB-2608-A,
pero la validación todavía considera el lote como no seleccionado porque:

ProductoLoteId == null

o porque el texto cambió después de seleccionar la sugerencia.

Si ocurre eso, corregir la sincronización.

============================================================
10. VALIDAR LOTES
============================================================

Para este caso:

CantidadBase requerida = 2

Lote PJB-2608-A:
Cantidad = 2

Por tanto:

Asignado = 2
Requerido = 2
Pendiente = 0

LotesValidos debe ser TRUE.

Además:

- no lote vacío;
- no cantidad <= 0;
- no cantidad excedida;
- no duplicado;
- no fechas requeridas porque este producto NO controla caducidad.

IMPORTANTE:

Si:

maneja_fecha_caducidad = false

NO exigir:

- fecha elaboración;
- fecha caducidad.

============================================================
11. VALIDAR SERIES
============================================================

Series:

SA-001
SA-002

Cantidad requerida = 2.

Por tanto:

Registradas = 2
Requeridas = 2
Pendientes = 0

SeriesValidas debe ser TRUE.

Validar además:

- serie no vacía;
- no duplicada;
- no existente previamente en BD;
- cada serie tiene lote asociado.

============================================================
12. VALIDAR ASOCIACIÓN SERIE → LOTE
============================================================

Ambas series están visualmente asociadas:

SA-001 → PJB-2608-A
SA-002 → PJB-2608-A

Y el lote requiere cantidad 2.

Por tanto:

SeriesAsignadasAlLote(PJB-2608-A) = 2
CantidadLote(PJB-2608-A) = 2

La validación por lote debe ser TRUE.

Investigar especialmente si el ComboBox de:

LOTE ASOCIADO

muestra correctamente PJB-2608-A pero internamente está almacenando:

- texto;
- objeto temporal diferente;
- Id incorrecto;
- referencia distinta;

y por eso la comparación del ViewModel falla.

La comparación debe basarse en un identificador estable de la fila/lote, no en referencia de objeto accidental.

============================================================
13. REVISAR MODELOS TEMPORALES
============================================================

Este punto es especialmente importante para lotes nuevos y existentes.

Si las filas de lotes del ajuste usan un modelo temporal, cada lote debería tener un identificador estable en memoria, por ejemplo:

TemporaryId / Guid

además de:

ProductoLoteId nullable

Esto permite asociar series correctamente incluso cuando el lote es NUEVO y todavía no tiene Id en PostgreSQL.

Revisar si actualmente LOTE ASOCIADO depende exclusivamente de:

ProductoLoteId

porque en lotes nuevos ese valor será null hasta guardar.

Si es así, corregir el modelo temporal para que:

- lote existente → ProductoLoteId + identificador temporal;
- lote nuevo → ProductoLoteId null + identificador temporal;
- serie → referencia al identificador temporal de lote.

No crear registros anticipadamente en PostgreSQL.

============================================================
14. REVISAR NOTIFICACIONES DEL COMMAND
============================================================

Es muy probable que el estado sea correcto pero:

RegistrarAjusteCommand

no esté reevaluando CanExecute después de modificar:

- Cantidad;
- Motivo;
- lotes;
- cantidad de lote;
- series;
- lote asociado a serie.

Auditar:

NotifyCanExecuteChangedFor

y/o llamadas a:

RegistrarAjusteCommand.NotifyCanExecuteChanged()

El comando debe reevaluarse inmediatamente cuando cambie cualquier propiedad que afecte su validez.

Revisar también cambios dentro de colecciones.

Agregar/Quitar elementos de ObservableCollection NO necesariamente notifica cambios en propiedades internas de cada elemento.

Si CanRegistrarAjuste depende de:

Lotes.All(...)
Series.All(...)

entonces suscribirse apropiadamente a PropertyChanged de los elementos o usar la estrategia existente del proyecto.

============================================================
15. PROPIEDADES CALCULADAS
============================================================

Auditar que cambios en:

Lote.Cantidad
Serie.Numero
Serie.LoteAsociado

notifiquen también correctamente:

CantidadLotesAsignada
LotesPendientes
CantidadSeriesRegistradas
SeriesPendientes
LotesValidos
SeriesValidas
CanRegistrarAjuste

o equivalentes reales.

No confiar únicamente en CollectionChanged.

============================================================
16. ESTADO DE ERROR DE CONTROLES
============================================================

Revisar si algún TextBox/ComboBox mantiene:

Validation.HasError = true

aunque el valor visual ya sea correcto.

Especial atención a:

- CostoPresentacion;
- CantidadPresentacion;
- Cantidad lote;
- Serie;
- Lote asociado.

Si existe validación vieja después de corregir el valor, limpiarla correctamente.

============================================================
17. CONDICIÓN FINAL ESPERADA
============================================================

Para el caso reproducido:

ENTRADA
LOTE_Y_SERIE

Bodega válida
Presentación válida
Cantidad = 2
CantidadBase = 2
Costo = 330
Motivo válido

Lotes:
PJB-2608-A = 2

Series:
SA-001 → PJB-2608-A
SA-002 → PJB-2608-A

Entonces:

LotesValidos = true
SeriesValidas = true
DatosGeneralesValidos = true
IsSaving = false

y:

CanRegistrarAjuste = true

Por tanto:

REGISTRAR AJUSTE

debe habilitarse.

============================================================
18. NO EXIGIR OBSERVACIÓN
============================================================

Observación es opcional.

Probar también el mismo caso con:

Observacion = ""

El botón debe habilitarse igualmente.

============================================================
19. PRUEBAS AUTOMATIZADAS
============================================================

Agregar una prueba específica para reproducir exactamente el bug.

Caso:

ENTRADA LOTE_Y_SERIE

cantidad = 2

lote existente:
PJB-2608-A cantidad 2

series:
SA-001 → PJB-2608-A
SA-002 → PJB-2608-A

motivo válido
costo 330

Esperado:

CanRegistrarAjuste == true

Agregar además pruebas:

A.
Falta 1 serie
→ false

B.
Falta cantidad de lote
→ false

C.
Serie sin lote asociado
→ false

D.
Lote asignado 2, series 2 pero distribución incorrecta
→ false

E.
Observación vacía
→ true

F.
Cantidad 0
→ false

G.
Motivo vacío
→ false

H.
Costo válido
→ true

============================================================
20. PRUEBA DE REACTIVIDAD
============================================================

No comprobar solamente el valor final.

Probar también secuencia:

1. Crear ajuste inválido.
2. Agregar lote.
3. Cambiar cantidad lote a 2.
4. Agregar SA-001.
5. Asociar lote.
6. Agregar SA-002.
7. Asociar lote.

Después del último cambio:

RegistrarAjusteCommand.CanExecute

debe cambiar automáticamente de:

false

a:

true

sin necesidad de:

- cambiar de campo extra;
- cerrar/reabrir;
- modificar otra propiedad;
- recargar pantalla.

============================================================
21. NO CAMBIAR UI
============================================================

En esta tarea NO modificar:

- distribución;
- colores;
- tamaños;
- tablas;
- autocomplete;
- botones;
- footer;
- textos;
- flujo.

Solo corregir la lógica que determina la habilitación del botón.

============================================================
22. VALIDACIÓN TÉCNICA
============================================================

Ejecutar:

dotnet build KONTAXPRO.slnx
dotnet test
git diff --check

No generar migración.

============================================================
23. INFORME FINAL
============================================================

Informar específicamente:

1. Cuál era la condición que mantenía el botón deshabilitado.
2. Si era un problema de validación o de notificación CanExecute.
3. Si ProductoLoteId del autocomplete estaba correctamente sincronizado.
4. Si LoteAsociado de las series estaba correctamente sincronizado.
5. Qué cambios se hicieron.
6. Qué prueba reproduce el bug.
7. Confirmar que con:
   - lote 2/2
   - series 2/2
   - motivo válido
   - costo válido
   el botón queda habilitado.
8. Confirmar que Observación sigue siendo opcional.
9. Build.
10. Tests.

No modificar SALIDA.
No modificar modo NUEVO.
No hacer commit ni push.