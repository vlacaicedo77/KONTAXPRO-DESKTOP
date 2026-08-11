# OPERACIONES SIN SUSTENTO V1

## Administración independiente

El acceso oficial está en `Caja y Bancos > Operaciones sin comprobante`. Ya no
forma parte de Compras: una compra representa exclusivamente una operación con
comprobante tributario válido.

La pantalla conserva el lenguaje visual de Compras: encabezado, tarjetas KPI,
búsqueda, filtros, lista principal, estado vacío y paneles internos. Sus
acciones son Nueva, Ver, Corregir y Anular.

Corregir no sobrescribe el histórico: revierte la operación confirmada y crea
una nueva vinculada mediante `operacion_sustituida_id`, todo en una transacción.
Anular crea reversos de caja o banco, asiento e inventario; nunca elimina una
operación confirmada.

Si la sesión original de caja ya cerró, el reverso se registra en una sesión
actual abierta de la misma caja; el cierre histórico no se altera. La anulación
se bloquea cuando no hay una sesión actual segura, el período contable está
cerrado, el movimiento ya fue revertido, existen lotes reservados/modificados,
movimientos posteriores, costo promedio alterado o series ya utilizadas.

## Propósito

Este flujo registra salidas reales de recursos que no cuentan con un comprobante tributario válido. No crea una Compra, no genera crédito tributario de IVA y no presenta el valor como gasto deducible.

Principio funcional:

> El sistema conserva la realidad de Caja, Banco, Contabilidad e Inventario sin convertir un documento inexistente en sustento tributario.

## Modalidades

### Gasto

Casos típicos: alimentación del personal, materiales de limpieza o pagos informales sin factura.

Efectos confirmados en una sola transacción:

- registra `operaciones_sin_sustento` y su detalle;
- genera salida de Caja o Banco;
- debita la cuenta de gasto elegida;
- acredita la cuenta contable del fondo;
- marca estructuralmente la operación como no deducible;
- registra auditoría;
- conserva soporte PDF/JPG/PNG opcional.

No genera:

- Compra;
- Documento recibido SRI;
- Cuenta por pagar;
- impuesto de compra;
- crédito tributario;
- movimiento de inventario.

### Adquisición de inventario

Casos típicos: mercadería comprada sin factura que sí debe controlarse físicamente.

Efectos confirmados en una sola transacción:

- salida de Caja o Banco;
- entrada en bodega no facturable;
- Kardex, existencia y costo promedio;
- lotes, caducidad y series según la configuración del producto;
- débito a la cuenta configurada `INVENTARIO`;
- crédito a la cuenta contable del fondo;
- auditoría y soporte opcional;
- clasificación no deducible y sin IVA crédito.

Una adquisición sin sustento nunca puede entrar en una bodega habilitada para venta facturada. Esta separación evita que mercadería sin respaldo llegue accidentalmente al circuito tributario normal.

## Interfaz

Se accede desde Compras mediante el botón `Sin sustento`, pero la ventana pertenece conceptualmente a Tesorería.

La cabecera informa de forma permanente:

- `NO DEDUCIBLE`;
- `SIN IVA CRÉDITO`.

El usuario elige:

- Gasto o Inventario;
- Caja o Banco;
- fecha;
- beneficiario/responsable;
- motivo obligatorio;
- referencia opcional;
- soporte opcional.

Para Gasto solo se muestran cuentas deudoras y auxiliares pertenecientes a la
raíz configurada `GASTOS_NO_DEDUCIBLES`. Para Inventario se busca la presentación
del producto, se indica cantidad y costo total, y se configura lote o series
cuando corresponde.

## Reglas de integridad

- Solo opera dentro de la empresa y establecimiento activos.
- Se valida en servidor la asignación usuario-empresa-establecimiento.
- Caja exige una sesión abierta del establecimiento.
- Banco exige una cuenta activa de la empresa.
- Debe existir un período contable abierto para la fecha.
- El asiento automático debe quedar balanceado.
- El fondo de salida no puede ser simultáneamente la cuenta debitada.
- Inventario exige producto, presentación comprable, bodega no facturable y cantidad positiva.
- Lotes y series reutilizan las mismas reglas de ingreso del inventario general;
  un lote similar requiere confirmación explícita.
- Una corrección precarga el control original y solo puede reutilizar las series
  pertenecientes a la operación sustituida.
- El total se redondea por línea a dos decimales.
- El soporte admite PDF, JPG, JPEG o PNG, máximo 10 MB, con nombre original,
  SHA-256 y ruta relativa; al recuperarlo se verifica tamaño y huella antes de exportar.
- Los permisos son granulares: `TESORERIA_VER_SIN_SUSTENTO`,
  `TESORERIA_REGISTRAR_SIN_SUSTENTO`, `TESORERIA_CORREGIR_SIN_SUSTENTO`,
  `TESORERIA_ANULAR_SIN_SUSTENTO` y `PRODUCTOS_CONFIGURAR_PRECIOS`.
- El rol Administrador recibe el permiso mediante `StructuralSeeder`.
- El catálogo usa paginación real y búsqueda con debounce y cancelación.
- Las líneas repetidas del mismo producto se procesan en transacción serializable
  y acumulan stock/costo línea por línea.
- Un producto creado desde el flujo queda como borrador inactivo hasta confirmar;
  al cancelar o limpiar se descarta si aún no posee historia.

## Persistencia y trazabilidad

Tablas nuevas:

- `s_tesoreria.operaciones_sin_sustento`;
- `s_tesoreria.operaciones_sin_sustento_detalles`.

Vínculos explícitos del encabezado:

- `movimiento_caja_id` o `movimiento_bancario_id`;
- `movimiento_inventario_id` para la modalidad Inventario;
- `asiento_id`;
- usuario, establecimiento y empresa;
- evidencia con ruta relativa, tamaño y SHA-256.

Catálogos incorporados:

- documento interno `OPERACION_SIN_SUSTENTO / OSS`;
- movimiento de inventario `ADQUISICION_SIN_SUSTENTO / ENTRADA`;
- origen de inventario `OPERACION_SIN_SUSTENTO`;
- origen contable `OPERACION_SIN_SUSTENTO`.

## Separación respecto de Compras

`compras.tipo_compra` queda restringido a `FACTURADA`. La compra manual muestra `CON SUSTENTO · FACTURA` y exige comprobante Factura `01` con número válido.

La existencia histórica del literal `SIN_FACTURA` en migraciones anteriores no representa una capacidad vigente. La migración `AddUnsupportedOperations` actualiza las restricciones de la tabla para impedir nuevos registros con ese tipo.

## Archivos principales

- `KONTAXPRO.Domain/Entities/Tesoreria/OperacionSinSustento.cs`
- `KONTAXPRO.Application/Tesoreria/OperacionSinSustentoRules.cs`
- `KONTAXPRO.Application/Interfaces/IOperacionSinSustentoService.cs`
- `KONTAXPRO.Application/Models/Tesoreria/OperacionesSinSustentoDtos.cs`
- `KONTAXPRO.Infrastructure/Tesoreria/OperacionSinSustentoService.cs`
- `KONTAXPRO.Infrastructure/Tesoreria/SoporteSinSustentoFileStorage.cs`
- `KONTAXPRO.Infrastructure/Persistence/Configurations/OperacionesSinSustentoConfiguration.cs`
- `KONTAXPRO.Desktop/ViewModels/Tesoreria/OperacionSinSustentoViewModel.cs`
- `KONTAXPRO.Desktop/Views/Tesoreria/OperacionSinSustentoWindow.xaml`
- `KONTAXPRO.Tests/Tesoreria/OperacionSinSustentoRulesTests.cs`

## Prueba manual práctica

### Ejemplo 1: almuerzo sin factura

1. Abrir Compras y pulsar `Sin sustento`.
2. Elegir `Gasto` y `Caja`.
3. Indicar beneficiario `RESTAURANTE DEL BARRIO`.
4. Motivo `ALMUERZO DEL PERSONAL`.
5. Elegir una cuenta de gasto y registrar USD 18,50.
6. Adjuntar una fotografía del recibo si existe.
7. Confirmar.
8. Comprobar salida de Caja y asiento Débito gasto / Crédito caja.
9. Comprobar que no existe una nueva Compra, CxP ni IVA crédito.

### Ejemplo 2: mercadería sin factura

1. Abrir `Sin sustento` y elegir `Inventario`.
2. Elegir Caja o Banco y una bodega no facturable.
3. Buscar un producto/presentación, indicar cantidad 5 y costo total USD 25,00.
4. Configurar lote o series si el producto lo exige.
5. Confirmar.
6. Comprobar salida del fondo, entrada de 5 unidades base, Kardex y actualización del costo promedio.
7. Comprobar que el asiento debita Inventario y acredita Caja/Banco.
8. Comprobar que no existe Compra, CxP ni crédito tributario.
