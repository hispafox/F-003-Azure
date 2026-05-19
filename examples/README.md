# Ejemplos de código — F-003-Azure

Esta carpeta contiene los proyectos de código que acompañan a las clases del curso.
Cada ejemplo es **autocontenido** (con su propia solución `.slnx` y sus tests) y se
mapea a un submódulo concreto de [`doc/`](../doc).

## Convenciones

- **TFM por defecto:** `net10.0` aunque las clases mencionen .NET 8. Las APIs son
  backward-compatible y mantenemos el código sobre la última LTS.
- **Estructura por ejemplo:**
  ```
  ExampleRoot/
  ├── README.md
  ├── <Solucion>.slnx
  ├── Directory.Build.props
  ├── src/<Proyecto>/
  └── tests/<Proyecto>.Tests/
  ```
- **Tests obligatorios:** xUnit + `WebApplicationFactory<Program>` para las APIs.
  Excepción: las prácticas que son **puramente CLI** (como S1.P2 — Cloud Shell)
  no llevan proyecto .NET; la validación se hace con un `06-smoke-tests.sh`.
- **Despliegue Azure:** los pasos del README siempre se documentan por **Portal**.
  Algunos ejemplos incluyen además scripts `az` opcionales en `scripts/` para
  escenificar la demo en clase — siempre como complemento, nunca como sustituto
  de la guía del Portal.
- **No lanzo apps:** los `dotnet run` los hace el alumno; la verificación automática
  se queda en `dotnet build` + `dotnet test`.

## Índice

| Módulo | Submódulo | Ejemplo | Estado |
| --- | --- | --- | --- |
| [M01 — Intro Azure](M01-Intro-Azure/README.md) | S1.P — Práctica: Hello World end-to-end | [HelloWorld](M01-Intro-Azure/S1.P-practica-helloworld/README.md) | ✅ Disponible |
| [M01 — Intro Azure](M01-Intro-Azure/README.md) | S1.P2 — Práctica: Cloud Shell (solo CLI, sin .NET) | [`scripts/`](M01-Intro-Azure/S1.P2-practica-cloud-shell/README.md) | ✅ Disponible |
| [M02 — App Services](M02-App-Services/README.md) | S2.1 — Creación, configuración y publicación | [AppService.Demo.Api](M02-App-Services/S2.1-creacion-config-publicacion/README.md) | ✅ Disponible |
| [M02 — App Services](M02-App-Services/README.md) | S2.2 — Slots staging / producción | [AppService.Demo.Slots](M02-App-Services/S2.2-slots-staging-produccion/README.md) | ✅ Disponible |
| [M02 — App Services](M02-App-Services/README.md) | S2.3 — Escalado automático | [AppService.Demo.Scale](M02-App-Services/S2.3-escalado-automatico-planes/README.md) | ✅ Disponible |
| [M02 — App Services](M02-App-Services/README.md) | S2.4 — Variables y configuración segura | [AppService.Demo.Config](M02-App-Services/S2.4-variables-conexion-config-segura/README.md) | ✅ Disponible |
| [M02 — App Services](M02-App-Services/README.md) | S2.5 — Monitorización y diagnóstico | [AppService.Demo.Monitor](M02-App-Services/S2.5-monitorizacion-diagnostico/README.md) | ✅ Disponible |
| [M02 — App Services](M02-App-Services/README.md) | S2.P — Práctica: slots y swap | [AppService.Practica.Slots](M02-App-Services/S2.P-practica-slots-swap/README.md) | ✅ Disponible |
| [M02 — App Services](M02-App-Services/README.md) | S2.P2 — Práctica: deploy básico | [MiPrimeraWebApp](M02-App-Services/S2.P2-practica-deploy-basico/README.md) | ✅ Disponible |
| [M03 — Azure Functions I](M03-Azure-Functions-I/README.md) | S3.1 — Principios del cómputo sin servidor | [AzureFunctions.Demo](M03-Azure-Functions-I/S3.1-principios-computo-sin-servidor/README.md) | ✅ Disponible |
| [M03 — Azure Functions I](M03-Azure-Functions-I/README.md) | S3.2 — Trigger HTTP (CRUD completo) | [AzureFunctions.Demo](M03-Azure-Functions-I/S3.2-trigger-http/README.md) | ✅ Disponible |
| [M03 — Azure Functions I](M03-Azure-Functions-I/README.md) | S3.3 — Trigger Timer (CRON + idempotencia) | [AzureFunctions.Demo](M03-Azure-Functions-I/S3.3-trigger-timer/README.md) | ✅ Disponible |
| [M03 — Azure Functions I](M03-Azure-Functions-I/README.md) | S3.4 — Trigger Blob Storage (CSV import) | [AzureFunctions.Demo](M03-Azure-Functions-I/S3.4-trigger-blob-storage/README.md) | ✅ Disponible |
| [M03 — Azure Functions I](M03-Azure-Functions-I/README.md) | S3.5 — Trigger Cosmos DB Change Feed | [AzureFunctions.Demo](M03-Azure-Functions-I/S3.5-trigger-cosmosdb-changefeed/README.md) | ✅ Disponible |
| [M03 — Azure Functions I](M03-Azure-Functions-I/README.md) | S3.6 — Bindings de entrada y salida | [AzureFunctions.Demo](M03-Azure-Functions-I/S3.6-bindings-entrada-salida/README.md) | ✅ Disponible |
| [M03 — Azure Functions I](M03-Azure-Functions-I/README.md) | S3.P — Práctica: 4 triggers | [AzureFunctions.Demo](M03-Azure-Functions-I/S3.P-practica-4-triggers/README.md) | ✅ Disponible |
| [M03 — Azure Functions I](M03-Azure-Functions-I/README.md) | S3.P2 — Práctica: HTTP CRUD en memoria | [AzureFunctions.Demo](M03-Azure-Functions-I/S3.P2-practica-http-crud-memoria/README.md) | ✅ Disponible |
| [M04 — Azure Functions II](M04-Azure-Functions-II/README.md) | S4.1 — Event Grid + Service Bus | [AzureFunctions.Demo](M04-Azure-Functions-II/S4.1-event-grid-service-bus/README.md) | ✅ Disponible |
| [M04 — Azure Functions II](M04-Azure-Functions-II/README.md) | S4.2 — Durable Functions | [AzureFunctions.Demo](M04-Azure-Functions-II/S4.2-durable-functions/README.md) | ✅ Disponible |
| [M04 — Azure Functions II](M04-Azure-Functions-II/README.md) | S4.3 — Errores, reintentos, dead-letter | [AzureFunctions.Demo](M04-Azure-Functions-II/S4.3-errores-reintentos-deadletter/README.md) | ✅ Disponible |
| [M04 — Azure Functions II](M04-Azure-Functions-II/README.md) | S4.4 — Despliegue y versionado | [AzureFunctions.Demo](M04-Azure-Functions-II/S4.4-despliegue-versionado/README.md) | ✅ Disponible |
| [M04 — Azure Functions II](M04-Azure-Functions-II/README.md) | S4.5 — Testing y depuración | [AzureFunctions.Demo](M04-Azure-Functions-II/S4.5-testing-depuracion/README.md) | ✅ Disponible |
| [M04 — Azure Functions II](M04-Azure-Functions-II/README.md) | S4.P — Práctica: flujo completo | [AzureFunctions.Demo](M04-Azure-Functions-II/S4.P-practica-flujo-completo/README.md) | ✅ Disponible |
| [M04 — Azure Functions II](M04-Azure-Functions-II/README.md) | S4.P2 — Práctica: Durable Hello World | [AzureFunctions.Demo](M04-Azure-Functions-II/S4.P2-practica-durable-hello-world/README.md) | ✅ Disponible |
| [M05 — Almacenamiento y BBDD](M05-Almacenamiento-BBDD/README.md) | S5.1 — Azure Storage (Blob/Table/Queue/File) | [Storage.Demo.Api](M05-Almacenamiento-BBDD/S5.1-azure-storage/README.md) | ✅ Disponible |
| [M05 — Almacenamiento y BBDD](M05-Almacenamiento-BBDD/README.md) | S5.2 — Azure SQL Database (EF Core, migraciones, retry) | [Sql.Demo.Api](M05-Almacenamiento-BBDD/S5.2-azure-sql-database/README.md) | ✅ Disponible |
| [M05 — Almacenamiento y BBDD](M05-Almacenamiento-BBDD/README.md) | S5.3 — Cosmos DB (partición, RU, consistencia, soft delete) | [Cosmos.Demo.Api](M05-Almacenamiento-BBDD/S5.3-cosmosdb/README.md) | ✅ Disponible |
| [M05 — Almacenamiento y BBDD](M05-Almacenamiento-BBDD/README.md) | S5.4 — Managed Identity (keyless, RBAC mínimo, Key Vault refs) | [ManagedIdentity.Demo.Api](M05-Almacenamiento-BBDD/S5.4-managed-identity/README.md) | ✅ Disponible |
| [M05 — Almacenamiento y BBDD](M05-Almacenamiento-BBDD/README.md) | S5.5 — Backups, replicación y DR (RPO/RTO, soft delete) | [Dr.Demo.Api](M05-Almacenamiento-BBDD/S5.5-backups-dr/README.md) | ✅ Disponible |
| [M05 — Almacenamiento y BBDD](M05-Almacenamiento-BBDD/README.md) | S5.P — Práctica: Cosmos DB con Managed Identity | [Cosmos.Mi.Demo.Api](M05-Almacenamiento-BBDD/S5.P-practica-cosmos-managed-identity/README.md) | ✅ Disponible |
| [M05 — Almacenamiento y BBDD](M05-Almacenamiento-BBDD/README.md) | S5.P2 — Práctica: Table Storage CRUD | [Tables.Demo.Api](M05-Almacenamiento-BBDD/S5.P2-practica-table-storage-crud/README.md) | ✅ Disponible |
| [M06 — Seguridad y Auth](M06-Seguridad-Auth/README.md) | S6.1 — Responsabilidad compartida, defense in depth, STRIDE | [Security.Demo.Api](M06-Seguridad-Auth/S6.1-responsabilidad-compartida/README.md) | ✅ Disponible |
| [M06 — Seguridad y Auth](M06-Seguridad-Auth/README.md) | S6.2 — Microsoft Entra ID (identidades, roles, JWT, App Roles) | [Entra.Demo.Api](M06-Seguridad-Auth/S6.2-entra-id/README.md) | ✅ Disponible |
| [M06 — Seguridad y Auth](M06-Seguridad-Auth/README.md) | S6.3 — OAuth2 / OpenID Connect (flujos, PKCE, authorize URL) | [Oauth.Demo.Api](M06-Seguridad-Auth/S6.3-oauth2-oidc/README.md) | ✅ Disponible |
| [M06 — Seguridad y Auth](M06-Seguridad-Auth/README.md) | S6.4 — Auth desktop / MSIX (WAM, redirect URIs, ciclo de token) | [Desktop.Demo.Api](M06-Seguridad-Auth/S6.4-auth-desktop-msix/README.md) | ✅ Disponible |
| [M06 — Seguridad y Auth](M06-Seguridad-Auth/README.md) | S6.5 — Seguridad de datos (cifrado at-rest/in-transit, CMK, CORS) | [Datos.Demo.Api](M06-Seguridad-Auth/S6.5-seguridad-datos/README.md) | ✅ Disponible |
| [M06 — Seguridad y Auth](M06-Seguridad-Auth/README.md) | S6.6 — Key Vault (secretos/keys/certs, RBAC, references, rotación) | [KeyVault.Demo.Api](M06-Seguridad-Auth/S6.6-key-vault/README.md) | ✅ Disponible |
| [M06 — Seguridad y Auth](M06-Seguridad-Auth/README.md) | S6.P — Práctica: OAuth2 + Key Vault | [Practica.Demo.Api](M06-Seguridad-Auth/S6.P-practica-oauth2-keyvault/README.md) | ✅ Disponible |
| [M06 — Seguridad y Auth](M06-Seguridad-Auth/README.md) | S6.P2 — Práctica: Easy Auth (auth sin código) | [EasyAuth.Demo.Api](M06-Seguridad-Auth/S6.P2-practica-easy-auth/README.md) | ✅ Disponible |
| [M07 — Integración y MSIX](M07-Integracion-MSIX/README.md) | S7.1 — Service Bus / Event Grid avanzado (filtros SQL, dedup, DLQ) | [Messaging.Demo.Api](M07-Integracion-MSIX/S7.1-service-bus-event-grid-avanzado/README.md) | ✅ Disponible |
| [M07 — Integración y MSIX](M07-Integracion-MSIX/README.md) | S7.2 — Diseño event-driven (patrones, Saga, Event Sourcing) | [EventDriven.Demo.Api](M07-Integracion-MSIX/S7.2-diseno-event-driven/README.md) | ✅ Disponible |
| [M07 — Integración y MSIX](M07-Integracion-MSIX/README.md) | S7.3 — Azure API Management (policies, versionado, tier) | [Apim.Demo.Api](M07-Integracion-MSIX/S7.3-api-management/README.md) | ✅ Disponible |
| [M07 — Integración y MSIX](M07-Integracion-MSIX/README.md) | S7.4 — ClickOnce vs MSIX (comparativa, migración, firma) | [Distribution.Demo.Api](M07-Integracion-MSIX/S7.4-clickonce-vs-msix/README.md) | ✅ Disponible |
| [M07 — Integración y MSIX](M07-Integracion-MSIX/README.md) | S7.5 — MSIX empaquetado y distribución (manifest, naming, canales) | [Msix.Demo.Api](M07-Integracion-MSIX/S7.5-msix-empaquetado-distribucion/README.md) | ✅ Disponible |

✅ **Módulo M02 completo** (5 submódulos + 2 prácticas, 7/7).
✅ **Módulo M03 completo** (6 submódulos + 2 prácticas, 8/8).
✅ **Módulo M04 completo** (5 submódulos + 2 prácticas, 7/7).
✅ **Módulo M05 completo** (5 submódulos + 2 prácticas, 7/7).
✅ **Módulo M06 completo** (6 submódulos + 2 prácticas, 8/8).
⏳ **Módulo M07 en construcción** (5/9 — S7.1–S7.5 disponibles).

## Cómo usar un ejemplo

1. Abrir la carpeta del ejemplo en VS Code o Visual Studio.
2. Leer su `README.md` — explica el objetivo, los conceptos cubiertos y el mapeo
   a las slides del submódulo.
3. `dotnet build` y `dotnet test` desde la carpeta del ejemplo.
4. `dotnet run` desde el proyecto que corresponda para probar local.
5. Seguir la sección "Despliegue por Portal" del README para subirlo a Azure.

## Requisitos comunes

- .NET SDK 10 (`dotnet --list-sdks` debe mostrar `10.x`).
- Una suscripción de Azure (cualquier plan, incluido el gratuito) para los
  apartados de despliegue.
- Visual Studio Code con la extensión **Azure App Service** o Visual Studio 2022+.
