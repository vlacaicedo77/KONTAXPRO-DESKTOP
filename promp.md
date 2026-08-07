Continuemos con KONTAXPRO Desktop.

Antes de declarar Proveedores V1 como finalizado, simplifica el módulo conforme a las siguientes decisiones funcionales definitivas.

Lee primero AGENTS.md, los documentos de /docs y revisa la implementación actual de Proveedores, Clientes y Terceros.

No rediseñes el módulo ni modifiques la integración GUIA/SIFAE, la identidad canónica o el funcionamiento global de Terceros.

## 1. Regla definitiva sobre condiciones de pago

Las condiciones de pago no pertenecen al proveedor.

Un mismo proveedor puede tener condiciones distintas en cada compra, por ejemplo:

- contado;
- crédito sin plazo definido;
- crédito a 30 días;
- crédito a 60 días;
- cuotas diferentes.

Por tanto, la condición de pago, plazo, fecha de vencimiento y observaciones de pago deberán definirse posteriormente en cada Compra.

Elimina de Proveedores V1:

- CréditoHabilitado.
- PlazoCreditoDias.
- ObservacionComercial.
- sección “Configuración comercial”.
- texto relacionado con la empresa activa.
- KPI “Sin crédito”.
- columna “Crédito”.
- columna “Config.”.
- filtros relacionados con crédito o configuración.
- acciones o indicadores “Configurar”.

Si existe una entidad ProveedorEmpresa cuya única finalidad es guardar estos campos, elimínala de manera segura junto con:

- entidades;
- configuraciones EF Core;
- DbSet;
- DTOs;
- servicios;
- métodos;
- ViewModels;
- bindings;
- pruebas;
- migración correspondiente.

Antes de eliminarla, confirma que no tenga otra responsabilidad real.

No elimines ni modifiques relaciones necesarias para otros módulos.

Las compras futuras contendrán:

- EmpresaId.
- ProveedorId.
- condición de pago.
- plazo.
- fecha de vencimiento.
- cuenta por pagar.

No implementes Compras en esta tarea.

## 2. Regla definitiva sobre Nombre comercial

Mantén la propiedad NombreComercial en la entidad y tabla Tercero.

No elimines la columna de la base de datos porque puede utilizarse posteriormente en otros módulos o integraciones.

En Proveedores V1:

- elimina Nombre comercial del formulario;
- no lo declares obligatorio;
- no lo valides para guardar;
- no lo incluyas en la tabla;
- no agregues un filtro específico;
- no permitas modificarlo manualmente desde Proveedores.

Si GUIA o SIFAE devuelven un nombre comercial:

- puede conservarse automáticamente en Tercero;
- no sobrescribas un valor existente con null, vacío o espacios;
- no debe ser necesario para considerar válida la consulta;
- no es obligatorio mostrarlo en la interfaz.

No elimines NombreComercial de los contratos compartidos si Clientes u otro módulo lo utiliza realmente.

Adapta únicamente Proveedores.

## 3. Formulario definitivo de Proveedores V1

El formulario debe contener solamente:

### Identificación

- Tipo fijo: RUC nacional.
- Número de RUC.
- botón Verificar.
- estado de verificación.
- fuente de verificación.
- Razón social.

### Datos de contacto

- Dirección.
- Correo electrónico.
- Teléfono / WhatsApp.

### Acciones

- Guardar proveedor.
- Cancelar.
- Cerrar.

Conserva:

- formulario lateral;
- encabezado;
- pie fijo;
- scroll vertical;
- estilos de Clientes y Productos;
- Light/Dark;
- diseño adaptable;
- mensajes;
- validaciones;
- consulta GUIA → SIFAE;
- registro manual cuando las fuentes no estén disponibles.

Aprovecha el espacio disponible sin dejar tarjetas vacías.

Puedes mostrar Datos de contacto en una tarjeta de ancho completo o utilizar una distribución adaptable coherente con ClienteFormView.

## 4. Vista principal definitiva

La tabla debe mostrar:

- RUC.
- Razón social.
- Dirección.
- Teléfono.
- Email.
- Estado de verificación.
- Estado.
- Acciones.

No muestres:

- Nombre comercial.
- Crédito.
- Configuración empresarial.
- Plazo.

Mantén siempre visibles:

- RUC.
- Razón social.
- Estado.
- Acciones.

## 5. KPIs

Utiliza indicadores realmente útiles para Proveedores V1:

- Proveedores activos.
- Pendientes de verificar.
- Sin correo electrónico.
- Sin contacto digital.

Define “Sin contacto digital” reutilizando exactamente la regla aplicada en Clientes, si corresponde.

No dupliques los mismos indicadores con significados casi iguales. Si “Sin correo” y “Sin contacto digital” resultan redundantes según la implementación actual, selecciona otra métrica objetiva basada en datos existentes, por ejemplo:

- Inactivos.

No inventes datos ni relaciones nuevas únicamente para llenar una tarjeta.

## 6. Comportamiento global

Proveedor continúa siendo global.

No agregues EmpresaId a Proveedor.

No filtres proveedores por empresa.

No se necesita una configuración empresarial en Proveedores V1.

La relación con una empresa se establecerá posteriormente mediante cada Compra.

Conserva:

- reutilización de Tercero;
- equivalencia cédula/RUC natural;
- múltiples perfiles Cliente y Proveedor;
- unicidad;
- seguridad;
- consulta oficial;
- estado de verificación.

## 7. Base de datos y migración

Revisa las migraciones actuales.

Crea una nueva migración solamente si las tablas o columnas de configuración comercial ya fueron incorporadas al modelo vigente.

No edites migraciones aplicadas.

La migración debe eliminar únicamente los elementos que hayan quedado sin utilidad por esta decisión.

No elimines:

- Proveedor;
- Tercero;
- identificaciones;
- identidad canónica;
- campos globales de contacto;
- NombreComercial en Tercero.

No ejecutes cambios destructivos sobre datos no relacionados.

## 8. Pruebas

Actualiza las pruebas de Proveedores para reflejar las reglas definitivas.

Elimina o adapta pruebas relacionadas exclusivamente con:

- crédito del proveedor;
- plazo del proveedor;
- configuración por empresa;
- observación comercial del proveedor.

Conserva y verifica pruebas de:

- RUC;
- GUIA;
- SIFAE;
- fallback;
- Tercero existente;
- Cliente que también pasa a ser Proveedor;
- equivalencia cédula/RUC natural;
- duplicados;
- creación;
- edición;
- activación;
- inactivación;
- búsqueda;
- filtros;
- paginación;
- cambio de empresa, únicamente para garantizar que el módulo siga estable y no conserve dependencias empresariales innecesarias.

Agrega una prueba que confirme que NombreComercial:

- no es obligatorio para crear Proveedor;
- permanece disponible en Tercero;
- no se elimina al editar un proveedor;
- no se sobrescribe con un valor externo vacío.

## 9. Documentación

Actualiza la documentación de implementación indicando:

- Proveedor es global.
- Proveedores V1 utiliza únicamente RUC nacional.
- Razón social es el nombre principal.
- NombreComercial permanece en Tercero, pero no se muestra ni edita en Proveedores V1.
- Las condiciones de pago pertenecen a cada Compra.
- No existe configuración comercial del proveedor por empresa en V1.
- Proveedores extranjeros y liquidaciones de compra permanecen fuera de alcance.

No declares todavía Proveedores V1 como auditado definitivamente si aún falta crear su documento final.

## 10. Validación final

Ejecuta:

dotnet restore
dotnet build
dotnet test

Verifica además:

1. Productos V1 continúa funcionando.
2. Clientes V1 continúa funcionando.
3. Proveedores crea y edita correctamente.
4. El formulario ya no muestra Nombre comercial.
5. El formulario ya no muestra Configuración comercial.
6. La tabla ya no muestra Crédito ni Config.
7. Los filtros empresariales fueron retirados.
8. No existen bindings rotos.
9. No existen columnas vacías.
10. No existen cambios pendientes del modelo EF sin migración.
11. No hay secretos.
12. No hay datos personales reales en pruebas.
13. Light y Dark funcionan.
14. El diseño adaptable sigue funcionando.

## 11. Restricciones

No implementes:

- Compras.
- Cuentas por pagar.
- condiciones de pago.
- plazos predeterminados.
- configuración empresarial del proveedor.
- proveedores extranjeros.
- liquidaciones de compra.
- retenciones.
- historial de compras.

No hagas commit ni push.

## 12. Reporte final

Entrega:

- análisis de la estructura eliminada;
- campos y componentes retirados;
- confirmación de que NombreComercial permanece en Tercero;
- archivos modificados;
- archivos eliminados, si existen;
- migración creada, si corresponde;
- pruebas adaptadas;
- resultado de restore;
- resultado de build;
- resultado de test;
- resultado de revisión EF Core;
- comportamiento final de Proveedores V1;
- aspectos que permanecen fuera de alcance.

Corrige únicamente estas decisiones y conserva estable el resto del módulo.