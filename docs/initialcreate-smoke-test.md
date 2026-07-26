# Smoke test de inicio sobre InitialCreate

Fecha: 2026-07-26.

## Ambiente

- `DOTNET_ENVIRONMENT=Development`
- PostgreSQL `localhost:5432`
- Base `kontax_desktop`
- Estructura creada exclusivamente por
  `20260726085718_InitialCreate`
- Datos creados exclusivamente por `StructuralSeeder` y `DemoSeeder`

## Resultado

| Prueba | Resultado | Evidencia |
|---|---|---|
| Inicio de KONTAXPRO | Correcto | La ventana `KONTAXPRO · Acceso seguro` abrió sin excepción. |
| Login demo | Correcto | Una ejecución con entrada real de teclado cerró el login y abrió `Seleccionar empresa`. |
| Dos empresas | Correcto | El selector expuso dos empresas; la consulta de acceso devolvió exactamente las empresas demo 1 y 2. |
| Contexto Empresa 1 | Correcto por servicio/datos | Empresa `1799999999001`, establecimiento `001`, punto `001`, bodega `PRINCIPAL` y rol `ADMINISTRADOR`. |
| MainWindow | Compatible | Construye después del flujo de sesión; el build WPF y la resolución DI son correctos. |
| Contexto Empresa 2 | Correcto por servicio/datos | Empresa `1799999999002`, establecimiento `001`, punto `001`, bodega `PRINCIPAL` y rol `ADMINISTRADOR`. |
| Productos | Corregido | La ruta no podía resolver `ProductsViewModel`; se añadió su registro transient en DI. |
| Lecturas básicas | Correcto | Consultas de productos, categorías, marcas, unidad de medida y listas se ejecutaron sin error. |

La base demo no contiene productos todavía, por lo que las lecturas de productos
devuelven cero filas en ambas empresas. Esto es un resultado válido, no un
error de compatibilidad.

## CurrentSession esperado y validado

Después de seleccionar cada empresa, las relaciones usadas por
`UsuarioEmpresaService.SeleccionarEmpresaAsync` entregan:

- usuario demo activo;
- empresa activa correspondiente;
- rol `ADMINISTRADOR`;
- establecimiento `001` / `MATRIZ`;
- punto de emisión `001`;
- bodega `PRINCIPAL`;
- dos empresas disponibles, por lo que `PuedeCambiarEmpresa` es verdadero.

La caja no se fija en `UsuarioEmpresaService`: su selección pertenece al flujo
operativo de caja compartida y no es un requisito para abrir Productos.

## Incompatibilidades corregidas

1. Se registró `ProductsViewModel` en el contenedor DI. Antes de la corrección,
   `NavigationService.NavigateTo("Productos")` resolvía `null`.
2. Se añadieron nombres de automatización a los controles principales de
   login, selección de empresa, menú de usuario y navegación lateral. Esto
   mejora accesibilidad y permite identificar controles con plantillas WPF.

No se modificaron entidades, configuraciones EF, migraciones ni el diseño de
base de datos.

## Observación sobre automatización UI

`PasswordBox` no sincroniza el ViewModel cuando una herramienta externa asigna
su `ValuePattern` y bloquea el pegado automatizado. Por eso la comprobación UI
repetible debe usar un framework WPF in-process o entrada física. Durante esta
revisión se observó una ejecución real que llegó al selector de empresas; las
transiciones posteriores se verificaron determinísticamente contra los mismos
servicios y relaciones que alimentan `CurrentSession`.

