# Documentación KONTAXPRO Desktop

## Documentos principales

- [PRODUCTOS_V1.md](PRODUCTOS_V1.md): memoria funcional y técnica del módulo Productos en su primera versión cerrada, incluidas reglas de inventario, auditoría y pruebas.
- [CLIENTES_V1.md](CLIENTES_V1.md): arquitectura, persistencia, consulta GUIA/SIFAE, seguridad, UI y pruebas de Clientes V1.
- [PROVEEDORES_IMPLEMENTACION.md](PROVEEDORES_IMPLEMENTACION.md): decisiones y estado de implementación de Proveedores V1 antes de su auditoría formal de cierre.
- [COMPRAS_IMPLEMENTACION.md](COMPRAS_IMPLEMENTACION.md): arquitectura, importación, recepción, CxP, contabilización automática, migraciones y límites vigentes del módulo Compras.
- [POSTGRESQL_INTEGRATION_TESTS.md](POSTGRESQL_INTEGRATION_TESTS.md): configuración segura y ejecución opt-in de las pruebas relacionales contra una base PostgreSQL exclusiva `_test`.
- [UI_UX_KONTAXPRO.md](UI_UX_KONTAXPRO.md): estándar visual global para interfaces WPF compatibles con los temas Light y Dark.
- [PATRONES_DESARROLLO_KONTAXPRO.md](PATRONES_DESARROLLO_KONTAXPRO.md): patrones técnicos para construir y mantener módulos respetando la arquitectura real del repositorio.

Antes de modificar un módulo existente, consultar primero su documentación específica. Antes de construir UI nueva, consultar `UI_UX_KONTAXPRO.md`. Antes de crear la arquitectura de un módulo, consultar `PATRONES_DESARROLLO_KONTAXPRO.md`.

Este archivo es solo el índice; la fuente de verdad ejecutable continúa siendo el código actual.
