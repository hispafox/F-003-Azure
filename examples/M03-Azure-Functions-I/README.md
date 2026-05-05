# M03 — Azure Functions I · ejemplos

Ejemplos de código que acompañan al
[Módulo 3 — Azure Functions I](../../doc/M03-Azure-Functions-I).

Cambia el stack respecto a M01/M02: pasamos de **Minimal API** sobre App Service
a **Azure Functions isolated worker** sobre plan **Consumption**. Mismo
`net10.0`, mismos tests con xUnit pero con un patrón distinto (sin
`WebApplicationFactory` — los tests instancian las funciones directamente).

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| [S3.1](../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.1-principios-computo-sin-servidor-v4.md) | Principios del cómputo sin servidor | [`S3.1-principios-computo-sin-servidor/`](S3.1-principios-computo-sin-servidor/README.md) | ✅ Disponible |
| [S3.2](../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.2-trigger-http-v4.md) | Trigger HTTP (CRUD completo) | [`S3.2-trigger-http/`](S3.2-trigger-http/README.md) | ✅ Disponible |
| [S3.3](../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.3-trigger-timer-v4.md) | Trigger Timer (CRON + idempotencia) | [`S3.3-trigger-timer/`](S3.3-trigger-timer/README.md) | ✅ Disponible |
| S3.4 | Trigger Blob Storage | _Pendiente_ | ⏳ |
| S3.5 | Trigger Cosmos DB Change Feed | _Pendiente_ | ⏳ |
| S3.6 | Bindings de entrada y salida | _Pendiente_ | ⏳ |
| S3.P | Práctica — 4 triggers | _Pendiente_ | ⏳ |
| S3.P2 | Práctica — HTTP CRUD en memoria | _Pendiente_ | ⏳ |

## Hilo conductor

S3.1 establece el **skeleton canónico** (Program.cs, host.json, csproj con
los paquetes correctos del Worker SDK 2.x, patrón de tests). S3.2 a S3.5
añaden cada uno **un trigger nuevo** sobre ese mismo skeleton. S3.6 amplía
con **input/output bindings**. Las dos prácticas (S3.P y S3.P2) consolidan
todo lo aprendido.

## Requisitos comunes

- .NET SDK 10
- Suscripción de Azure
- (opcional) Azure Functions Core Tools (`func`) para `func start` en local
- (opcional) Azurite para emular Storage en local
- Azure CLI (`az`) para los scripts
