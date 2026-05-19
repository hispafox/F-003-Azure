# M05 — Almacenamiento y BBDD · ejemplos

Ejemplos de código que acompañan al
[Módulo 5 — Almacenamiento y BBDD](../../doc/M05-Almacenamiento-BBDD).

Cambia el stack respecto a M03/M04: ya no son Azure Functions sino
**aplicaciones que consumen servicios de datos** (Storage, Azure SQL,
Cosmos DB) desde sus SDKs. El patrón de tests vuelve al de M02
(`WebApplicationFactory`) + integración real con emuladores
(**Testcontainers.Azurite**, marcada como `SkippableFact` para que
`dotnet test` siga verde sin Docker).

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| [S5.1](../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.1-azure-storage-v3.md) | Azure Storage (Blob/Table/Queue/File) | [`S5.1-azure-storage/`](S5.1-azure-storage/README.md) | ✅ Disponible |
| [S5.2](../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.2-azure-sql-database-v3.md) | Azure SQL Database (EF Core, migraciones, retry) | [`S5.2-azure-sql-database/`](S5.2-azure-sql-database/README.md) | ✅ Disponible |
| [S5.3](../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.3-cosmosdb-v3.md) | Cosmos DB (partición, RU, consistencia, soft delete) | [`S5.3-cosmosdb/`](S5.3-cosmosdb/README.md) | ✅ Disponible |
| [S5.4](../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.4-managed-identity-v3.md) | Managed Identity (keyless, RBAC mínimo, Key Vault refs) | [`S5.4-managed-identity/`](S5.4-managed-identity/README.md) | ✅ Disponible |
| [S5.5](../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.5-backups-v3.md) | Backups, replicación y DR (RPO/RTO, soft delete, retención) | [`S5.5-backups-dr/`](S5.5-backups-dr/README.md) | ✅ Disponible |
| [S5.P](../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.P-practica-v3.md) | Práctica — Cosmos DB con Managed Identity (integra S5.3+S5.4) | [`S5.P-practica-cosmos-managed-identity/`](S5.P-practica-cosmos-managed-identity/README.md) | ✅ Disponible |
| [S5.P2](../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.P2-practica-table-storage-crud-v1.md) | Práctica — Table Storage CRUD | [`S5.P2-practica-table-storage-crud/`](S5.P2-practica-table-storage-crud/README.md) | ✅ Disponible |

✅ **Módulo M05 completo** (5 submódulos + 2 prácticas, 7/7).

## Coste

Casi todo es **~0 €**: Storage Account guarda pocos KB en las demos,
Cosmos serverless cobra por uso, Azure SQL tiene tier serverless con
auto-pausa. Aun así, **borrar el RG al acabar** cada práctica (los
scripts traen `cleanup`).

## Requisitos comunes

- .NET SDK 10
- Suscripción de Azure
- Azure CLI (`az`)
- (Opcional) Docker para los integration tests con Azurite — si no está,
  esos tests se **saltan** y la suite sigue verde.
