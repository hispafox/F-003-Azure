# S3.2 — Trigger HTTP: endpoints, autenticación y enrutamiento

> **Submódulo de referencia:** [M03-S3.2](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.2-trigger-http-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption (gratuito)

> ℹ️ Reutiliza el skeleton del [S3.1](../S3.1-principios-computo-sin-servidor) y
> añade un **CRUD completo** sobre `/api/productos`, middlewares y validación.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: la equivalencia Functions HTTP / Minimal API, los niveles de autorización, validación con 400 vs 422 + Problem Details y la disciplina del middleware en orden.

## Objetivo

Construir una API REST CRUD completa con HTTP triggers usando el modelo
ASP.NET Core en Functions (slide 4 — `HttpRequest` + `IActionResult`):

- 5 verbos sobre `/api/productos` (GET list/by-id, POST, PUT, DELETE).
- **Routing avanzado** con parámetros de ruta y query strings.
- **Mezcla de niveles de autorización**: `/api/ping` Anonymous, `/api/productos/*` Function (slide 10).
- **Validación con DataAnnotations** y respuestas en formato **Problem Details** RFC 7807 (slides 15, 24).
- **Dos middlewares**: `ExceptionHandlingMiddleware` y `CorrelationIdMiddleware` (slide 14).
- **DI completo** con servicio en memoria thread-safe (slide 13).

> 🎯 **Patrón clave**: las APIs que escribirías en M02 con **Minimal API** (`MapGet`,
> `MapPost`...) y las que escribes aquí con **HTTP triggers** comparten casi
> 100 % del código (mismo `HttpRequest`, mismo `IActionResult`, mismos `Results.Ok`).
> La diferencia es solo el bootstrap (`FunctionsApplication.CreateBuilder` vs
> `WebApplication.CreateBuilder`) y el modelo de hosting (Consumption vs App Service).

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| Modelo ASP.NET Core (`HttpRequest`/`IActionResult`) | 4 | todas las funciones |
| CRUD completo GET/POST/PUT/DELETE | 5 | [`Functions/ProductosFunctions.cs`](src/AzureFunctions.Demo/Functions/ProductosFunctions.cs) |
| Routing personalizado + parámetros | 6 | `Route = "productos/{id}"` |
| Query parameters + headers | 7 | `Listar` lee `nombre`, `categoria`, `minPrecio`, `maxPrecio`, `pagina`, `porPagina` |
| Request body con `ReadFromJsonAsync` | 8 | `Crear` y `Actualizar` |
| Códigos HTTP variados | 9 | 200, 201, 204, 400, 404, 422 |
| Niveles de autorización (Anonymous + Function) | 10 | `PingFunction` (Anonymous) vs `ProductosFunctions` (Function) |
| DI por constructor | 13 | `ProductosFunctions(IProductoService, IOptions<...>, ILogger<...>)` |
| Middleware con `IFunctionsWorkerMiddleware` | 14 | [`Middleware/`](src/AzureFunctions.Demo/Middleware/) |
| Validación con DataAnnotations | 15 | `Models/Producto.cs` + `Validator.TryValidateObject` |
| REST Client local | 16 | [`api.http`](src/AzureFunctions.Demo/api.http) |
| Problem Details RFC 7807 | 24 | helpers `NotFoundProblem` y `ValidationProblem` |

## Estructura

```
S3.2-trigger-http/
├── README.md
├── AzureFunctions.Demo.slnx
├── Directory.Build.props
├── global.json
├── .gitattributes
├── src/AzureFunctions.Demo/
│   ├── AzureFunctions.Demo.csproj
│   ├── Program.cs              DI + middleware
│   ├── host.json
│   ├── local.settings.json.example
│   ├── api.http                ← REST Client examples (10 requests)
│   ├── Configuration/
│   │   └── ProductosOptions.cs   MaxPorPagina + PorPaginaPorDefecto
│   ├── Models/
│   │   └── Producto.cs           entidad + DTOs Crear/Actualizar/Buscar
│   ├── Services/
│   │   ├── IProductoService.cs
│   │   └── InMemoryProductoService.cs   ConcurrentDictionary thread-safe
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs   slide 14 + 24
│   │   └── CorrelationIdMiddleware.cs       slide 14
│   └── Functions/
│       ├── HelloFunction.cs       (heredado de S3.1)
│       ├── PingFunction.cs        GET /api/ping  (Anonymous)
│       └── ProductosFunctions.cs  CRUD completo  (Function key)
├── tests/AzureFunctions.Demo.Tests/   (22 tests)
└── scripts/                            (5 scripts az + demo)
```

## Endpoints

| Verbo | Ruta | Auth | Notas |
| --- | --- | --- | --- |
| GET | `/api/ping` | Anonymous | health público (slide 10) |
| GET | `/api/hello?name=...` | Anonymous | heredado de S3.1 |
| GET | `/api/productos` | Function | filtros: `nombre`, `categoria`, `minPrecio`, `maxPrecio`, `pagina`, `porPagina`. Header `X-Total-Count`. |
| GET | `/api/productos/{id}` | Function | 200 con producto, **404 ProblemDetails** si no existe |
| POST | `/api/productos` | Function | 201 con `Location`, **400 ProblemDetails** si JSON malformado, **422 ValidationProblemDetails** si DataAnnotations falla |
| PUT | `/api/productos/{id}` | Function | actualización parcial; 200, 404 o 422 |
| DELETE | `/api/productos/{id}` | Function | 204 si OK, 404 si no existe |

## Tests

```bash
dotnet test
```

22 tests:

- `HelloFunctionTests` (3) — heredados del S3.1.
- `PingFunctionTests` (1).
- `ProductosListarTests` (5): sin filtros, filtro categoría, header `X-Total-Count`, clamp de `porPagina` al máximo de Options, filtro por `minPrecio`.
- `ProductosCrudTests` (9): GET 200/404, POST 201/422/400, PUT 200/404, DELETE 204/404.
- `InMemoryProductoServiceTests` (4): unit tests puros del servicio.

Helper `HttpRequestFactory` para fabricar `HttpRequest` con body JSON, query
strings o cuerpos malformados (sin necesidad de `WebApplicationFactory`).

## Tour del código

### `Program.cs` — middleware en orden estricto

```csharp
builder.UseMiddleware<CorrelationIdMiddleware>();        // primero: genera el ID
builder.UseMiddleware<ExceptionHandlingMiddleware>();    // segundo: lo envuelve
builder.ConfigureFunctionsWebApplication();
```

`CorrelationId` debe ir **antes** que `ExceptionHandling` para que cuando el
handler escriba el ProblemDetails ya tenga el header listo en la respuesta.

### `ExceptionHandlingMiddleware` (slides 14 + 24)

Captura cualquier excepción no controlada y devuelve un Problem Details
RFC 7807 con `traceId = context.InvocationId` para correlacionar con logs.
Si la función no es HTTP (Timer, Blob, etc.) deja que la excepción suba
para que el runtime de Functions la marque como fallo y haga retry.

### `ProductosFunctions.Crear` — el patrón de validación

```csharp
1. Try ReadFromJsonAsync<T>             → si falla por JSON malformado: 400
2. TryValidateObject(dto)               → si falla DataAnnotations: 422
3. service.Crear(dto)                    → 201 Created con Location header
```

Los tres caminos están cubiertos por tests. La separación 400 vs 422 es
intencional: 400 es "no entiendo la sintaxis", 422 es "entiendo la sintaxis
pero la semántica es inválida".

### `Listar` con `X-Total-Count`

```csharp
req.HttpContext.Response.Headers["X-Total-Count"] = total.ToString();
```

Cabecera estándar de paginación: el cliente sabe cuántas páginas hay sin
hacer un segundo request. El servicio devuelve `total` por separado para
poder calcularlo sin volver a recorrer.

## Ejecución local

```bash
cp src/AzureFunctions.Demo/local.settings.json.example src/AzureFunctions.Demo/local.settings.json
cd src/AzureFunctions.Demo
func start
```

Abre `api.http` en VS Code (extensión REST Client) y ejecuta los 10 requests
de muestra: muestran cada verbo, los filtros, los códigos de error y los
formatos de Problem Details.

## Despliegue por Portal

Igual que [S3.1](../S3.1-principios-computo-sin-servidor/README.md#despliegue-por-portal-de-azure)
— mismo Function App Consumption Linux .NET 10 isolated. Diferencia: en
**Configuration → Application settings** añade:

| Name | Value |
| --- | --- |
| `Productos__MaxPorPagina` | `100` |
| `Productos__PorPaginaPorDefecto` | `20` |

Tras el deploy, prueba con la function key (Portal → tu Function App → tu
función → "Get function URL" copia la URL con el `?code=...`).

## Despliegue alternativo con scripts

```bash
cd scripts
cp .env.demo.example .env.demo
# editar SUBSCRIPTION_ID, STORAGE único, FUNC único

bash 01-provision.sh        # RG + Storage + Function App + App Settings
bash 02-deploy.sh           # publish + zip + deploy
bash 03-smoke-test.sh       # 8 checks cubriendo todos los verbos
bash 04-cleanup.sh
```

`03-smoke-test.sh` lee la function key con `az functionapp keys list` y la
añade a cada URL — así no necesitas copiarla a mano.

## Hand-off al siguiente submódulo

[`S3.3 — Trigger Timer`](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.3-trigger-timer-v4.md)
añadirá triggers programados con expresiones CRON sobre el mismo skeleton.
Cambiará el atributo `[HttpTrigger]` por `[TimerTrigger("0 0 * * * *")]` —
lo demás (DI, middleware, logging) es idéntico.

## Lo que NO está aquí (deliberadamente)

| Tema | Slide | Cuándo lo verás |
| --- | --- | --- |
| OpenAPI/Swagger | 20 | S3.6 — Bindings (donde aparece más natural con DTOs) |
| Rate limiting | 21 | M07 con API Management |
| API versioning | 22 | M07 con API Management |
| HttpClient + Polly resilience | 23 | M07 con APIs de integración |
| FluentValidation | 25 | DataAnnotations basta para enseñar el patrón |
| Entra ID / Easy Auth | 11 | M06 — Seguridad y Auth |
