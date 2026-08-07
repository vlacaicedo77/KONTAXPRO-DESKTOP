# Proveedores V1

## 1. Propósito del documento

Este documento conserva el diseño funcional y técnico actualmente implementado para **Proveedores V1** en KONTAXPRO Desktop. Su objetivo es servir como memoria del módulo, guía de mantenimiento y contrato para los módulos que posteriormente consuman proveedores, especialmente Compras, Cuentas por Pagar, Retenciones y Contabilidad.

Fecha de revisión: **6 de agosto de 2026**.

La documentación describe el código existente al momento de la revisión. No representa una propuesta de rediseño ni modifica el comportamiento de la aplicación.

---

## 2. Resumen ejecutivo

Proveedores V1 es un catálogo global de terceros que tienen habilitado el rol de proveedor.

Decisiones esenciales:

- No existe una entidad ni una tabla independiente `Proveedor`.
- Un proveedor es un `Tercero` con `EsProveedor = true`.
- El tercero y sus datos de identidad y contacto son globales para toda la instalación.
- El catálogo no se duplica por empresa y se mantiene idéntico al cambiar de empresa.
- La empresa activa sigue siendo obligatoria para autorizar el acceso y contextualizar la auditoría.
- Los roles Cliente y Proveedor pueden coexistir sobre la misma persona o contribuyente.
- Los estados de ambos roles son independientes: `EstadoCliente` y `EstadoProveedor`.
- El registro V1 acepta únicamente RUC ecuatoriano de 13 dígitos.
- La existencia del RUC se consulta en fuentes oficiales mediante GUIA y SIFAE.
- Solo se permite ingreso manual cuando las fuentes oficiales no están disponibles después de tres intentos.
- Las mutaciones requieren permiso y vuelven a validarlo en la capa de infraestructura.
- Se usa concurrencia optimista con `xmin` de PostgreSQL.
- Las altas, cambios, verificaciones y cambios de estado se auditan dentro de la misma transacción.
- No existe eliminación física desde el módulo; se activa o inactiva exclusivamente el rol Proveedor.

---

## 3. Alcance funcional de V1

El módulo permite:

1. Consultar el catálogo global de proveedores.
2. Buscar por RUC, razón social, correo o teléfono.
3. Filtrar por estado y condición de verificación.
4. Aplicar filtros rápidos mediante KPIs.
5. Crear un proveedor a partir de un tercero nuevo.
6. Habilitar como proveedor a un tercero que ya existe como cliente.
7. Consultar automáticamente un RUC en las fuentes oficiales.
8. Registrar manualmente al proveedor solo después de tres fallos técnicos de las fuentes.
9. Editar los datos permitidos.
10. Verificar posteriormente un proveedor creado en modo offline.
11. Activar o inactivar el rol Proveedor sin alterar el rol Cliente.
12. Detectar modificaciones concurrentes y evitar sobrescrituras silenciosas.
13. Auditar las operaciones relevantes.

### Fuera del alcance de V1

No están implementados en este módulo:

- Proveedores extranjeros, pasaporte u otros documentos distintos de RUC ecuatoriano.
- Condiciones comerciales del proveedor por empresa.
- Crédito, plazo de pago, cupo o saldo del proveedor.
- Compras, devoluciones, liquidaciones de compra o historial de compras.
- Cuentas por pagar, pagos o anticipos.
- Configuración de retenciones.
- Información bancaria del proveedor.
- Contactos múltiples, sucursales o direcciones múltiples.
- Eliminación física de proveedores.

Estos datos deben incorporarse en sus módulos funcionales o relaciones por empresa cuando exista una necesidad real. No deben añadirse al maestro global solo para anticipar funcionalidad futura.

---

## 4. Modelo conceptual

### 4.1 Tercero global

`Tercero` representa la identidad compartida de una persona o contribuyente. Conserva:

- identificación principal;
- clave canónica de identidad;
- razón social;
- nombre comercial, cuando llega desde una fuente oficial;
- dirección, correo y teléfono;
- origen y estado de verificación;
- roles globales de Cliente y Proveedor;
- estado independiente de cada rol;
- estado general;
- fechas de creación y actualización;
- versión de concurrencia.

El diseño evita registrar dos veces al mismo contribuyente cuando participa simultáneamente como cliente y proveedor.

### 4.2 Roles coexistentes

Los siguientes escenarios son válidos:

| EsCliente | EsProveedor | Interpretación |
|---:|---:|---|
| Sí | No | Solo cliente |
| No | Sí | Solo proveedor |
| Sí | Sí | Cliente y proveedor |

`EstadoCliente` y `EstadoProveedor` son independientes. Inactivar al proveedor no inactiva al cliente ni elimina el tercero.

### 4.3 Relación con empresas

`EmpresaTercero` continúa existiendo como relación neutral para configuración comercial por empresa y para referencias operativas futuras. Proveedores V1 no la utiliza como dueño del proveedor ni crea una relación por empresa al registrar un proveedor.

Implicaciones:

- el proveedor aparece en el catálogo desde cualquier empresa autorizada;
- los cambios de identidad y contacto son visibles globalmente;
- el estado del rol Proveedor también es global;
- las futuras operaciones deben guardar la empresa y referenciar la relación apropiada según el modelo definitivo de Compras;
- cualquier configuración realmente específica de una empresa debe vivir en una relación empresarial, nunca duplicando al tercero.

---

## 5. Persistencia

### 5.1 Tabla `s_comercial.terceros`

Campos relevantes para Proveedores:

- `id`
- `tipo_identificacion_id`
- `numero_identificacion`
- `clave_identidad`
- `razon_social`
- `nombre_comercial`
- `direccion`
- `correo`
- `telefono`
- `origen_registro`: `OFICIAL` o `OFFLINE`
- `estado_verificacion`: `PENDIENTE` o `VERIFICADO`
- `fuente_verificacion`
- `verificado_at`
- `es_cliente`
- `estado_cliente`
- `es_proveedor`
- `estado_proveedor`
- `estado`
- `created_at`
- `updated_at`
- `xmin`, usado por EF Core como versión de concurrencia

Restricciones importantes:

- identificación principal única por tipo y número;
- `clave_identidad` única;
- estados limitados a 0 o 1;
- origen y estado de verificación limitados a sus códigos válidos;
- Consumidor Final protegido por restricción de base de datos.

### 5.2 Tabla `s_comercial.terceros_identificaciones`

Permite conservar más de un documento para un tercero, algo necesario para asociar correctamente cédula y RUC de una persona natural.

Campos principales:

- `id`
- `tercero_id`
- `tipo_identificacion_id`
- `numero_identificacion`
- `numero_normalizado`
- `es_principal`
- `estado_verificacion`
- `fuente_verificacion`
- `verificado_at`
- `estado`
- timestamps

Garantías:

- el tipo y número normalizado son únicos globalmente;
- solo puede existir una identificación principal por tercero;
- las relaciones usan `DeleteBehavior.Restrict`;
- cada identificación conserva su propia evidencia de verificación.

### 5.3 Tabla `s_comercial.empresas_terceros`

No contiene los roles globales Cliente o Proveedor. Conserva configuración empresarial como lista de precios, crédito de cliente, cupo, días de crédito, observación y estado de la relación.

Su índice único es `(empresa_id, tercero_id)`.

### 5.4 Consumidor Final

El documento `9999999999999` y la razón social `CONSUMIDOR FINAL` están protegidos. El registro se excluye del catálogo de proveedores y el servicio rechaza su uso como proveedor.

---

## 6. Identidad canónica y prevención de duplicados

El número escrito por el usuario no es la única regla para determinar identidad.

### Persona natural

Un RUC de persona natural se forma con una cédula válida de 10 dígitos más el sufijo `001`.

Ejemplo:

```text
Cédula: 1724853377
RUC:    1724853377001
Clave:  NAT:1724853377
```

Ambos documentos producen la misma clave canónica. Si el tercero ya existe como cliente con cédula, el registro como proveedor agrega o reutiliza su RUC y habilita `EsProveedor`; no crea otra persona.

### Otros RUC

Los RUC que no corresponden al patrón válido de persona natural usan una clave con la forma:

```text
RUC:{numero_normalizado}
```

### Barreras contra duplicados

La prevención ocurre en varios niveles:

1. normalización de entrada;
2. búsqueda por clave canónica;
3. búsqueda por identificación secundaria;
4. índices únicos de PostgreSQL;
5. traducción de conflictos de unicidad a un mensaje funcional.

No se debe reemplazar esta estrategia por una simple comparación textual del RUC.

---

## 7. Validación y normalización de datos

### 7.1 RUC

- obligatorio;
- exactamente 13 dígitos;
- se eliminan caracteres no numéricos antes de validar;
- V1 no aplica un algoritmo universal de existencia para todos los tipos de RUC;
- la existencia y los datos oficiales se confirman con GUIA o SIFAE.

Un formato inválido no inicia consultas externas y nunca habilita modo offline.

### 7.2 Razón social

- obligatoria al guardar;
- se completa desde la fuente oficial cuando la verificación tiene éxito;
- permanece bloqueada para un proveedor oficialmente verificado;
- puede escribirse manualmente cuando existe autorización offline;
- un nombre comercial oficial vacío nunca sobrescribe uno previamente almacenado.

### 7.3 Correo

- opcional en Proveedores V1;
- se normaliza a minúsculas;
- si la fuente devuelve varios correos separados por coma o punto y coma, se toma solo el primero;
- si el usuario proporciona uno, debe tener formato válido;
- no se guarda automáticamente `cliente@kontax.com` para proveedores.

### 7.4 Teléfono / WhatsApp

- opcional;
- la interfaz aplica una máscara de entrada;
- se normalizan números ecuatorianos, locales e internacionales;
- la representación persistida utiliza prefijo internacional cuando puede determinarse;
- el resultado válido contiene entre 8 y 15 dígitos.

### 7.5 Dirección

- opcional en el proveedor V1;
- puede completarse con el dato oficial disponible;
- se comparte globalmente con el tercero.

---

## 8. Verificación oficial

### 8.1 Fuentes y orden

La orquestación consulta:

1. **GUIA**;
2. **SIFAE**, si GUIA no entrega datos mínimos utilizables.

La tarjeta muestra la entidad funcional **SRI** y distingue el proveedor técnico con una insignia:

- `G`: GUIA;
- `F`: SIFAE.

### 8.2 Consulta GUIA

- obtiene un token con credenciales configuradas;
- consulta el RUC como contribuyente natural/jurídico según el contrato del servicio;
- ante HTTP 401 invalida el token, solicita uno nuevo y reintenta una vez;
- interpreta errores funcionales, incluido RUC no encontrado;
- obtiene razón social y el primer correo utilizable;
- errores de red, timeout o configuración se clasifican como indisponibilidad técnica.

### 8.3 Consulta SIFAE

Para proveedores se consultan en paralelo dos endpoints:

- ubicación/datos del contribuyente, para razón social y dirección;
- correo del contribuyente.

La razón social es el dato mínimo requerido. La ausencia de correo no convierte por sí sola una respuesta válida en fallo.

### 8.4 Priorización del resultado

Cuando ninguna fuente completa una verificación, el resultado final prioriza:

1. documento inválido;
2. datos oficiales incompletos;
3. documento no encontrado;
4. servicios no disponibles.

Esta distinción es importante: solo la indisponibilidad técnica puede conducir al modo offline.

### 8.5 Tres intentos y modo offline

Al completar un RUC localmente válido, el formulario inicia la consulta automáticamente. Ejecuta hasta tres rondas, con una pausa aproximada de 500 ms entre ellas.

| Resultado | Comportamiento |
|---|---|
| Verificado | Completa datos, bloquea razón social y enfoca Dirección |
| Inválido | Muestra estado rojo; no habilita modo offline |
| No encontrado | Muestra advertencia; no habilita modo offline |
| Datos incompletos | Informa el problema; no autoriza registro manual |
| No disponible, intentos 1–2 | Continúa automáticamente |
| No disponible, intento 3 | Emite constancia temporal y habilita ingreso manual |

La consulta puede cancelarse desde el botón rojo animado que aparece mientras está activa.

### 8.6 Constancia de verificación

La interfaz no puede declarar por sí sola que un documento está verificado o que agotó los intentos. El servicio de interoperabilidad emite una constancia temporal que el servicio de persistencia consume al guardar.

Propiedades de la constancia:

- reside en memoria;
- dura 10 minutos;
- está ligada al usuario, tipo de identificación, número normalizado y propósito `Proveedor`;
- es de un solo uso;
- una constancia de verificación oficial transporta los datos oficiales;
- una constancia offline solo aparece tras tres resultados técnicos no disponibles del mismo flujo;
- reiniciar la aplicación elimina las constancias pendientes.

Esto impide falsificar desde la UI un proveedor verificado o el acceso al modo manual.

### 8.7 Reintento al editar

Un proveedor pendiente creado offline puede abrirse en edición y verificarse posteriormente. Si la consulta tiene éxito, se actualizan su evidencia y datos oficiales conforme a las reglas de preservación.

Un proveedor ya verificado se carga localmente y no vuelve a consultar las fuentes de forma innecesaria al abrirse.

---

## 9. Servicio de aplicación y persistencia

La interfaz `IProveedorService` expone:

- listado paginado;
- obtención de detalle;
- búsqueda local por RUC;
- guardado;
- cambio de estado.

`ProveedorService` usa `IDbContextFactory<KontaxDbContext>` y crea un contexto por operación.

### 9.1 Consulta del catálogo

- exige sesión autenticada, empresa activa y relación activa usuario–empresa;
- consulta terceros globales con `EsProveedor = true`;
- excluye Consumidor Final;
- selecciona el RUC activo, prefiriendo la identificación principal;
- proyecta sin seguimiento mediante `AsNoTracking`;
- ordena por razón social y luego por RUC;
- pagina en servidor;
- admite tamaños 25, 50 y 100.

### 9.2 Búsqueda local por RUC

Busca por clave canónica y por identificaciones asociadas. Puede devolver un tercero que todavía no sea proveedor, permitiendo reutilizar un cliente existente sin duplicarlo.

### 9.3 Guardado

El flujo de guardado:

1. valida sesión, empresa y permiso;
2. valida y normaliza los datos;
3. abre una transacción;
4. localiza el tercero por identidad exacta o canónica;
5. valida consistencia del identificador en edición;
6. verifica `Version` para registros existentes;
7. rechaza Consumidor Final;
8. consume la constancia oficial u offline cuando corresponde;
9. crea o actualiza la identificación RUC;
10. preserva datos verificados cuando no existe nueva evidencia oficial;
11. habilita el rol `EsProveedor`;
12. actualiza únicamente `EstadoProveedor` para ese rol;
13. vuelve a validar que la empresa activa no haya cambiado durante la operación;
14. guarda la mutación y su auditoría en la misma transacción;
15. confirma la transacción.

Un tercero que ya es cliente conserva su documento, rol y estado de cliente.

### 9.4 Cambio de estado

- requiere la versión observada por el usuario;
- modifica solo `EstadoProveedor`;
- no altera `EstadoCliente`, la relación empresarial ni el estado general;
- registra la acción en auditoría;
- ante conflicto concurrente obliga a recargar.

---

## 10. Seguridad y autorización

### Lectura

Para consultar el catálogo se exige:

- usuario autenticado;
- empresa activa;
- vínculo activo en `usuarios_empresas`.

Aunque el catálogo sea global, la aplicación nunca lo expone fuera de una sesión empresarial válida.

### Mutaciones

Crear, editar y cambiar estado requieren `TERCEROS_GESTIONAR`. Inicialmente el rol `ADMINISTRADOR` posee este permiso.

La autorización se aplica en dos niveles:

- la UI oculta o deshabilita acciones no permitidas;
- el servicio vuelve a validar sesión, vínculo, rol y permiso contra la base.

La capa visual no constituye una barrera de seguridad.

### Cambio de empresa durante una operación

Si cambia la empresa activa con el formulario abierto:

- el formulario se cierra;
- se cancela la operación en curso;
- se advierte al usuario;
- una mutación valida nuevamente la empresa antes de confirmar.

---

## 11. Concurrencia y transacciones

`Tercero.Version` está configurado como `IsRowVersion()` y Npgsql lo mapea a `xmin` de PostgreSQL.

La versión viaja en los DTO de lista y detalle y debe regresar al editar o cambiar estado. Si otro usuario modificó el tercero desde que fue leído:

- PostgreSQL/EF detecta que la versión ya no coincide;
- el servicio no sobrescribe la modificación concurrente;
- devuelve un conflicto funcional;
- la interfaz recarga los datos antes de mostrar el aviso.

Las mutaciones complejas y sus auditorías comparten el mismo contexto y transacción. Si falla cualquiera de las dos partes, ambas se revierten.

---

## 12. Auditoría

Acciones registradas según el caso:

- creación del tercero proveedor;
- asignación del rol Proveedor a un tercero existente;
- actualización del proveedor;
- verificación oficial;
- activación;
- inactivación.

La auditoría guarda contexto técnico y funcional:

- usuario;
- empresa activa;
- establecimiento activo cuando existe;
- entidad `terceros` e identificador;
- acción y descripción no sensible;
- fecha.

Las descripciones no incluyen RUC, razón social, correo, teléfono ni otros datos personales. Operaciones rechazadas o conflictos de concurrencia no generan una auditoría de éxito.

---

## 13. Pantalla principal

### 13.1 Encabezado

- título: **Proveedores**;
- descripción: **Consulta y administra el catálogo global de proveedores**;
- icono asociado a proveedores/transporte;
- botón para crear un proveedor, condicionado por permiso.

### 13.2 KPIs con filtro

Se muestran cuatro tarjetas:

| KPI | Color | Regla |
|---|---|---|
| Activos | Azul | `EstadoProveedor = 1` |
| Pendientes de verificar | Ámbar | RUC sin verificación oficial |
| Sin correo | Rojo | Correo vacío o correo genérico reservado para clientes |
| Sin contacto digital | Morado | Sin correo y sin teléfono |

Al pulsar un KPI se aplica su filtro. Pulsar nuevamente el KPI activo regresa a Todos. Elegir un filtro avanzado limpia el KPI seleccionado.

Los contadores se calculan después del texto de búsqueda y antes de los filtros avanzados de estado/verificación.

### 13.3 Barra de búsqueda y filtros

- búsqueda por RUC, razón social, correo o teléfono;
- espera de 300 ms antes de consultar para evitar una búsqueda por cada tecla;
- botón `X` para limpiar;
- filtro de verificación: Todos, Verificados, No verificados;
- filtro de estado: Todos, Activos, Inactivos;
- botón Actualizar.

La dirección no forma parte del texto de búsqueda en V1.

### 13.4 Lista

La cuadrícula usa el lenguaje visual base de Productos y Clientes, sin una columna separada de verificación.

Columnas:

| Columna | Comportamiento visual |
|---|---|
| RUC | Número e icono de verificación; verde si está verificado y azul/advertencia si está pendiente; tooltip con fuente |
| Razón social | Columna principal, con mayor espacio |
| Dirección | Visible en anchos amplios |
| Teléfono | Visible en ancho medio |
| Email | Visible en anchos amplios |
| Estado | Badge completo ACTIVO/INACTIVO |
| Acciones | Editar y activar/inactivar según permiso |

Características de la cuadrícula:

- filas compactas de 58 px;
- encabezado de 42 px;
- columnas reajustables con separador visual oculto;
- no permite reordenar columnas;
- comportamiento adaptable para ocultar columnas secundarias;
- estado de carga;
- estado vacío;
- paginación anterior/siguiente;
- selector de 25, 50 o 100 registros.

Las cargas anteriores se cancelan y una respuesta tardía se descarta mediante una secuencia de carga, evitando que resultados antiguos reemplacen una búsqueda más reciente.

---

## 14. Formulario de proveedor

El formulario aparece como panel superpuesto dentro de la vista, con cuerpo desplazable y pie de acciones fijo.

### 14.1 Encabezado

- icono `TruckCheck`;
- título y descripción según modo nuevo/edición;
- botón de cierre con estilo de peligro.

### 14.2 Sección Identificación

- tipo fijo: **RUC Nacional**;
- caja de RUC limitada a 13 dígitos;
- entrada solo numérica;
- botón `X` para limpiar en modo nuevo;
- `Enter` permite verificar;
- verificación automática al completar un RUC válido;
- botón Verificar;
- botón rojo animado para cancelar durante la consulta;
- campo Razón social con indicador de bloqueo;
- tarjeta de estado de verificación alineada con la sección.

Estados visuales de la tarjeta:

- azul: pendiente o preparada;
- verde: verificado;
- ámbar: modo offline autorizado;
- rojo: inválido, no encontrado o error funcional.

La tarjeta evita duplicar el mismo mensaje y conserva fuente, intento actual y alertas necesarias.

### 14.3 Datos de contacto

Orden y controles:

1. Dirección.
2. Correo electrónico con conversión a minúsculas y autocompletado de dominios.
3. Teléfono / WhatsApp con máscara de entrada.

La cuadrícula usa tres columnas cuando hay espacio y se compacta de manera adaptativa en anchos menores.

### 14.4 Pie

- aviso de que el proveedor pertenece al catálogo global;
- botón Cancelar;
- botón Guardar;
- indicador de operación en curso;
- acciones deshabilitadas cuando el formulario está ocupado o el usuario carece de permiso.

### 14.5 Foco y navegación

- al crear, el foco comienza en RUC;
- al elegir/limpiar la identificación, vuelve a RUC;
- después de verificación exitosa, pasa a Dirección;
- después de habilitarse modo offline, pasa a Razón social;
- el formulario lleva al campo correspondiente cuando una validación falla.

### 14.6 Edición

- el RUC no se cambia desde edición;
- la razón social de un registro verificado permanece bloqueada;
- un registro pendiente permite edición manual y ofrece Verificar;
- cancelar con datos presentes solicita confirmación;
- V1 no mantiene un seguimiento granular de campos sucios, por lo que la confirmación puede aparecer aunque no haya una diferencia real.

### 14.7 Adaptabilidad y tema

- usa recursos dinámicos del tema;
- compatible con modo claro/oscuro;
- la identificación cambia a una columna en modo compacto;
- contacto pasa de tres columnas a distribución compacta;
- el contenido puede desplazarse, manteniendo visibles las acciones finales.

El code-behind se limita a comportamiento visual: foco, desplazamiento, restricción numérica y suscripción a eventos. La lógica funcional permanece en ViewModels y servicios.

---

## 15. ViewModels y comportamiento asíncrono

### `ProveedoresViewModel`

Responsabilidades:

- carga y paginación;
- búsqueda diferida;
- filtros y KPIs;
- apertura del formulario;
- comandos de edición y estado;
- confirmaciones y notificaciones;
- reacción al cambio de empresa;
- cancelación y descarte de respuestas tardías;
- liberación de suscripciones mediante `Dispose`.

Configuración inicial:

- estado: Todos;
- verificación: Todos;
- KPI activo: Activos;
- página: 1;
- tamaño: 25.

### `ProveedorFormViewModel`

Responsabilidades:

- modos nuevo/edición;
- validación local;
- consulta local previa;
- orquestación visual de los tres intentos;
- estados de verificación, cancelación y modo offline;
- recepción de datos oficiales;
- reglas de edición y foco;
- creación de la solicitud de guardado;
- tratamiento del conflicto concurrente;
- cierre y notificación al catálogo.

Las consultas mantienen su propio `CancellationTokenSource`. La cancelación solicitada por el usuario se distingue de un fallo técnico de las fuentes.

---

## 16. Integración y configuración

### Registro de dependencias

- `IProveedorService` → `ProveedorService`, transitorio.
- ViewModels y vistas de Proveedores, transitorios.
- servicio de consulta y almacén de constancias, singleton.
- clientes HTTP de GUIA y SIFAE con timeout configurado.
- plantilla WPF que relaciona `ProveedoresViewModel` con `ProveedoresView`.
- navegación integrada en `NavigationService`, que libera el ViewModel anterior cuando corresponde.

### Configuración externa

GUIA obtiene URLs desde configuración y credenciales desde configuración local o variables de entorno:

```text
KONTAXPRO_GUIA_CLIENT_ID
KONTAXPRO_GUIA_CLIENT_SECRET
```

SIFAE obtiene su URL base desde configuración. La conexión PostgreSQL usa:

```text
KONTAXPRO_CONNECTION_STRING
```

No se deben documentar ni versionar los valores de secretos.

---

## 17. Migraciones relacionadas

La evolución que produjo el modelo actual está representada por:

- `20260806004947_AddCanonicalIdentityKeyToTerceros`
- `20260806020027_AddSuppliersV1AndThirdPartyIdentifications`
- `20260806034059_MakeSuppliersGlobal`
- `20260806153953_MakeClientRoleGlobal`

Efectos principales:

- incorporación de la clave canónica;
- creación de `terceros_identificaciones` y migración de documentos existentes;
- traslado del rol y estado Proveedor desde `empresas_terceros` hacia `terceros`;
- eliminación de campos empresariales prematuros de proveedor;
- traslado del rol Cliente al tercero global;
- consolidación de estados independientes por rol.

Estas migraciones describen la evolución de la base de desarrollo. Conforme a `AGENTS.md`, antes de cerrar el modelo definitivo deberán consolidarse en una única migración limpia `InitialCreate` al reconstruir completamente la base de desarrollo.

---

## 18. Pruebas y evidencia existente

### Pruebas del servicio

Cubren, entre otros casos:

- creación global sin relación por empresa;
- auditoría de creación y verificación sin datos personales;
- conversión de cliente con cédula en proveedor con RUC sin perder información;
- catálogo idéntico al cambiar de empresa;
- independencia de los estados Cliente/Proveedor;
- reglas de KPIs;
- preservación del nombre comercial;
- rechazo por falta de autenticación, empresa o permiso;
- rechazo de verificación declarada sin constancia;
- modelo de roles globales;
- conflictos de versión al editar y cambiar estado.

### Pruebas del formulario

Cubren:

- validez de iconos Material Design utilizados;
- propósito `Proveedor` en las consultas;
- finalización con datos oficiales;
- tres rondas no disponibles y modo manual;
- inicio automático al completar un RUC válido;
- estado cancelable durante la consulta;
- ausencia de consulta externa para proveedor ya verificado;
- tratamiento del nombre comercial oficial;
- bloqueo de consultas y de modo offline para RUC inválido.

### Pruebas compartidas de interoperabilidad

Existen pruebas para:

- validador ecuatoriano;
- clave de identidad canónica;
- orquestación GUIA/SIFAE;
- respuestas funcionales y técnicas de GUIA;
- composición de razón social y correo con los endpoints SIFAE;
- prioridad de estados y emisión de constancias.

### Pruebas PostgreSQL reales

`TercerosPostgreSqlTests` valida contra una base PostgreSQL separada:

1. migraciones vigentes y mapeo de `Version` a `xmin`;
2. rechazo de actualización obsoleta;
3. unicidad de identidad canónica entre cédula y RUC;
4. rollback conjunto de mutación y auditoría;
5. independencia real de los estados Cliente y Proveedor.

El `StructuralSeeder` también tiene una prueba de idempotencia para tipos de identificación y Consumidor Final.

Estado conocido al cierre de la implementación V1:

- compilación de la solución: correcta, sin errores ni advertencias;
- suite completa: **251 pruebas aprobadas, 0 fallidas, 0 omitidas**;
- modelo EF Core: sin cambios pendientes detectados;
- pruebas PostgreSQL: aprobadas en base de integración separada.

Esta evidencia corresponde al cierre previamente ejecutado. La elaboración de este documento no volvió a ejecutar pruebas porque no modificó código ni base de datos.

---

## 19. Limitaciones y deuda conocida

1. **Solo RUC nacional.** No se admiten proveedores extranjeros en V1.
2. **Validación local limitada.** El formato se valida localmente, pero la existencia real depende de las fuentes oficiales.
3. **Constancias en memoria.** Caducan, son locales al proceso y se pierden al reiniciar la aplicación.
4. **Dependencia externa.** La disponibilidad y el contrato de GUIA/SIFAE pueden cambiar.
5. **Mensaje compartido.** Un mensaje genérico de indisponibilidad del servicio de identificación todavía puede mencionar “cliente” aunque el propósito sea Proveedor.
6. **Sin pruebas visuales automatizadas.** Los estilos WPF se verifican por código y revisión manual, no mediante capturas de regresión.
7. **Sin prueba continua contra servicios reales.** Las pruebas automatizadas simulan HTTP; no deben depender de los servicios oficiales reales.
8. **Búsqueda sin dirección.** La dirección no participa en el filtro textual.
9. **Orden fijo.** No existe ordenamiento configurable ni reordenamiento de columnas.
10. **Confirmación de cancelación aproximada.** El formulario no implementa seguimiento exacto de cambios sucios.
11. **Datos compartidos globalmente.** Editar contacto desde una empresa afecta al mismo tercero visto por todas; es intencional y debe comunicarse en la UX.
12. **Estado global del rol.** Inactivar un proveedor lo inactiva globalmente, no solo para la empresa activa.
13. **Sin configuración comercial de compras.** Plazos, crédito, retenciones o preferencias deben diseñarse con Compras/CxP y no improvisarse en este formulario.

---

## 20. Invariantes para módulos futuros

Los desarrollos posteriores deben respetar estas reglas:

1. No crear una tabla `proveedores` duplicando a `terceros`.
2. No volver a guardar `es_proveedor` o su estado en `empresas_terceros`.
3. No crear un tercero por empresa para el mismo RUC.
4. Resolver siempre la equivalencia cédula–RUC mediante la clave canónica.
5. No cambiar datos oficiales verificados sin nueva evidencia o un flujo explícito autorizado.
6. No confiar en la UI para autorización o verificación.
7. Toda operación debe validar la empresa y el usuario activos.
8. Usar `Version`/`xmin` al modificar datos compartidos.
9. Auditar mutación y cambio de estado en la misma transacción.
10. Nunca borrar físicamente un proveedor con historial; inactivar el rol.
11. No usar el estado general del tercero para simular el estado particular del proveedor.
12. Mantener independiente el rol Cliente.
13. Las operaciones de Compras deben guardar su propia empresa, establecimiento, usuario y snapshots históricos.
14. Los saldos deben vivir en Cuentas por Pagar, no en el maestro.
15. La configuración empresarial futura debe referenciar al tercero sin duplicar su identidad.

---

## 21. Archivos principales

### Domain

- `KONTAXPRO.Domain/Entities/Comercial/Tercero.cs`
- `KONTAXPRO.Domain/Entities/Comercial/EmpresaTercero.cs`
- `KONTAXPRO.Domain/Entities/Catalogos/TipoIdentificacion.cs`

### Application

- `KONTAXPRO.Application/Interfaces/IProveedorService.cs`
- `KONTAXPRO.Application/Models/Proveedores/ProveedorDtos.cs`
- `KONTAXPRO.Application/Security/TercerosSecurity.cs`
- `KONTAXPRO.Application/Clientes/IdentificacionEcuadorValidator.cs`
- `KONTAXPRO.Application/Clientes/ClaveIdentidadTercero.cs`
- `KONTAXPRO.Application/Interfaces/IConsultaIdentificacionService.cs`
- `KONTAXPRO.Application/Models/Interoperabilidad/ConsultaIdentificacionModels.cs`

### Infrastructure

- `KONTAXPRO.Infrastructure/Proveedores/ProveedorService.cs`
- `KONTAXPRO.Infrastructure/Interoperabilidad/ConsultaIdentificacionService.cs`
- `KONTAXPRO.Infrastructure/Interoperabilidad/ConstanciaVerificacionIdentificacionStore.cs`
- `KONTAXPRO.Infrastructure/Interoperabilidad/GuiaTokenClient.cs`
- `KONTAXPRO.Infrastructure/Interoperabilidad/GuiaIdentificacionProvider.cs`
- `KONTAXPRO.Infrastructure/Interoperabilidad/SifaeIdentificacionProvider.cs`
- `KONTAXPRO.Infrastructure/Persistence/Configurations/ComercialConfiguration.cs`

### Desktop

- `KONTAXPRO.Desktop/ViewModels/Proveedores/ProveedoresViewModel.cs`
- `KONTAXPRO.Desktop/ViewModels/Proveedores/ProveedorFormViewModel.cs`
- `KONTAXPRO.Desktop/Views/Proveedores/ProveedoresView.xaml`
- `KONTAXPRO.Desktop/Views/Proveedores/ProveedoresView.xaml.cs`
- `KONTAXPRO.Desktop/Views/Proveedores/ProveedorFormView.xaml`
- `KONTAXPRO.Desktop/Views/Proveedores/ProveedorFormView.xaml.cs`

### Tests

- `KONTAXPRO.Tests/Proveedores/ProveedorServiceTests.cs`
- `KONTAXPRO.Tests/Proveedores/ProveedorFormViewModelTests.cs`
- `KONTAXPRO.Tests/PostgreSql/TercerosPostgreSqlTests.cs`
- pruebas compartidas de Clientes para validación, identidad e interoperabilidad;
- `KONTAXPRO.Tests/Persistence/StructuralSeederTests.cs`.

---

## 22. Criterio de aceptación conservado

Proveedores V1 se considera funcionalmente cerrado cuando se mantienen simultáneamente estas condiciones:

- catálogo global sin duplicación por empresa;
- coexistencia segura Cliente/Proveedor;
- identidad canónica cédula–RUC;
- verificación oficial con fallback GUIA → SIFAE;
- modo offline únicamente tras tres indisponibilidades;
- autorización real en el servicio;
- concurrencia `xmin`;
- auditoría transaccional sin datos personales;
- interfaz coherente con el sistema y adaptable;
- cobertura automatizada e integración PostgreSQL aprobadas.

Toda ampliación debe tratar este documento y `AGENTS.md` como contexto, pero `AGENTS.md` prevalece si una decisión futura aprobada modifica el diseño.
