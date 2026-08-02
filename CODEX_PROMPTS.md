Continuar con la reconstrucción de Development.

La variable PGPASSWORD ya está disponible únicamente en esta sesión. Antes de cualquier acción destructiva, vuelve a confirmar:

- DOTNET_ENVIRONMENT=Development
- host PostgreSQL=localhost
- base de datos=kontax_desktop
- rama actual=development

Después:

1. Elimina únicamente la base local kontax_desktop.
2. Recréala vacía.
3. Aplica la única migración InitialCreate existente.
4. Ejecuta StructuralSeeder.
5. Ejecuta DemoSeeder.
6. Verifica el usuario demo 1724853377.
7. Verifica las dos empresas demo.
8. Verifica las bodegas PRODUCTOS CON FACTURA y PRODUCTOS SIN FACTURA.
9. Verifica las listas A, B y C.
10. Verifica el catálogo actualizado de unidades de medida.
11. Verifica las tablas de conversión de control de inventario.
12. Ejecuta build, pruebas y comprobación de cambios pendientes de EF.

No regeneres InitialCreate salvo que la aplicación de la migración revele un error estructural real. No hagas commit ni push.