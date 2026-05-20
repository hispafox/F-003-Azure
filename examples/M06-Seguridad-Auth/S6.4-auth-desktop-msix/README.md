# S6.4 — Autenticación en desktop y MSIX

> **Submódulo de referencia:** [M06-S6.4](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.4-auth-desktop-msix-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API (advisory) · **Coste:** 0 € (scripts solo lectura)

> ℹ️ Submódulo **conceptual** (como S6.1–S6.3): modela la *decisión* de
> auth de una app WPF/WinForms/MSIX (método, redirect URI, ciclo de
> token) como lógica pura + grafo DI. La app desktop real (MSAL +
> broker WAM) **no se ejecuta aquí** — no es emulable.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: las llaves del piso compartido, WAM frente a system browser frente a embedded, el redirect URI del broker plugin para MSIX y la máquina de estados del ciclo de token (incluyendo el reto de Conditional Access).

## Objetivo

| Concepto | Dónde |
| --- | --- |
| WAM vs system browser vs embedded vs device code | [`DesktopFlowAdvisor.cs`](src/Desktop.Demo.Api/Desktop/DesktopFlowAdvisor.cs) |
| Redirect URIs desktop/MSIX (localhost / broker / oob) | [`RedirectUriAdvisor.cs`](src/Desktop.Demo.Api/Desktop/RedirectUriAdvisor.cs) |
| Ciclo de token: silent / refresh / interactive / claims | [`TokenLifecycle.cs`](src/Desktop.Demo.Api/Desktop/TokenLifecycle.cs) |
| Plan de auth desktop completo | [`IDesktopAuthPlanner.cs`](src/Desktop.Demo.Api/Desktop/IDesktopAuthPlanner.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Flujos OAuth2 para desktop | 3 | [`DesktopFlowAdvisor.cs`](src/Desktop.Demo.Api/Desktop/DesktopFlowAdvisor.cs) |
| Public vs Confidential client | 4 | `DesktopFlowAdvisor.EsClientePublico` |
| System browser | 5 | `DesktopFlowAdvisor` (SystemBrowser) |
| WAM (broker Windows, SSO nativo) | 6 | `DesktopFlowAdvisor` (Wam) |
| App Registration desktop / redirect URIs | 7 | [`RedirectUriAdvisor.cs`](src/Desktop.Demo.Api/Desktop/RedirectUriAdvisor.cs) + `scripts/01` |
| Token cache persistente | 8 | `TokenLifecycle` (estados de cache) |
| Refresh silencioso | 10 | [`TokenLifecycle.cs`](src/Desktop.Demo.Api/Desktop/TokenLifecycle.cs) |
| MSIX: redirect URI broker plugin | 11 | `RedirectUriAdvisor.Para(Msix, ...)` |
| Conditional Access (claims challenge) | 12 | `TokenLifecycle` (`InteractiveConClaims`) |

## Estructura

```
S6.4-auth-desktop-msix/
├── src/Desktop.Demo.Api/
│   ├── Desktop/    DesktopFlowAdvisor, RedirectUriAdvisor, TokenLifecycle
│   │               (lógica pura) + IDesktopAuthPlanner/DesktopAuthPlanner
│   ├── Endpoints/  DesktopEndpoints
│   └── Program.cs  AddSingleton<IDesktopAuthPlanner>
├── tests/Desktop.Demo.Api.Tests/
│   ├── Unit_*            las 3 piezas (ciclo de token = máquina estados)
│   └── DiContainer_Tests resuelve IDesktopAuthPlanner del contenedor real
└── scripts/        01-desktop-app-config (App Registrations — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 26 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `DesktopFlowAdvisor` (WAM en Windows joined,
  embedded solo "aceptable", cliente público), `RedirectUriAdvisor`
  (localhost / broker plugin / oob legacy), `TokenLifecycle` (los 4
  estados de la slide 10 + el reto de Conditional Access slide 12 que
  manda sobre todo).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve `IDesktopAuthPlanner`
  (mismo singleton) y planifica (Windows joined + cache → WAM + silent).
  Cubre la [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).

> 🧠 **Sin CAPA de integración (a propósito)**: el login interactivo de
> escritorio (MSAL `PublicClientApplication` + broker WAM + browser del
> sistema) no es emulable en un test verde. La *decisión* (método,
> redirect URI, siguiente acción de token) sí es pura y se testea al
> 100%; el login real se valida a mano en una app WPF con MSAL.

## Ejecución local

```bash
dotnet run --project src/Desktop.Demo.Api
# http://localhost:5091  — usa src/Desktop.Demo.Api/api.http
```

Todo offline. Endpoints: `/desktop/flujo`, `/desktop/redirect-uri`,
`/desktop/token-accion` (POST), `/desktop/plan` (POST).

## Inspeccionar las apps desktop reales (Portal / scripts)

- **Portal** — *Entra ID → App registrations → tu app →
  Authentication*: marca *Allow public client flows*, añade redirect
  URIs `http://localhost` y `ms-appx-web://microsoft.aad.brokerplugin/<client-id>`
  (WAM/MSIX, slide 7/11).
- **Scripts** [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**:
  `01-desktop-app-config.sh` lista apps de cliente público, comprueba
  el broker URI y avisa si usan `oob` (legacy). Requiere *Directory
  Readers*.

## Ideas centrales

> Desktop = **cliente público** (sin client secret → PKCE, slide 4).
> **WAM es la mejor opción en Windows** (SSO nativo, como Office/Teams,
> slide 6); system browser multiplataforma; embedded solo "aceptable".
> El token se reutiliza de cache (silent) y solo se vuelve interactivo
> en primera vez, refresh caducado (~90 d) o reto de Conditional
> Access (slide 10/12). MSIX usa el redirect URI del broker plugin.

## Próximo paso

[`S6.5 — Seguridad de datos`](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.5-seguridad-datos-v3.md).
