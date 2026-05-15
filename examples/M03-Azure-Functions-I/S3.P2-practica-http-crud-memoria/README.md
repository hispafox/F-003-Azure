# S3.P2 — Práctica: HTTP CRUD en memoria

> **Submódulo de referencia:** [M03-S3.P2](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.P2-practica-http-crud-memoria-v1.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption
> **Dependencias externas:** ninguna — solo Storage Account (lo pide el host)

## Objetivo

La práctica **más simple del módulo**: 5 endpoints HTTP CRUD sobre un repositorio
en memoria, sin Cosmos, sin Blob, sin Timer, sin Cosmos emulator ni Azurite (los
necesitarías para los otros 3 triggers, aquí no).

**Por qué empezar por aquí** (slide 2):

- Solo UN tipo de trigger → cero confusión sobre qué dispara qué
- Cero emuladores → arranca con `func start` y curl
- CRUD es un modelo mental conocido por cualquier dev
- Aprendes el ciclo de Functions sin el ruido de la persistencia

> 🎯 **Patrón clave**: el repositorio se registra como **Singleton** en DI
> (slide 6). Los datos persisten entre invocaciones mientras la instancia
> está caliente, **pero se pierden cuando se reinicia o escala** (slide 12,
> limitación deliberada). Para persistencia real → Cosmos en el módulo 5.

## Endpoints

| Método | Ruta | Resultado | Slide |
| --- | --- | --- | --- |
| GET | `/api/productos` | 200 con lista (3 seed iniciales) | 7 |
| GET | `/api/productos/{id}` | 200 con producto, o 404 | 7 |
| POST | `/api/productos` | 201 + producto creado, o 400 si inválido | 7 |
| PUT | `/api/productos/{id}` | 200 + producto, o 404 si id no existe | 7 |
| DELETE | `/api/productos/{id}` | 204, o 404 si id no existe | 7 |

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| Modelo `Producto` como record | 5 | [`Producto.cs`](src/AzureFunctions.Demo/Models/Producto.cs) |
| Repositorio en memoria con `ConcurrentDictionary` | 5 | [`InMemoryProductoService.cs`](src/AzureFunctions.Demo/Services/InMemoryProductoService.cs) |
| Singleton en DI | 6 | [`Program.cs`](src/AzureFunctions.Demo/Program.cs) |
| 5 endpoints HTTP | 7 | [`ProductosApi.cs`](src/AzureFunctions.Demo/Functions/ProductosApi.cs) |
| Probar con curl | 8 | [`api.http`](src/AzureFunctions.Demo/api.http) + [`scripts/03-smoke-test.sh`](scripts/03-smoke-test.sh) |
| Provision en Azure | 10 | [`scripts/01-provision.sh`](scripts/01-provision.sh) |
| Smoke tests automatizados | 14 | [`scripts/03-smoke-test.sh`](scripts/03-smoke-test.sh) |
| Tests unitarios del repo | 15 | [`InMemoryProductoServiceTests.cs`](tests/AzureFunctions.Demo.Tests/InMemoryProductoServiceTests.cs) |
| Limitación del estado en memoria | 12 | sección **Limitaciones** abajo |
| Cleanup obligatorio | 17 | [`scripts/04-cleanup.sh`](scripts/04-cleanup.sh) |

## Estructura

```
S3.P2-practica-http-crud-memoria/
├── README.md
├── AzureFunctions.Demo.slnx
├── Directory.Build.props
├── global.json
├── src/AzureFunctions.Demo/
│   ├── Functions/
│   │   ├── HelloFunction.cs                (esqueleto)
│   │   ├── PingFunction.cs                 (Anonymous health)
│   │   └── ProductosApi.cs                 ← 5 endpoints CRUD
│   ├── Models/
│   │   └── Producto.cs                     (record + CrearProductoDto)
│   ├── Services/
│   │   ├── IProductoService.cs
│   │   └── InMemoryProductoService.cs
│   ├── Middleware/
│   ├── host.json                           (sin extensions especiales)
│   ├── local.settings.json.example
│   └── api.http
├── tests/AzureFunctions.Demo.Tests/        (26 tests)
└── scripts/                                (az CLI didáctico)
    ├── 01-provision.sh                     (RG + Storage + Function App)
    ├── 02-deploy.sh
    ├── 03-smoke-test.sh                    (5/5 endpoints)
    └── 04-cleanup.sh
```

## Requisitos

- .NET SDK 10
- Suscripción de Azure (gratuita; primeras 1M ejecuciones gratis en Consumption)
- (Local) Azurite para `AzureWebJobsStorage` — opcional si haces deploy directo

## Ejecución local

```bash
cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json

# Asegúrate de que Azurite está corriendo (lo necesita AzureWebJobsStorage)
azurite --silent --location ./azurite-data &

func start --csharp
```

> ⚠️ Yo no lanzo apps. Tú haces `func start`.

```bash
# 1) Listar (3 productos seed: p001, p002, p003)
curl http://localhost:7071/api/productos

# 2) Crear
curl -X POST http://localhost:7071/api/productos \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Mouse","precio":29.99,"stock":15}'

# 3) Actualizar
curl -X PUT http://localhost:7071/api/productos/p001 \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Laptop XPS","precio":1499,"stock":3}'

# 4) Borrar
curl -X DELETE http://localhost:7071/api/productos/p002 -w "Status: %{http_code}\n"
```

## Tests

```bash
dotnet test
```

26 tests sin runtime de Functions:

- **`InMemoryProductoServiceTests`** (9) — slide 15: contrato del repositorio,
  generación de id único, idempotencia del Borrar inexistente, thread-safety
  bajo paralelismo de 100 inserts.
- **`ProductosApiTests`** (13) — los 5 endpoints HTTP completos:
  - Listar: devuelve los 3 seed.
  - Obtener: existente → 200, inexistente → 404.
  - Crear: válido → 201 + auto-id, sin nombre → 400, precio ≤ 0 → 400, stock
    negativo → 400, JSON malformado → 400.
  - Actualizar: existente → 200, inexistente → 404 (NO crea — PUT no es upsert),
    body inválido → 400.
  - Borrar: existente → 204, inexistente → 404.
- **`HelloFunctionTests`** + **`PingFunctionTests`** (4) — heredados del esqueleto.

## Despliegue por Portal de Azure

### 1) Resource Group

Portal → **Resource groups** → **Create** → `rg-curso-m03-s3p2`.

### 2) Storage Account

Portal → **Storage accounts** → **Create**:
- Name: `stcursom03s3p2{iniciales}`
- Performance: Standard, LRS

> ℹ️ Aunque esta práctica **no usa** Storage, Functions lo necesita
> obligatoriamente para `AzureWebJobsStorage` (slide 10).

### 3) Function App

Portal → **Function App** → **Create**:
- Runtime: **.NET 10 Isolated** (o **8 Isolated**)
- OS: **Linux**
- Plan: **Consumption (Serverless)**
- Storage: el del paso 2

### 4) Deploy

VS Code → click derecho en el proyecto → **Deploy to Function App** → selecciona
el Function App del paso 3.

### 5) Probar

```bash
KEY="<function-key>"
APP="https://func-curso-m03-s3p2-{iniciales}.azurewebsites.net/api"

curl "$APP/productos?code=$KEY"

curl -X POST "$APP/productos?code=$KEY" \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Mouse","precio":29.99,"stock":15}'
```

> ⚠️ El **primer request tarda 5-15 segundos** (cold start del Consumption Plan).
> Los siguientes son rápidos. Si la function lleva ~20 min sin tráfico, vuelve a
> dormirse → siguiente request será cold start otra vez.

### 6) Limpieza

Portal → **Resource groups** → `rg-curso-m03-s3p2` → **Delete resource group**.

## Despliegue por scripts (CLI, opcional)

```bash
cd scripts
cp .env.demo.example .env.demo
# Edita .env.demo con tus valores
./demo.sh
```

`03-smoke-test.sh` ejecuta los 5 endpoints en una pasada y verifica los códigos
HTTP esperados (200/201/204).

## Limitaciones (slide 12)

```
Los datos creados en memoria SE PIERDEN cuando:
- La function se reinicia
- Cambia de instancia (escalado)
- Pasa el tiempo de inactividad (~20 min sin tráfico → cold start)

Cuando arranca de nuevo, los datos del Seed() vuelven a aparecer
(p001, p002, p003), pero todo lo que hayas creado vía POST se ha ido.
```

Esto es **a propósito** en esta práctica. Cuando notes la limitación, estarás
listo para el módulo 5 (Cosmos DB).

## Functions vs Web App (slide 16)

|  | Web App (M02) | **Function (M03)** |
| --- | --- | --- |
| Coste con bajo tráfico | ~€13/mes mínimo (B1) | **€0** (free tier hasta 1M/mes) |
| Cold start | Solo en F1 | **Siempre en Consumption** |
| Latency mínima | <100 ms | 200-500 ms warm; 5-15 s cold |
| Estado | Cualquiera | **No state entre invocations** |
| Triggers | HTTP only | HTTP, Timer, Blob, Cosmos, etc. |

**Cuándo Function**: APIs esporádicas, webhooks, tareas event-driven.
**Cuándo Web App**: APIs con tráfico constante, SPAs, latencia crítica.

## Rúbrica de "done" (slide 21)

```
Mínimo aceptable:
[x] Function App pública con los 5 endpoints accesibles
[x] CRUD funciona correctamente
[x] Tests obligatorios — 26/26 verdes

Bien hecho:
[x] Logging estructurado en cada endpoint
[x] local.settings.json NO está en git (.gitignore)
[x] Tests del repositorio cubren contrato + thread-safety
[x] README con setup local + deploy Portal + scripts CLI

Avanzado (queda como ejercicio):
[ ] FluentValidation en vez de validación manual (reto 1)
[ ] OpenAPI / Swagger generado (reto 3)
[ ] Filtros con query strings (reto 4)
[ ] Reemplazar repo en memoria por Azure Tables o Cosmos (reto avanzado)
```

## Próximo paso

**Fin del módulo 3**. Has visto los 4 triggers (HTTP, Timer, Blob, Cosmos), los
bindings de entrada y salida (S3.6), y dos prácticas integradoras (S3.P, S3.P2).

Lo que viene en **M04 — Azure Functions II**:

- Service Bus (mensajería transaccional)
- Event Grid (eventos de Azure y custom)
- Durable Functions (orchestrators stateful)
- Patrones avanzados: fan-out/fan-in, async APIs
- Manejo de errores con dead-letter queues
- Testing automatizado en profundidad
