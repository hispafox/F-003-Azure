# S5.P — Práctica: Cosmos DB con Managed Identity

> **Submódulo de referencia:** [M05-S5.P](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.P-practica-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** < 1 € (Cosmos serverless + App F1)

> 🎓 **Práctica del módulo** — integra **S5.3** (Cosmos DB: partición,
> RU) + **S5.4** (Managed Identity: keyless). Entregable: una app que lee
> y escribe en Cosmos **sin una sola key ni password**.

> 📘 **¿Primera vez con esta práctica?** Lee el [MANUAL.md](MANUAL.md) —
> manual del alumno: el entregable, la prueba definitiva (regenerar la
> key y ver que la app sigue funcionando) y cómo justificar el diseño
> de partition key.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| `CosmosClient` con `DefaultAzureCredential` (cero keys) | [`Program.cs`](src/Cosmos.Mi.Demo.Api/Program.cs) |
| Credencial singleton compartida (patrón S5.4) | [`CredentialFactory.cs`](src/Cosmos.Mi.Demo.Api/Security/CredentialFactory.cs) |
| CRUD + RU por operación (patrón S5.3) | [`IProductoRepository.cs`](src/Cosmos.Mi.Demo.Api/Repositories/IProductoRepository.cs) |
| Partition key `/categoria`: ¿buena? | [`PartitionKeyAdvisor.cs`](src/Cosmos.Mi.Demo.Api/Cosmos/PartitionKeyAdvisor.cs) |
| Entregable zero-secrets: auditar App Settings | [`ZeroSecretsAuditor.cs`](src/Cosmos.Mi.Demo.Api/Security/ZeroSecretsAuditor.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Crear Cosmos serverless + db/container | 4 | [`scripts/01-provision.sh`](scripts/01-provision.sh) |
| Habilitar MI en la Web App | 5 | `01-provision.sh` (`webapp identity assign`) |
| RBAC: Cosmos DB Built-in Data Contributor | 6 | `01-provision.sh` |
| App Setting solo con la URL (sin key) | 7 | `Program.cs` (`CosmosEndpoint`) + provision |
| `CosmosClient` + `DefaultAzureCredential` | 8 | [`Program.cs`](src/Cosmos.Mi.Demo.Api/Program.cs) |
| Local con `az login` (mismo código) | 9 | `CredentialFactory` + `appsettings.Development.json` |
| Partition key strategy | 11 | [`PartitionKeyAdvisor.cs`](src/Cosmos.Mi.Demo.Api/Cosmos/PartitionKeyAdvisor.cs) |
| Monitoring de RU (`RequestCharge`) | 12 | `IProductoRepository` (devuelve RU; single vs cross-partition) |
| Validación zero-secrets | 13, 19 | [`ZeroSecretsAuditor.cs`](src/Cosmos.Mi.Demo.Api/Security/ZeroSecretsAuditor.cs) + `02-smoke-test.sh` |
| Checklist de la práctica | 14 | este README + `02-smoke-test.sh` |
| Troubleshooting (MI, RBAC, propagación) | 20 | `02-smoke-test.sh` + sección abajo |
| Cleanup + costes | 24 | [`scripts/03-cleanup.sh`](scripts/03-cleanup.sh) |

## Estructura

```
S5.P-practica-cosmos-managed-identity/
├── src/Cosmos.Mi.Demo.Api/
│   ├── Security/   CredentialFactory (S5.4), ZeroSecretsAuditor
│   ├── Cosmos/     CosmosDefaults, PartitionKeyAdvisor (S5.3)
│   ├── Domain/     Producto + DTOs
│   ├── Repositories/ IProductoRepository (CRUD + RU)
│   ├── Endpoints/  ProductosEndpoints
│   └── Program.cs  TokenCredential singleton + CosmosClient keyless
├── tests/Cosmos.Mi.Demo.Api.Tests/
│   ├── Unit_*            PartitionKeyAdvisor, ZeroSecretsAuditor
│   ├── DiContainer_Tests grafo keyless real (sin Docker)
│   └── Integration_…     emulador Cosmos (key auth, SkippableFact)
└── scripts/        01-provision (Cosmos+MI+RBAC) / 02-smoke (zero-secrets) / 03-cleanup
```

## Tests

```bash
dotnet test     # 18 pass + 1 skip (emulador sin Docker) + 0 fail
```

- **CAPA 1 · Unit**: `PartitionKeyAdvisor` (4 reglas slide 11),
  `ZeroSecretsAuditor` (entregable slide 13/19).
- **CAPA 0 · DI**: resuelve el grafo **keyless** real
  (`TokenCredential` + `CosmosClient` + `Container` + repo) sin Docker
  ni red (ambos SDK son *lazy*); verifica la credencial singleton
  compartida. Cubre la [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA 2 · Integration** (`SkippableFact`): CRUD real contra el
  **emulador de Cosmos** vía la API completa. Se **salta** sin Docker.

> 🧠 **El emulador NO hace Managed Identity** (usa una key fija, no
> Entra ID). El test de integración **sustituye** el `CosmosClient`
> keyless por uno con key del emulador para ejercitar el CRUD y la
> partition key. El **camino keyless** se valida en CAPA 0 (DI) y a mano
> contra Azure (paso "regenerar la key" del `02-smoke-test.sh`, slide
> 13: si la app sigue funcionando tras rotar la key → de verdad no usa
> keys).

## Ejecución local

```bash
az login                       # DefaultAzureCredential usa tu identidad
# Opción A: emulador de Cosmos en Docker (CRUD local, key auth)
# Opción B: tu Cosmos real con tu user con rol de datos (keyless)
dotnet run --project src/Cosmos.Mi.Demo.Api
# http://localhost:5086  — usa src/Cosmos.Mi.Demo.Api/api.http
```

`/practica/*` funcionan offline. El CRUD necesita Cosmos accesible.

## Despliegue por Portal (entregable)

1. **Cosmos DB** — *Create resource → Azure Cosmos DB for NoSQL*,
   **Serverless**. *Data Explorer*: database `tienda`, container
   `productos` con partition key `/categoria` (slide 4).
2. **Web App** — *Settings → Identity → System assigned → On* (slide 5).
3. **RBAC de datos** — Cosmos → *Data plane RBAC*: asigna **Cosmos DB
   Built-in Data Contributor** al `principalId` de la Web App. (No es
   IAM de control plane; es role assignment de datos, slide 6.)
4. **App Setting** — Web App → *Environment variables* →
   `CosmosEndpoint = https://<cuenta>.documents.azure.com:443/`
   (solo la URL, **sin AccountKey**, slide 7).
5. **Desplegar** la app y verificar `POST/GET /productos` (slide 10).
6. **Validar zero-secrets** (slide 13): los App Settings solo deben
   tener `CosmosEndpoint`. Prueba definitiva: **regenera la key** de
   Cosmos — si la app sigue funcionando, de verdad usa Managed Identity.

> Scripts `az` en [`scripts/`](scripts) (`./demo.sh`) hacen los pasos
> 1-4 y la verificación zero-secrets. Complemento de clase, no sustituto
> del Portal.

## Troubleshooting (slide 20)

| Error | Causa típica | Fix |
| --- | --- | --- |
| `Forbidden` en Cosmos | RBAC sin propagar (5-10 min) o scope mal | esperar / revisar `cosmosdb sql role assignment list` |
| `DefaultAzureCredential failed` (local) | sin `az login` | `az login` |
| `403` tras deploy | MI no habilitada o rol no asignado | `webapp identity show` + role assignment |
| RU altísimas | query cross-partition | incluir `categoria` (la partition key) |

## Próximo paso

[`S5.P2 — Práctica: Table Storage CRUD`](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.P2-practica-table-storage-crud-v1.md):
cierra el módulo M05.
