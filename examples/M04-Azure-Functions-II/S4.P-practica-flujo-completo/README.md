# S4.P — Práctica: flujo completo Cosmos → Function → Blob → Queue

> **Submódulo de referencia:** [M04-S4.P](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.P-practica-flujo-completo-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption
> **Servicios:** Cosmos DB serverless + Azure Storage (Blob + Queue) · coste ~0 €

## Objetivo

Práctica **integradora de M04**: un sistema event-driven de 3 saltos que
combina lo de S3.5 (Cosmos Change Feed), S3.6 (multi-output bindings) y
S4.1 (Queue), con **idempotencia por máquina de estados** (slide 11).

```
POST /api/pedidos
   │ (HTTP, valida + total)
   ▼
[CosmosDBOutput] → Cosmos "pedidos" (estado: nuevo)
   │ Change Feed
   ▼
ProcesarNuevosPedidos
   ├─ skip si estado≠"nuevo" o ya facturado (idempotencia, slide 11)
   ├─ genera factura (IVA 21%)
   ├─ [BlobOutput]  → facturas/{guid}.json
   └─ [QueueOutput] → "facturas-generadas"
   │ Queue trigger
   ▼
NotificarFacturaGenerada → log + tracker

GET /api/estado → creados / facturados / notificados (verificación e2e)
```

> 🎯 **Idempotencia en 2 capas (slide 11)**: el documento lleva `estado`
> (`nuevo`→procesar, otro→skip) **y** un `IFlujoTracker.TryMarcarFacturado`
> (`TryAdd` atómico — la lección de S3.5). El Change Feed es at-least-once:
> reenviar el mismo pedido NO genera una segunda factura.

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| HTTP → CosmosDBOutput | 7 | [`CrearPedidoFunction.cs`](src/AzureFunctions.Demo/Functions/CrearPedidoFunction.cs) |
| CosmosDBTrigger + multi-output (Blob+Queue) | 8 | [`ProcesarNuevosPedidosFunction.cs`](src/AzureFunctions.Demo/Functions/ProcesarNuevosPedidosFunction.cs) |
| QueueTrigger (notificación) | 9 | [`NotificarFacturaFunction.cs`](src/AzureFunctions.Demo/Functions/NotificarFacturaFunction.cs) |
| Idempotencia end-to-end (estado + tracker) | 11 | `Pedido.Estado` + [`InMemoryFlujoTracker`](src/AzureFunctions.Demo/Services/IFlujoTracker.cs) |
| Verificación del flujo | 10 | `GET /api/estado` ([`EstadoFunction.cs`](src/AzureFunctions.Demo/Functions/EstadoFunction.cs)) |

## Tests

```bash
dotnet test     # 21/21 — sin Cosmos ni Storage reales
```

- **`PedidoFactoryTests`** (6) — cálculo del total, validación.
- **`FacturaGeneratorTests`** (5) — IVA 21%, redondeo, número de factura,
  JSON camelCase, mensaje de cola.
- **`FlujoFunctionsTests`** (10) — las 4 funciones: multi-output de
  CrearPedido, **idempotencia** (mismo pedido 2× → 1 factura), skip por
  estado, parsing del notificador, snapshot end-to-end.

> ⚠️ **DI verificado a mano** (lección del bug de S3.4): cada constructor
> de `[Function]` cruzado contra `Program.cs`. `IFlujoTracker` es Singleton
> — estado compartido por los 3 saltos + el endpoint de inspección.

## Despliegue por Portal

1. RG `rg-curso-m04-s4p` · Storage `stcursom04s4p{ini}` (LRS) → crea
   container **`facturas`** y queue **`facturas-generadas`**.
2. Cosmos DB **Serverless** `cosmos-curso-m04-s4p-{ini}` → DB `tienda`,
   container `pedidos` (PK `/clienteId`).
3. Function App .NET 10 Isolated / Linux / Consumption, ese Storage.
4. Configuration → `CosmosDbConnection` = primary connection string de Cosmos.
5. Deploy desde VS Code.
6. Probar:
   ```bash
   curl -X POST "https://func-...-{ini}.azurewebsites.net/api/pedidos?code=KEY" \
     -H "Content-Type: application/json" \
     -d '{"clienteId":"c1","clienteNombre":"P","items":[{"productoId":"p","nombre":"x","cantidad":1,"precioUnitario":100}]}'
   sleep 20
   curl "https://func-...-{ini}.azurewebsites.net/api/estado?code=KEY"
   # creados=1, facturados=1, notificados=1
   ```
   Mira el blob `facturas/` (1 JSON) y la queue (vacía, ya consumida).
7. Borra el RG.

(También `scripts/demo.sh` para hacerlo por CLI — Cosmos serverless ~0 €.)

## Rúbrica de "done"

```
[x] POST /pedidos crea el documento en Cosmos
[x] El Change Feed dispara la facturación
[x] La factura se escribe a Blob y el mensaje a Queue
[x] El Queue trigger notifica
[x] Reenviar el mismo pedido NO duplica la factura (idempotencia)
[x] GET /estado refleja los 3 saltos
[x] Tests obligatorios 21/21 + DI cruzado a mano
```

## Próximo paso

[`S4.P2 — Práctica: Durable Hello World`](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.P2-practica-durable-hello-world-v1.md)
cierra M04 con una práctica corta de Durable Functions.
