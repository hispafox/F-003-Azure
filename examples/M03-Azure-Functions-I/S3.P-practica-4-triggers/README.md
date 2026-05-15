# S3.P — Práctica: Funciones con los 4 tipos de triggers

> **Submódulo de referencia:** [M03-S3.P](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.P-practica-4-triggers-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption

## Objetivo

Esta es la **práctica integradora del Módulo 3**: una sola Function App con
los 4 triggers que has visto en S3.2 a S3.5 conviviendo en el mismo proceso.

| Trigger | Endpoint / fuente | Demuestra |
| --- | --- | --- |
| **HTTP** | `GET/POST /api/productos`, `GET /api/estado` | S3.2 — API CRUD con DI |
| **Timer** | NCRONTAB `0 */1 * * * *` (cada minuto) | S3.3 — tarea programada |
| **Blob** | `uploads/{nombre}.csv` → `resultados/{nombre}-resumen.json` | S3.4 — trigger + output binding |
| **Cosmos DB** | Change Feed sobre `tienda/pedidos` | S3.5 — reacción a cambios |

> 🎯 **El patrón clave de la práctica**: los 4 triggers viven en el **mismo
> Function App** (el mismo proceso), comparten **3 singletons in-memory** vía
> DI (`IProductoService`, `ILimpiezaTracker`, `INotificacionLog`), y el endpoint
> `GET /api/estado` deja inspeccionar el efecto agregado desde una sola llamada.
> Esa convivencia es lo que hace a Functions económico: un consumption plan
> aloja N triggers de naturaleza distinta sin coste fijo.

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| HTTP trigger CRUD | 6 | [`ProductosApi.cs`](src/AzureFunctions.Demo/Functions/ProductosApi.cs) |
| Timer trigger con NCRONTAB | 7 | [`LimpiezaProgramadaFunction.cs`](src/AzureFunctions.Demo/Functions/LimpiezaProgramadaFunction.cs) |
| Blob trigger + BlobOutput | 8 | [`ProcesarCsvFunction.cs`](src/AzureFunctions.Demo/Functions/ProcesarCsvFunction.cs) |
| Cosmos DB Change Feed | 9 | [`ReaccionarPedidosFunction.cs`](src/AzureFunctions.Demo/Functions/ReaccionarPedidosFunction.cs) |
| Smoke tests automatizados | 11 | [`scripts/03-smoke-test.sh`](scripts/03-smoke-test.sh) |
| Checklist "done" | 16, 21 | sección **Rúbrica de "done"** abajo |
| Inspección estado en runtime | 13, 15 | `GET /api/estado` ([`EstadoFunction.cs`](src/AzureFunctions.Demo/Functions/EstadoFunction.cs)) |

## Estructura

```
S3.P-practica-4-triggers/
├── README.md
├── AzureFunctions.Demo.slnx
├── Directory.Build.props
├── global.json
├── src/AzureFunctions.Demo/
│   ├── Functions/
│   │   ├── HelloFunction.cs                (esqueleto)
│   │   ├── PingFunction.cs                 (Anonymous health)
│   │   ├── ProductosApi.cs                 ← Trigger 1/4 HTTP
│   │   ├── LimpiezaProgramadaFunction.cs   ← Trigger 2/4 Timer
│   │   ├── ProcesarCsvFunction.cs          ← Trigger 3/4 Blob
│   │   ├── ReaccionarPedidosFunction.cs    ← Trigger 4/4 Cosmos
│   │   └── EstadoFunction.cs               (GET /estado consolidado)
│   ├── Models/                             (Producto, Pedido, ResumenCsv, LimpiezaResultado)
│   ├── Services/                           (3 in-memory singletons)
│   ├── Middleware/
│   ├── host.json                           (extensions.cosmosDB feedPollDelay)
│   ├── local.settings.json.example
│   └── api.http
├── tests/AzureFunctions.Demo.Tests/        (22 tests)
└── scripts/                                (az CLI didáctico)
```

## Requisitos

- .NET SDK 10
- Suscripción de Azure (todo gratuito o céntimos)
- (Local) Azurite + Cosmos emulator

## Ejecución local

```bash
cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json
```

Asegúrate de tener corriendo:
- **Azurite** (Storage local) — `azurite --silent --location ./azurite-data`
- **Cosmos emulator** en `https://localhost:8081/`
- Crear database `tienda` + container `pedidos` (PK `/clienteId`)
- Crear containers `uploads` y `resultados` en Azurite

```bash
func start --csharp
```

> ⚠️ Yo no lanzo apps. Tú haces `func start`.

Al arrancar deberías ver los 6 endpoints + 3 triggers no-HTTP:

```
ListarProductos:          [GET]  http://localhost:7071/api/productos
GetProducto:              [GET]  http://localhost:7071/api/productos/{id}
CrearProducto:            [POST] http://localhost:7071/api/productos
Estado:                   [GET]  http://localhost:7071/api/estado
LimpiezaProgramada:       timerTrigger
ProcesarCsv:              blobTrigger
ReaccionarCambiosPedidos: cosmosDBTrigger
```

## Tests

```bash
dotnet test
```

22 tests sin runtime de Functions ni emuladores. Cobertura por trigger:

- **`ProductosApiTests`** (6) — listar, get por id existente/inexistente, POST
  válido/inválido/malformado.
- **`LimpiezaProgramadaFunctionTests`** (3) — registro de ejecución, acumulación
  en el tracker, manejo del flag `IsPastDue`.
- **`ProcesarCsvFunctionTests`** (5) — extracción de columnas y filas, preview
  limitado a 3, tolerancia a CRLF, CSV vacío, sólo cabecera.
- **`ReaccionarPedidosFunctionTests`** (3) — anotación al log, batch vacío/null,
  preservación de campos.
- **`EstadoFunctionTests`** (1) — snapshot consolidado de los 3 servicios.
- **`HelloFunctionTests`** + **`PingFunctionTests`** (4) — heredados.

## Despliegue por Portal de Azure

### 1) Resource Group

Portal → **Resource groups** → **Create** → `rg-curso-m03-s3p`.

### 2) Storage Account

Portal → **Storage accounts** → **Create**:
- Name: `stcursom03s3p{iniciales}`
- Performance: Standard, LRS

Una vez creado:
- **Containers** → **+ Container** → `uploads` (acceso privado)
- **Containers** → **+ Container** → `resultados` (acceso privado)

### 3) Cosmos DB

Portal → **Cosmos DB** → **Create** → **Azure Cosmos DB for NoSQL**:
- Capacity mode: **Serverless**
- Account name: `cosmos-curso-m03-s3p-{iniciales}`

Tras crear:
- **Data Explorer** → **+ New Container**
  - Database id: `tienda` (Create new)
  - Container id: `pedidos`
  - Partition key: `/clienteId`

### 4) Function App

Portal → **Function App** → **Create**:
- Runtime: **.NET 10 Isolated** (o **8 Isolated** si no está disponible)
- OS: **Linux**
- Plan: **Consumption**
- Storage: **usa el Storage del paso 2** (importante — el Blob trigger lee
  de ese mismo Storage vía `AzureWebJobsStorage`).

### 5) Conectar Cosmos

Cosmos account → **Keys** → copia **Primary Connection String**.

Function App → **Configuration** → **+ New application setting**:
- Name: **`CosmosDbConnection`**
- Value: el connection string copiado.

### 6) Deploy

VS Code → click derecho en el proyecto → **Deploy to Function App**.

### 7) Probar los 4 triggers

```bash
KEY="<function-key>"
APP="https://func-curso-m03-s3p-{iniciales}.azurewebsites.net"

# 1) HTTP
curl "$APP/api/productos?code=$KEY"
curl -X POST "$APP/api/productos?code=$KEY" \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Mouse","precio":29.99}'

# 2) Timer — corre solo cada minuto. Espera 60s y consulta:
curl "$APP/api/estado?code=$KEY"
# totalEjecuciones debería incrementarse en ~1/min

# 3) Blob — sube un CSV
echo "nombre,precio" > test.csv
echo "Laptop,999" >> test.csv
az storage blob upload --account-name stcursom03s3p{iniciales} \
  --container-name uploads --name test.csv --file test.csv --auth-mode login

# Espera 30-60s y comprueba el output:
az storage blob exists --account-name stcursom03s3p{iniciales} \
  --container-name resultados --name test-resumen.json --auth-mode login \
  --query exists

# 4) Cosmos — inserta un pedido en Data Explorer:
# { "id": "pedido-001", "clienteId": "cliente-A", "estado": "nuevo", "total": 150 }

# Espera 10s y comprueba:
curl "$APP/api/estado?code=$KEY"
# totalNotificaciones debería ser >= 1
```

### 8) Limpieza

Portal → **Resource groups** → `rg-curso-m03-s3p` → **Delete resource group**.

## Despliegue por scripts (CLI, opcional)

```bash
cd scripts
cp .env.demo.example .env.demo
# Edita .env.demo
./demo.sh
```

`03-smoke-test.sh` toca los 4 triggers en una sola pasada y verifica el efecto
en cada uno (espera ~2 min — el Timer y el Blob trigger en Consumption no son
instantáneos).

## Rúbrica de "done" (slide 21)

```
Mínimo (lo esperado):
[x] HTTP trigger devuelve JSON al llamarlo
[x] Timer trigger se ejecuta cada minuto (visible en /api/estado)
[x] Blob trigger procesa un CSV subido al container
[x] Cosmos trigger anota cambios en notificaciones
[x] Las 4 funciones en la MISMA Function App
[x] Tests obligatorios — 22/22 cubriendo handlers puros

Bien hecho:
[x] Logging estructurado en todas las funciones
[x] Output binding usado donde aplica (BlobOutput en Blob trigger)
[x] local.settings.json NO está en git
[x] README con setup local + deploy Portal + scripts CLI

Avanzado (queda como ejercicio):
[ ] Managed Identity en vez de connection strings (slide 19)
[ ] Application Insights con dashboard
[ ] CI/CD con GitHub Actions OIDC (slide 20)
```

## Próximo paso

[`S3.P2 — Práctica: HTTP CRUD en memoria`](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.P2-practica-http-crud-memoria-v1.md)
es una práctica más corta centrada en el HTTP trigger, sin dependencias
externas. Cierra el módulo M03 antes de pasar a Functions II.
