# Datos demo de Development

`DemoSeeder` se ejecuta únicamente cuando `DOTNET_ENVIRONMENT` o
`ASPNETCORE_ENVIRONMENT` tiene el valor `Development`.

Para una base de desarrollo nueva:

- Identificación: `0999999999`
- Contraseña: `KontaxDemo2026!`

Estas credenciales son públicas, ficticias y exclusivas para desarrollo local.
No deben reutilizarse como credenciales reales ni habilitarse en producción.

El usuario tiene acceso con rol `ADMINISTRADOR` a:

- `1799999999001` — KONTAXPRO DEMO UNO
- `1799999999002` — KONTAXPRO DEMO DOS

Cada empresa incluye establecimiento y punto de emisión `001`, bodega y caja
principal, lista de precios base, configuración electrónica deshabilitada en
ambiente de pruebas, secuenciales mínimos y preferencias iniciales del usuario.

El seeder es idempotente: conserva la contraseña si el usuario demo ya existe y
solo completa relaciones o configuración faltantes.
