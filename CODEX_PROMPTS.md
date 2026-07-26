# CODEX_PROMPTS.md — Ejecución de la reconstrucción KONTAXPRO

Ejecutar los prompts en orden. Todos asumen que Codex leerá primero `AGENTS.md`.

---

## PROMPT 0 — Auditoría inicial

Lee `AGENTS.md` completo.

Inspecciona la solución `KONTAXPRO.slnx` y crea `docs/database-rebuild-audit.md`.

Localiza:
- KontaxDbContext;
- entidades Domain;
- IEntityTypeConfiguration;
- migraciones;
- seeders;
- App.xaml.cs;
- DI;
- CurrentSession;
- AuthenticationService;
- servicios de Productos/Inventario;
- dependencias del modelo viejo.

Documenta:
- archivos a conservar;
- archivos a reemplazar;
- entidades/campos obsoletos;
- migraciones actuales;
- riesgos;
- plan exacto por bloques.

No borres la BD ni hagas cambios masivos aún.

Ejecuta:
`dotnet build KONTAXPRO.slnx`

No avances si el build está roto.

---

## PROMPT 1 — s_catalogos

Lee `AGENTS.md` y `docs/database-rebuild-audit.md`.

Implementa TODO `s_catalogos` según AGENTS.md:
- entidades;
- configuraciones EF Core;
- DbSet;
- índices;
- checks;
- precisiones;
- seeds estructurales idempotentes.

No crees migración todavía.

No inventes catálogos adicionales.

Compila y corrige todo.

---

## PROMPT 2 — s_configuracion

Implementa:
- empresas;
- establecimientos;
- puntos_emision;
- secuenciales_comprobantes;
- secuenciales_internos;
- usuarios_configuracion_empresa;
- facturacion_electronica.

Respeta índices parciales, unicidad y reglas de multiempresa.

No MAX+1.
No migración aún.

Compila.

---

## PROMPT 3 — s_seguridad

Implementa:
- usuarios;
- roles;
- permisos;
- roles_permisos;
- usuarios_empresas;
- usuarios_empresas_roles;
- usuarios_empresas_establecimientos;
- auditoria.

Incluye seeds base de roles/permisos.

No tabla de sesiones de login.
No permisos directos por usuario.

No migración aún.

Compila.

---

## PROMPT 4 — s_comercial

Implementa:
- terceros;
- empresas_terceros.

Incluye Consumidor Final en StructuralSeeder.

No crear tablas clientes/proveedores.

Implementa cupo_credito y dias_credito.

No almacenar saldo.

No migración aún.

Compila.

---

## PROMPT 5 — s_inventario

Implementa completamente el bloque de inventario según AGENTS.md:
- productos;
- presentaciones;
- listas;
- precios;
- costos;
- bodegas;
- existencias;
- lotes;
- series;
- movimientos;
- detalles;
- lotes/series por movimiento;
- transferencias;
- ajustes.

Reglas críticas:
- producto no guarda precio/costo/stock/barcode/impuesto;
- presentación base única;
- lista base única;
- un movimiento = una bodega;
- transferencias generan salida+entrada;
- snapshots de stock/costo;
- reversos append-only.

Adapta servicios existentes para compilar sin reintroducir el modelo viejo.

No migración todavía.

Build limpio.

---

## PROMPT 6 — s_ventas

Implementa todo `s_ventas`.

Reglas:
- XF siempre contado;
- NE directa o desde XF, no mezclada en v1;
- factura DIRECTA/DESDE_NOTA_ENTREGA/DESDE_XF;
- factura derivada no mueve stock/CxC/dinero otra vez;
- origen_facturable snapshot;
- devoluciones desde FACTURA/NE/XF;
- NC siempre contra Factura;
- ND no inventario;
- Guía no inventario;
- no clave_acceso en documentos funcionales;
- codigo_forma_pago_sri snapshot.

No migración.

Compila.

---

## PROMPT 7 — s_compras

Implementa:
- documentos_recibidos_sri;
- compras;
- detalles;
- impuestos;
- liquidaciones;
- devoluciones;
- ajustes.

Reglas:
- documento recibido SRI no es Compra;
- tercero recibido puede ser NULL al importar;
- FACTURADA/SIN_FACTURA;
- gastos/servicios pueden no usar producto;
- bodega facturable/no facturable según tipo;
- toda Compra genera CxP en la futura orquestación;
- Liquidación no crea Compra duplicada;
- devolución COMPRA/LIQUIDACION;
- ajuste no mueve inventario.

No migración.

Compila.

---

## PROMPT 8 — s_cartera

Implementa CxC y CxP completas.

Incluye:
- cobros_medios;
- pagos_medios;
- aplicaciones;
- reversos;
- movimientos append-only.

Reglas:
- saldo >=0;
- cobros/pagos totalmente aplicados en v1;
- no anticipos reutilizables;
- retenciones no son dinero;
- ND puede reabrir CxC;
- NC no deja saldo negativo.

No migración.

Compila.

---

## PROMPT 9 — s_tesoreria + s_bancos

Implementa:
- cajas;
- cajas_sesiones;
- movimientos_caja;
- depositos_caja_banco;
- cuentas_bancarias;
- movimientos_bancarios;
- transferencias_bancarias.

Reglas:
- caja = fondo físico;
- sesión compartible por varios usuarios/equipos;
- abierta_por_usuario_id;
- cerrada_por_usuario_id;
- una sesión abierta por caja;
- valor positivo, naturaleza define dirección;
- cobro_medio_id/pago_medio_id;
- depósitos y transferencias generan movimientos pareados.

No migración.

Compila.

---

## PROMPT 10 — s_contabilidad

Implementa:
- plan_cuentas;
- configuracion_cuentas;
- periodos_contables;
- secuenciales_asientos;
- asientos;
- detalles.

Reglas:
- sin saldos persistidos;
- periodos abiertos/cerrados;
- ASI-año-secuencial;
- varios asientos pueden compartir origen;
- contabilizado no se edita;
- reverso único;
- debe/haber exacto;
- asiento balanceado antes de commit;
- operación funcional genera asiento, efectos derivados no lo duplican.

No migración.

Compila.

---

## PROMPT 11 — s_tributacion

Implementa:
- retenciones_emitidas;
- detalles;
- retenciones_recibidas;
- retenciones_recibidas_documentos;
- detalles.

Reglas:
- emitidas origen COMPRA/LIQUIDACION;
- periodo_fiscal;
- sin subtotal_retenido;
- sin clave_acceso en emitidas;
- recibidas pueden aplicar a varias facturas;
- concepto nullable en recibidas;
- retenciones afectan cartera, no caja/banco.

No migración.

Compila.

---

## PROMPT 12 — s_facturacion_electronica

Implementa:
- comprobantes_electronicos;
- comprobantes_electronicos_eventos.

Estados:
PENDIENTE, PROCESANDO, GENERADO, FIRMADO, ENVIADO, RECIBIDO, AUTORIZADO, NO_AUTORIZADO, ERROR.

Eventos:
GENERACION, FIRMA, ENVIO, RECEPCION, AUTORIZACION, REINTENTO, ERROR.

Reglas:
- clave única y centralizada;
- un comprobante electrónico por origen;
- procesamiento_iniciado_at;
- procesado_por_instalacion_uuid;
- XML/RIDE fuera de BD;
- eventos append-only;
- mensaje/informacion_adicional TEXT;
- guardar TODOS los mensajes SRI;
- varios mensajes SRI => varios eventos;
- no sobrescribir errores previos;
- error secundario después de AUTORIZADO no cambia AUTORIZADO.

Diseña interfaces/worker para ejecución solo en nodo SERVIDOR.

No conectes a SRI real si todavía no existe configuración segura; deja contratos y separación correctos.

No migración.

Compila.

---

## PROMPT 13 — StructuralSeeder completo

Consolida un `StructuralSeeder` idempotente para:
- catálogos;
- roles;
- permisos;
- roles_permisos;
- consumidor final;
- datos estructurales obligatorios.

No incluir:
- empresas demo;
- usuario demo;
- productos demo;
- ventas/compras demo.

Usa códigos estables para resolver relaciones.

Marca claramente datos tributarios que deban verificarse antes de release de producción.

Compila.

---

## PROMPT 14 — DemoSeeder Development

Crea `DemoSeeder` separado.

Solo debe ejecutarse si el ambiente es Development.

Crear:
- 1 usuario demo con PasswordHasher real;
- 2 empresas demo;
- relación del usuario con ambas;
- ADMINISTRADOR en ambas;
- acceso a establecimientos;
- establecimiento 001 matriz en cada una;
- punto de emisión 001;
- bodega principal;
- caja principal;
- lista base;
- configuración mínima;
- secuenciales mínimos;
- usuarios_configuracion_empresa.

Objetivo:
- Login funciona;
- aparece selección de empresa;
- se puede entrar a ambas;
- Cambiar empresa funciona;
- MainWindow abre sin configuración manual.

Documenta credenciales demo sin usar secretos reales.

Compila.

---

## PROMPT 15 — Auditoría final EF antes de migrar

No generes migración todavía.

Crea `docs/final-model-audit.md`.

Verifica contra AGENTS.md:
- todas las tablas;
- schemas;
- relaciones;
- nullabilidad;
- índices;
- índices parciales;
- checks;
- precisiones;
- DeleteBehavior;
- multiempresa;
- campos obsoletos ausentes;
- clave_acceso no duplicada;
- cobros.medio_pago_id ausente;
- movimientos inventario sin bodega_origen/destino.

Ejecuta:
- `dotnet restore`
- `dotnet build KONTAXPRO.slnx`

No continúes con errores.

---

## PROMPT 16 — Reset Development + InitialCreate

TAREA DESTRUCTIVA.

Antes de borrar:
1. confirma `Development`;
2. muestra host y nombre de BD;
3. aborta si parece Production.

Luego:
1. elimina migraciones experimentales obsoletas;
2. elimina la BD PostgreSQL de desarrollo actual;
3. genera una única migración `InitialCreate`;
4. revisa la migración;
5. aplica a BD vacía;
6. ejecuta StructuralSeeder;
7. ejecuta DemoSeeder;
8. build final.

No uses pgAdmin como solución oficial.

---

## PROMPT 17 — Smoke test de inicio

Con la BD creada únicamente por InitialCreate + seeders:

1. inicia KONTAXPRO;
2. login con demo;
3. verificar selección de 2 empresas;
4. elegir Empresa 1;
5. validar CurrentSession;
6. validar MainWindow;
7. cambiar a Empresa 2;
8. abrir Productos;
9. ejecutar lecturas básicas;
10. corregir incompatibilidades del código con el nuevo modelo.

No modifiques el diseño de BD solo para hacer compilar si el problema está en Application/Desktop.

Crear `docs/initialcreate-smoke-test.md`.

---

## PROMPT 18 — Adaptar Productos al modelo final

Adapta el módulo Productos ya existente al modelo definitivo.

Debe soportar:
- Producto;
- presentación BASE;
- presentaciones adicionales;
- categoría;
- marca;
- unidad base;
- impuestos mediante productos_impuestos;
- precios/listas;
- costos;
- existencias.

Reglas:
- crear producto genera BASE;
- editar no destruye presentaciones adicionales;
- mantener look & feel;
- no tarifa_impuesto_id en Producto;
- no precio/costo/stock en Producto;
- EF con IDbContextFactory;
- transacciones correctas.

Build limpio.

---

## PROMPT 19 — Revisión final

1. `git diff`.
2. revisar secretos/rutas/certificados.
3. build completo.
4. verificar DemoSeeder solo Development.
5. verificar que BD se recrea desde cero.
6. actualizar docs.
7. resumir cambios por capa.
8. sugerir mensaje de commit.
9. no hacer push salvo instrucción explícita.
