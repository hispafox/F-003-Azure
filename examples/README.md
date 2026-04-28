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
- **Despliegue Azure:** los pasos del README siempre se documentan por **Portal**.
  Algunos ejemplos incluyen además scripts `az` opcionales en `scripts/` para
  escenificar la demo en clase — siempre como complemento, nunca como sustituto
  de la guía del Portal.
- **No lanzo apps:** los `dotnet run` los hace el alumno; la verificación automática
  se queda en `dotnet build` + `dotnet test`.

## Índice

| Módulo | Submódulo | Ejemplo | Estado |
| --- | --- | --- | --- |
| [M02 — App Services](M02-App-Services/README.md) | S2.1 — Creación, configuración y publicación | [AppService.Demo.Api](M02-App-Services/S2.1-creacion-config-publicacion/README.md) | ✅ Disponible |
| [M02 — App Services](M02-App-Services/README.md) | S2.2 — Slots staging / producción | [AppService.Demo.Slots](M02-App-Services/S2.2-slots-staging-produccion/README.md) | ✅ Disponible |

Los demás submódulos se irán añadiendo siguiendo el mismo patrón.

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
