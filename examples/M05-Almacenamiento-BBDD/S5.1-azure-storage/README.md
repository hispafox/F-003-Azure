# S5.1 — Azure Storage: Blob, Table, Queue y File

> **Submódulo de referencia:** [M05-S5.1](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.1-azure-storage-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** ~0 € (StorageV2, pocos KB)

> ℹ️ Cambia el stack respecto a M03/M04: ya no son Azure Functions sino
> una **Minimal API** que encapsula los 4 SDKs de Storage tras repos. El
> patrón de tests vuelve al de M02 (`WebApplicationFactory`) + integración
> real con **Testcontainers.Azurite**.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) —
> manual del alumno: el para qué, el porqué (decisión Blob/Table/Queue/File,
> coste, durabilidad, seguridad) y cómo ponerlo en marcha y probarlo.

## Objetivo

Dominar los 4 servicios que viven dentro de un Storage Account (slide 3):

| Servicio | Para qué | SDK | Endpoint |
| --- | --- | --- | --- |
| **Blob** | archivos: PDFs, imágenes, backups, exports | `Azure.Storage.Blobs` | `/blob/*` |
| **Table** | NoSQL key-value barato (audit, config) | `Azure.Data.Tables` | `/table/*` |
| **Queue** | mensajería simple async | `Azure.Storage.Queues` | `/queue/*` |
| **File** | file share SMB/NFS (un NAS en la nube) | `Azure.Storage.Files.Shares` | (sin endpoint — ver nota) |

> 🎯 **Cuándo cada uno (slides 3, 11)**: Blob = archivos · Table = NoSQL
> simple y **barato** (~0.04 €/GB vs Cosmos ~25 €/GB) sin queries
> complejas · Queue = colas simples (Service Bus si necesitas
> topics/sessions/transacciones, ver M04) · File = compartir como un NAS.

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Storage Account StorageV2 + flags seguridad | 3 | [`scripts/01-provision.sh`](scripts/01-provision.sh) |
| Redundancia (LRS/ZRS/GRS) | 4 | README (decisión); LRS en provision |
| Access tiers + Lifecycle | 5 | [`AccessTierPolicy.cs`](src/Storage.Demo.Api/Storage/AccessTierPolicy.cs) + lifecycle JSON en provision |
| Blob: jerarquía "carpetas" | 6 | [`BlobPath.cs`](src/Storage.Demo.Api/Storage/BlobPath.cs) |
| Blob CRUD con SDK | 7-10 | [`IBlobRepository.cs`](src/Storage.Demo.Api/Repositories/IBlobRepository.cs) |
| Conexión: connection string vs Managed Identity | 7 | [`Program.cs`](src/Storage.Demo.Api/Program.cs) (URI → DefaultAzureCredential) |
| Table Storage CRUD + query PartitionKey | 11-12 | [`ITableRepository.cs`](src/Storage.Demo.Api/Repositories/ITableRepository.cs) |
| Queue Storage send/receive | 18-19 | [`IQueueRepository.cs`](src/Storage.Demo.Api/Repositories/IQueueRepository.cs) |
| File share | 3, 20 | [`IFileShareRepository.cs`](src/Storage.Demo.Api/Repositories/IFileShareRepository.cs) |

## Estructura

```
S5.1-azure-storage/
├── src/Storage.Demo.Api/
│   ├── Storage/        BlobPath, AccessTierPolicy   (lógica pura)
│   ├── Repositories/   IBlob/ITable/IQueue/IFileShare + impls (SDK)
│   ├── Models/         BlobItemDto, AuditEntity, DTOs
│   ├── Endpoints/      StorageEndpoints (Minimal API)
│   ├── Program.cs      (cs vs Managed Identity por config)
│   └── appsettings*.json / api.http
├── tests/Storage.Demo.Api.Tests/
│   ├── Unit_BlobPathTests / Unit_AccessTierPolicyTests
│   └── Integration_AzuriteTests   (Testcontainers, SkippableFact)
└── scripts/            01-provision (Storage+lifecycle) / 02-smoke / 03-cleanup
```

## Tests

```bash
dotnet test     # 18 pass + 1 skip (integration sin Docker) + 0 fail
```

- **Unit** (17): `BlobPath` (ruta jerárquica, prefijo de mes, "carpeta")
  y `AccessTierPolicy` (curva Hot→Cool→Archive→borrado, slide 5). Rápidos,
  sin Azure.
- **Integration** (1, `SkippableFact`): round-trip **real** Blob+Table+Queue
  contra **Azurite** levantado con Testcontainers, a través de la API
  completa (`WebApplicationFactory<Program>` con `StorageConnection`
  apuntando al container). Se **salta** si Docker no está → la suite
  siempre verde (patrón de M04-S4.5).

> ⚠️ **Azure Files no se testea en integración**: Azurite emula solo
> Blob/Queue/Table, **no** Azure Files. El `IFileShareRepository` y su
> contrato existen para ver el SDK; se valida contra un Storage real
> (paso "File share" en `01-provision.sh` lo crea).

## Ejecución local

```bash
# 1. Azurite (emulador de Storage)
azurite --silent --location ./.azurite

# 2. La API (la lanzas tú)
dotnet run --project src/Storage.Demo.Api
# http://localhost:5080  — usa src/Storage.Demo.Api/api.http
```

`appsettings.Development.json` ya apunta a Azurite
(`UseDevelopmentStorage=true`).

## Despliegue / Storage real

```bash
cd scripts
cp .env.demo.example .env.demo   # edita STORAGE (único global), RG, sub
./demo.sh                        # 1) provisiona  2) smoke  3) cleanup
```

`01-provision.sh` crea el Storage Account StorageV2 (LRS, TLS1.2, sin
acceso público anónimo — slide 3), los contenedores/cola/share y una
**lifecycle policy** (slide 5: Cool a 30 d, Archive a 180 d, borrado a
365 d sobre `facturas/`).

Para que la API use el Storage real con **Managed Identity** (sin
secretos, recomendado — se profundiza en **M05-S5.4**): configura
`StorageAccountUri=https://<cuenta>.blob.core.windows.net` y deja
`StorageConnection` vacío; `Program.cs` usará `DefaultAzureCredential`.

## Redundancia: cuál elegir (slide 4)

```
Desarrollo / práctica       → LRS   (3 copias, 1 datacenter)
Producción estándar         → ZRS   (3 zonas, misma región)
Producción crítica con DR   → GRS / GZRS  (+ región par)
Lectura distribuida / DR ya → RA-GRS / RA-GZRS
```

## Próximo paso

[`S5.2 — Azure SQL Database`](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.2-azure-sql-database-v3.md):
el motor relacional gestionado, con EF Core, migraciones y tuning.
