# Control de diferencias de mercadería

## Estado

Propuesta para análisis e implementación futura. No forma parte de la versión actual y no autoriza cambios en base de datos, dominio, contabilidad, inventario ni facturación.

## Necesidad identificada

Existe un segmento de negocios en el que la mercadería entregada por el proveedor no coincide con la descrita en el comprobante recibido. Ejemplo:

- el comprobante documenta el producto X;
- físicamente se recibe el producto Y;
- X tiene sustento documental, pero no fue recibido;
- Y existe físicamente, pero puede carecer de un comprobante que lo describa correctamente.

El negocio necesita controlar simultáneamente el documento, la existencia física, la salida de dinero y la diferencia pendiente de regularización.

## Decisión conceptual

No modelar esta necesidad mediante una “bodega de fantasía”. En KONTAXPRO una bodega representa existencias físicas y un movimiento de inventario afirma que la mercadería existe. Un producto no inventariable tampoco puede considerarse contenido en una bodega, porque no genera existencias ni Kardex.

La propuesta es separar:

1. **Realidad documental:** lo declarado en el comprobante recibido.
2. **Realidad física:** lo efectivamente recibido y disponible.
3. **Conciliación:** explicación, seguimiento y regularización de la diferencia.

Las cantidades documentales no recibidas no serán stock disponible y no podrán utilizarse para simular una transferencia de bienes. Los comprobantes de venta deben respaldar las transacciones realmente efectuadas, conforme a la información oficial del [SRI sobre comprobantes de venta](https://www.sri.gob.ec/web/intersri/facturacion-fisica).

## Módulo futuro propuesto

Nombre funcional sugerido: **Diferencias de recepción** o **Mercadería no conciliada**.

Debe ser una función opcional por empresa, con permisos específicos, auditoría append-only y activación consciente por un administrador.

### Clasificaciones iniciales

- COINCIDE
- PRODUCTO_SUSTITUIDO
- FALTANTE
- SOBRANTE
- DESCRIPCION_INCORRECTA
- CANTIDAD_INCORRECTA
- MERCADERIA_SIN_SUSTENTO
- DOCUMENTO_SIN_RECEPCION
- PENDIENTE_REGULARIZACION
- REGULARIZADA

Estos códigos son preliminares y deberán validarse antes de incorporarlos al dominio.

## Flujo propuesto

1. Registrar o importar el comprobante recibido sin alterar su contenido.
2. Registrar la recepción física con los productos y cantidades realmente entregados.
3. Comparar líneas documentales y líneas físicas.
4. Confirmar coincidencias o clasificar diferencias.
5. Ingresar al inventario únicamente la mercadería física.
6. Mantener los productos documentados pero no recibidos fuera de existencias y Kardex.
7. Registrar el tratamiento financiero o contable en una cuenta transitoria definida por configuración y revisión profesional.
8. Regularizar mediante el documento o evento correspondiente: factura corregida, nota de crédito, devolución, reemplazo u otra solución válida.
9. Conservar trazabilidad entre documento original, recepción, diferencia y regularización.

## Relación con módulos existentes

### Compras

- El XML o comprobante manual conserva exactamente la información tributaria recibida.
- La recepción representa lo realmente entregado.
- Una diferencia impide que las líneas no recibidas creen existencias.
- La compra y su tratamiento contable deberán distinguir valores aceptados, observados y pendientes de regularización.

### Operaciones sin comprobante

- Puede registrar la salida de caja y el ingreso físico de mercadería sin sustento cuando corresponda al flujo aprobado.
- No convierte automáticamente esa mercadería en gasto deducible ni en crédito tributario.
- Una diferencia podrá enlazarse con la operación que originó el ingreso físico, sin fusionar ambos documentos.

### Inventario y Kardex

- Solo reflejan cantidades físicas confirmadas.
- No se crean existencias documentales ficticias.
- Debe conservarse el origen y nivel de sustento de cada entrada cuando el modelo futuro lo permita.

### Contabilidad y tributación

- Los valores no conciliados requieren cuentas transitorias y reglas configurables.
- No se debe asumir automáticamente deducibilidad, crédito tributario ni costo fiscal.
- La definición final debe revisarse con un profesional contable y tributario ecuatoriano antes de implementarse.

### Ventas y facturación electrónica

- La factura debe describir los bienes o servicios realmente transferidos.
- Un documento de compra de X no habilita por sí mismo una venta documental de X si no hubo transferencia real.
- Los productos no inventariables se reservan para operaciones reales que legítimamente no requieren control de stock.

## Información mínima de una diferencia

Modelo conceptual pendiente de diseño definitivo:

- empresa y establecimiento;
- proveedor y comprobante recibido;
- línea documental de origen;
- producto y cantidad documentados;
- producto y cantidad físicamente recibidos;
- tipo, estado y fecha de la diferencia;
- valor observado;
- usuario responsable;
- explicación sin datos sensibles innecesarios;
- evidencias o referencias externas, si se habilitan;
- documento o evento de regularización;
- historial append-only.

No se aprueban todavía tablas ni campos nuevos. La persistencia se diseñará cuando se priorice el módulo y se confirme la inconsistencia funcional.

## Reportes esperados

- comprobantes pendientes de conciliación;
- mercadería recibida sin sustento correcto;
- mercadería facturada pero no recibida;
- diferencias por proveedor y antigüedad;
- valores en cuentas transitorias;
- regularizaciones realizadas;
- stock físico clasificado por origen o nivel de sustento, sin confundirlo con una bodega física.

## Controles obligatorios

- aislamiento estricto por empresa;
- acceso solo a establecimientos autorizados;
- permisos separados para registrar, aprobar y regularizar;
- ninguna diferencia crea stock por sí sola;
- ninguna regularización sobrescribe documentos originales;
- operaciones confirmadas se corrigen mediante reversos o documentos vinculados;
- auditoría de creación, aprobación, cambio de estado y regularización;
- prohibición de usar cantidades documentales como disponibilidad de venta;
- mensajes claros que distingan control operativo, contable y tributario.

## Decisiones pendientes

- nombre definitivo del módulo y ubicación en el menú;
- estados y transiciones autorizadas;
- cuentas contables transitorias;
- relación con cuentas por pagar y pagos al proveedor;
- tratamiento de costos y del IVA en cada escenario;
- soporte de regularizaciones parciales;
- evidencia requerida para confirmar recepción;
- alcance de reportes para usuarios no contables;
- condiciones legales aplicables a liquidaciones de compra;
- política de retención documental y adjuntos.

## Criterios para una futura implementación

La propuesta podrá avanzar cuando:

1. el flujo haya sido validado con casos reales anonimizados;
2. exista revisión contable y tributaria ecuatoriana;
3. se definan permisos y responsabilidades;
4. se garantice que inventario representa únicamente existencias físicas;
5. las ventas continúen representando transferencias reales;
6. exista una estrategia de pruebas transaccionales y de auditoría;
7. entidades, configuraciones EF Core y una `InitialCreate` limpia reflejen el modelo aprobado.

