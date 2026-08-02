# ROADMAP_AGROCALIDAD.md

## KONTAXPRO Desktop — Módulo futuro de Cumplimiento Agrocalidad

**Estado:** Diferido para una versión posterior a KONTAXPRO 1.0  
**Prioridad actual:** Terminar y estabilizar Inventario, Ventas, Facturación Electrónica, migración de clientes y licenciamiento/suscripción.  
**Objetivo:** Conservar las decisiones y hallazgos funcionales para retomar el módulo regulatorio sin reconstruir el análisis desde cero.

---

# 1. Motivo para postergar

El principal nicho de KONTAXPRO son almacenes agropecuarios y veterinarios. Existe una oportunidad comercial importante en incorporar controles de cumplimiento de Agrocalidad para productos cuya venta requiere receta o prescripción.

Sin embargo, implementar correctamente esta funcionalidad no consiste únicamente en imprimir una receta. Requiere integrar:

- clasificación regulatoria del producto;
- profesionales prescriptores;
- recetas agrícolas y veterinarias;
- productos e ingredientes/principios activos;
- control de vigencia;
- validación al momento de vender;
- archivo documental;
- trazabilidad receta ↔ venta;
- control de cantidades dispensadas;
- reportes para inspección;
- concordancia receta ↔ inventario/Kardex;
- tratamiento de productos restringidos y venta fraccionada cuando aplique.

Por la urgencia actual de liberar KONTAXPRO y migrar clientes al nuevo sistema, se decidió **no incorporar este módulo en la versión inicial**.

---

# 2. Hallazgos normativos relevantes

La documentación revisada de Agrocalidad evidencia que la regulación distingue diferentes escenarios de comercialización.

## Productos veterinarios

Se manejan al menos tres grupos regulatorios:

### Grupo I
Productos de mayor riesgo, incluyendo estupefacientes, psicotrópicos y otros productos sujetos a prescripción restringida.

La venta debe realizarse bajo receta médica de prescripción restringida emitida por un médico veterinario.

### Grupo II
Productos veterinarios que requieren receta médica veterinaria.

### Grupo III
Productos de venta libre.

La documentación también hace referencia a:

- **ROEP** — receta oficial de estupefacientes y psicotrópicos.
- **REV** — receta estándar veterinaria.

Debe considerarse también la venta fraccionada autorizada para determinados productos veterinarios del Grupo II y otras reglas específicas de comercialización.

## Productos agrícolas

Existe la **REA — Receta Estándar Agrícola**, física o telemática, para productos cuya comercialización exige prescripción.

El formato revisado contempla información como:

- fecha de emisión;
- número de receta;
- profesional que prescribe;
- cédula;
- registro SENESCYT;
- teléfono;
- cultivo y área;
- propietario;
- dirección;
- prescripción;
- ingrediente activo;
- concentración;
- formulación;
- dosis;
- indicaciones;
- firma.

El formato de Agrocalidad es ilustrativo; la interfaz de KONTAXPRO no debe imitar visualmente el formulario físico. KONTAXPRO puede tener una UI moderna y generar posteriormente el documento requerido.

---

# 3. Principio arquitectónico

El módulo Agrocalidad futuro debe implementarse **sin mezclar la regulación dentro del Kardex o del maestro básico de Producto**.

La relación conceptual será:

```text
PRODUCTO
   │
   ├── información comercial/inventario ya existente
   │
   └── configuración regulatoria
             │
             ↓
       tipo de regulación
             │
             ↓
          RECETA
             │
             ├── profesional
             ├── propietario/cliente
             ├── productos/principios activos
             ├── cantidades/dosis/indicaciones
             └── documento adjunto/generado
                     │
                     ↓
                   VENTA
                     │
                     ↓
                 INVENTARIO
                     │
                     ↓
                   KARDEX
```

La receta controla o habilita la dispensación, pero el movimiento físico de stock continúa funcionando mediante los mecanismos normales de Inventario.

---

# 4. Qué NO debe cambiar en el diseño actual

El futuro módulo no debe obligar a rediseñar:

- `productos`;
- `productos_presentaciones`;
- `productos_existencias`;
- `productos_costos`;
- `productos_lotes`;
- `productos_lotes_existencias`;
- `productos_series`;
- `movimientos_inventario`;
- `movimientos_inventario_detalles`;
- bodegas;
- costos;
- precios;
- Kardex;
- Inventario Inicial.

La arquitectura actual de inventario debe mantenerse como base.

La regulación interviene principalmente en la **comercialización/dispensación**, no en la existencia física inicial del producto.

---

# 5. No agregar ahora campos “por si acaso”

No añadir anticipadamente campos simplistas como:

```text
productos.requiere_receta
productos.grupo_agrocalidad
productos.tipo_receta
productos.principio_activo
```

El problema regulatorio es más amplio que un booleano.

Cuando se implemente, debe diseñarse un módulo específico y normalizado.

---

# 6. Esquema futuro recomendado

Evaluar un nuevo esquema:

```text
s_regulatorio
```

Nombre sujeto a revisión cuando se implemente.

Posibles entidades:

## `tipos_control_regulatorio`

Ejemplos conceptuales:

```text
LIBRE
REA
REV
ROEP
```

No cerrar todavía el catálogo; validar nuevamente normativa vigente antes de implementarlo.

## `productos_configuracion_regulatoria`

Posibles campos:

```text
id
producto_id
tipo_control_regulatorio_id
registro_agrocalidad
grupo_regulatorio
requiere_prescripcion
permite_venta_fraccionada
estado
created_at
updated_at
```

La estructura definitiva deberá validarse con normativa vigente.

## `principios_activos`

Catálogo de principios/ingredientes activos.

## `productos_principios_activos`

Relación N:M para soportar productos con uno o varios principios activos.

Posibles datos adicionales:

```text
concentracion
unidad_concentracion
formulacion
```

## `profesionales_prescriptores`

Posibles datos:

```text
id
tipo_identificacion_id
numero_identificacion
nombres
apellidos
profesion
registro_senescyt
telefono
correo
direccion
tipo_autorizacion
estado
created_at
updated_at
```

Un profesional prescriptor NO necesariamente debe ser usuario de KONTAXPRO.

Debe poder existir relación opcional con un usuario si posteriormente se requiere autenticación del profesional.

## `recetas`

Cabecera común de prescripción.

Posibles campos:

```text
id
empresa_id
establecimiento_id
tipo_receta_id
numero_receta
fecha_emision
fecha_vencimiento
profesional_prescriptor_id
empresa_tercero_id
estado
observacion
created_at
updated_at
```

No asumir que REA, REV y ROEP tendrán exactamente los mismos datos. Puede requerirse una cabecera común más tablas especializadas.

## `recetas_detalles`

Debe permitir relacionar:

- producto comercial;
- principio activo;
- concentración;
- cantidad prescrita;
- cantidad dispensada;
- dosis;
- frecuencia;
- duración;
- indicaciones;
- información adicional.

## `recetas_ventas` / relaciones de dispensación

Debe relacionar una receta con una o varias operaciones comerciales o detalles de venta, según permita la normativa.

Debe impedir dispensar cantidades superiores a las autorizadas cuando corresponda.

---

# 7. Profesional prescriptor

El sistema no debe convertir al vendedor o almacenista automáticamente en prescriptor.

Una receta generada desde KONTAXPRO debe estar asociada a un profesional habilitado.

Diferenciar claramente:

```text
Usuario KONTAXPRO
≠
Profesional prescriptor
```

El flujo futuro debe permitir:

1. Registrar una receta externa traída por el cliente.
2. Adjuntar PDF/JPG/PNG de la receta.
3. Generar una receta dentro de KONTAXPRO solamente cuando exista un profesional autorizado.

---

# 8. Flujo futuro de venta regulada

Cuando un vendedor agregue un producto regulado:

```text
VENTA
   ↓
Producto regulado detectado
   ↓
¿Existe receta válida?
   │
   ├── SÍ → validar receta/cantidad/vigencia
   │          ↓
   │        permitir venta
   │
   └── NO → bloquear finalización
             ↓
      Registrar/adjuntar receta
```

La UI debe ser clara y no depender de que el vendedor recuerde qué productos requieren receta.

---

# 9. Cliente identificado

Para una venta regulada debe evaluarse exigir un tercero/cliente identificado.

No asumir que `CONSUMIDOR FINAL` será válido en todos los escenarios regulados.

La receta puede requerir información del propietario/productor.

La regla definitiva debe revisarse contra la normativa vigente antes de implementar.

---

# 10. Vigencia de recetas

El sistema futuro debe manejar vigencia según el tipo de receta.

No hardcodear una única duración global.

Cada tipo de receta/regulación debe poder definir reglas diferentes.

Las reglas deben verificarse nuevamente con documentación oficial actualizada antes del desarrollo.

---

# 11. Archivo documental

KONTAXPRO debe permitir conservar:

- receta generada;
- receta externa digitalizada;
- PDF/JPG/PNG cuando corresponda;
- relación con venta;
- profesional;
- cliente;
- productos dispensados.

Los archivos no deben almacenarse como binarios en PostgreSQL salvo una justificación posterior.

Seguir la filosofía actual de filesystem + referencias/metadatos.

Posible estructura:

```text
KONTAXPRO/
  empresas/
    {RUC}/
      documentos/
        agrocalidad/
          recetas/
```

---

# 12. Consulta e inspecciones

Debe existir una pantalla futura:

```text
CUMPLIMIENTO AGROCALIDAD
```

Con búsqueda por:

- número receta;
- fecha;
- cliente/productor;
- profesional;
- producto;
- principio activo;
- factura/venta;
- tipo de receta;
- estado.

Debe ser posible localizar rápidamente el respaldo de una venta durante una inspección.

---

# 13. Trazabilidad receta ↔ inventario

No crear un Kardex paralelo.

La relación futura debe permitir responder:

```text
¿Qué receta respaldó esta venta?
¿Qué productos se dispensaron?
¿Qué cantidad se autorizó?
¿Qué cantidad se vendió?
¿Qué movimiento de inventario produjo esa venta?
```

La trazabilidad se obtiene enlazando:

```text
RECETA
→ VENTA
→ DETALLE DE VENTA
→ MOVIMIENTO INVENTARIO
→ KARDEX
```

---

# 14. Reportes futuros

Considerar:

- ventas de productos regulados;
- recetas emitidas;
- recetas externas registradas;
- recetas por profesional;
- recetas por cliente/productor;
- recetas por producto/principio activo;
- productos Grupo I/II/III;
- productos agrícolas restringidos;
- cantidades prescritas vs dispensadas;
- recetas vencidas;
- ventas sin respaldo — debe idealmente ser cero;
- conciliación receta ↔ Kardex;
- productos fraccionados;
- archivo de inspección.

---

# 15. Fases recomendadas

## Fase 1 — Control básico
Complejidad: MEDIA.

- clasificación regulatoria de producto;
- requiere receta;
- registro de receta externa;
- adjuntar documento;
- relación receta ↔ venta;
- bloqueo de venta sin receta;
- consulta histórica.

## Fase 2 — Prescripción desde KONTAXPRO
Complejidad: MEDIA-ALTA.

- profesionales prescriptores;
- principios activos;
- generación REA;
- generación REV;
- datos agrícolas/veterinarios;
- PDF;
- vigencias;
- documentos para almacenista/cliente.

## Fase 3 — Cumplimiento integral
Complejidad: ALTA.

- ROEP;
- venta fraccionada;
- cantidades autorizadas vs dispensadas;
- controles especiales;
- auditoría;
- reportes para Agrocalidad;
- conciliación receta ↔ Kardex;
- alertas regulatorias;
- archivo documental avanzado.

---

# 16. Prioridad de KONTAXPRO 1.0

Antes de retomar este módulo, terminar y estabilizar:

1. Productos.
2. Presentaciones.
3. Inventario Inicial.
4. Costos.
5. Precios.
6. Lotes.
7. Series.
8. Caducidad.
9. Kardex.
10. Bodegas.
11. Transferencias/Ajustes.
12. Ventas.
13. Facturación electrónica SRI.
14. RIDE/XML/firma/autorización.
15. Migración de clientes actuales.
16. Instalación.
17. Licenciamiento/suscripción.

No retrasar KONTAXPRO 1.0 por el módulo Agrocalidad.

---

# 17. Migraciones futuras

Cuando llegue esta funcionalidad, NO reconstruir la BD de los clientes.

Crear una migración incremental, por ejemplo:

```text
AddAgrocalidadComplianceModule
```

La migración deberá:

- crear nuevas tablas;
- crear relaciones;
- agregar índices;
- agregar configuración necesaria;
- preservar completamente datos existentes.

---

# 18. Regla para Codex/agentes futuros

Antes de implementar Cumplimiento Agrocalidad:

1. Leer este documento completo.
2. Revisar `AGENTS.md`.
3. Revisar la última estructura real de Productos/Ventas/Inventario.
4. Consultar la normativa oficial vigente de Agrocalidad.
5. No asumir que las reglas descritas aquí siguen vigentes sin verificación.
6. Diseñar primero el modelo completo.
7. Revisarlo con el responsable del proyecto.
8. Solo después generar entidades/migraciones/UI.
9. No modificar Kardex o costos salvo necesidad real.
10. Mantener la filosofía: **robustez regulatoria por dentro, simplicidad por fuera.**

---

# 19. Decisión registrada

**Decisión:** No implementar el módulo de recetas/prescripciones Agrocalidad en KONTAXPRO 1.0.

**Razón:** La prioridad es liberar el nuevo KONTAXPRO dentro del plazo operativo disponible, estabilizando facturación electrónica, inventario y migración de clientes.

**Compromiso arquitectónico:** El diseño actual debe mantenerse extensible para incorporar posteriormente Cumplimiento Agrocalidad mediante migraciones incrementales, sin reconstruir Inventario ni las bases de clientes.

**Valor comercial futuro:** El módulo podrá comercializarse como una mejora especializada para almacenes agropecuarios y veterinarios y constituir una ventaja competitiva importante de KONTAXPRO.
