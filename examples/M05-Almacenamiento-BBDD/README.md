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
| S5.2 | Azure SQL Database | _Pendiente_ | ⏳ |
| S5.3 | Cosmos DB | _Pendiente_ | ⏳ |
| S5.4 | Managed Identity | _Pendiente_ | ⏳ |
| S5.5 | Backups | _Pendiente_ | ⏳ |
| S5.P | Práctica | _Pendiente_ | ⏳ |
| S5.P2 | Práctica — Table Storage CRUD | _Pendiente_ | ⏳ |

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
