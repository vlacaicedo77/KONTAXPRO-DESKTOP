# Despachos a sucursales — diseño futuro

## Estado

Documento de arquitectura para una implementación posterior. Este módulo no
forma parte de Inventario V1 y todavía no está implementado.

## Objetivo

Permitir que una instalación matriz entregue mercadería a una sucursal que
trabaja con otra instalación y otra base PostgreSQL, utilizando un archivo
portable para registrar la salida en el origen y la recepción en el destino.

No debe confundirse con:

- una transferencia entre bodegas de la misma base de datos;
- la corrección de una bodega equivocada en el inventario inicial;
- una venta comercial que genere ingresos, cartera o impuestos.

## Principios aprobados

1. La matriz y la sucursal pueden operar con bases de datos independientes.
2. Los IDs `BIGINT` son locales y nunca se usan para relacionar instalaciones.
3. Productos y presentaciones se identifican mediante sus UUID portables.
4. La salida y la recepción conservan un UUID común de despacho.
5. Un archivo solo puede importarse una vez en la instalación de destino.
6. La importación se confirma en una sola transacción.
7. Una recepción parcial no debe marcar el despacho como recibido totalmente.
8. Las operaciones confirmadas no se eliminan: se anulan o revierten.
9. El tratamiento tributario y contable se definirá según la relación jurídica
   entre origen y destino; no se inferirá automáticamente como una venta.

## Flujo propuesto

### En la matriz

1. Crear un despacho con sucursal destino y bodega de origen.
2. Seleccionar productos, presentaciones, cantidades, lotes y series.
3. Validar disponibilidad y reservas.
4. Confirmar la salida de inventario.
5. Generar el archivo portable firmado o protegido mediante hash.
6. Marcar el despacho como `EXPORTADO`.

### En la sucursal

1. Examinar o arrastrar el archivo recibido.
2. Validar formato, versión, integridad, empresa y sucursal destino.
3. Detectar si el UUID del despacho ya fue importado.
4. Resolver productos y presentaciones por UUID.
5. Informar cualquier maestro inexistente o incompatible.
6. Seleccionar la bodega física de recepción cuando corresponda.
7. Confirmar la entrada de inventario en una sola transacción.
8. Guardar el UUID del despacho origen y marcarlo como `RECIBIDO`.

## Archivo portable

Formato recomendado: JSON UTF-8 dentro de un contenedor con extensión propia,
por ejemplo `.kontax-transfer`.

Contenido mínimo:

- versión del formato;
- UUID del despacho;
- UUID de la instalación origen;
- identificación estable de la empresa y código del establecimiento origen;
- UUID de instalación o código portable de la sucursal destino;
- número y fecha del despacho;
- usuario que confirmó;
- productos y presentaciones identificados por UUID;
- cantidades de presentación, factor histórico y cantidad base;
- lotes, fechas de elaboración y caducidad;
- series y su lote relacionado;
- costo unitario base histórico cuando la política lo requiera;
- observación;
- fecha de generación;
- hash de integridad;
- firma digital del archivo, si se adopta intercambio no confiable.

El archivo no debe contener IDs locales, credenciales, cadenas de conexión ni
otros secretos de la instalación.

## Idempotencia

La sucursal debe guardar una identidad de importación con restricción única,
como mínimo:

```text
(instalacion_origen_uuid, despacho_uuid)
```

Si el archivo ya fue procesado, la aplicación mostrará el resultado existente
y no volverá a incrementar existencias.

## Estados sugeridos

### Origen

- `BORRADOR`
- `CONFIRMADO`
- `EXPORTADO`
- `ANULADO`

### Destino

- `PENDIENTE_IMPORTACION`
- `VALIDADO`
- `RECIBIDO_PARCIAL`
- `RECIBIDO`
- `RECHAZADO`
- `ANULADO`

## Inventario

La matriz genera un movimiento `TRANSFERENCIA_SALIDA` o un tipo específico de
despacho remoto. La sucursal genera la entrada correspondiente únicamente al
confirmar la recepción.

El movimiento del destino conserva:

- UUID del despacho origen;
- documento y fecha originales;
- cantidades y factor histórico;
- lotes y series;
- usuario receptor;
- archivo o hash que dio origen a la importación.

La salida y la entrada no comparten una transacción de base de datos porque
ocurren en instalaciones independientes. La consistencia se obtiene mediante
UUID, estados, idempotencia y conciliación.

## Catálogo portable

Antes de habilitar el intercambio se debe comprobar que:

- cada producto tenga UUID único;
- cada presentación tenga UUID único;
- la presentación pertenezca al producto indicado;
- las unidades y factores sean compatibles en ambas instalaciones;
- lotes y series no entren en conflicto con registros existentes.

Si un UUID no existe en la sucursal, V1 del intercambio debería detener la
línea y solicitar resolución explícita. No debe crear productos silenciosamente.

## Contabilidad y tributación

El despacho remoto es una operación logística. No debe generar por sí solo:

- ventas;
- cuentas por cobrar;
- caja o banco;
- IVA por pagar;
- ingresos contables.

Si matriz y sucursal corresponden a sujetos jurídicos diferentes, el flujo
comercial y tributario deberá originarse en los módulos de Ventas y Compras y
el despacho solo servirá como trazabilidad física relacionada.

## Seguridad y auditoría

- validar que el usuario tenga permiso para despachar o recibir;
- registrar instalación, empresa, establecimiento y usuario;
- auditar generación, exportación, importación, rechazo y anulación;
- comprobar el hash antes de mostrar o procesar el contenido;
- impedir cambios manuales en un archivo ya confirmado;
- no confiar en nombres o códigos cuando exista UUID portable.

## Relación con Inventario V1

Inventario V1 expondrá únicamente la corrección controlada de bodega para
existencias provenientes del inventario inicial. Esa operación ocurre dentro
de una misma base de datos y no genera archivos.

El futuro módulo `Despachos a sucursales` tendrá navegación, permisos, estados,
administración y documentación propios.
