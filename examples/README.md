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

✅ **Módulo M02 completo** (5 submódulos + 2 prácticas, 7/7). Módulo M03 en curso.

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
