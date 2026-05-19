# S6.P2 — Práctica: Easy Auth con Microsoft Entra

> **Submódulo de referencia:** [M06-S6.P2](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.P2-practica-easy-auth-v1.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (Web App F1 + Easy Auth + Entra gratis)

> 🎓 **Práctica que cierra M06** — autenticación **sin escribir código**:
> App Service Authentication (Easy Auth). Modela la *decisión* de
> configuración + simula el contrato del middleware (302/401 + cabeceras).

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Acción no-autenticado por tipo de app (302 web / 401 API) | [`EasyAuthConfigAdvisor.cs`](src/EasyAuth.Demo.Api/EasyAuth/EasyAuthConfigAdvisor.cs) |
| Endpoints integrados `/.auth/*` (login, me, logout) | [`AuthEndpoints.cs`](src/EasyAuth.Demo.Api/EasyAuth/AuthEndpoints.cs) |
| Cabeceras `X-MS-CLIENT-PRINCIPAL-*` | [`EasyAuthHeaders.cs`](src/EasyAuth.Demo.Api/EasyAuth/EasyAuthHeaders.cs) |
| Plan + checklist del entregable | [`IEasyAuthSetupPlanner.cs`](src/EasyAuth.Demo.Api/EasyAuth/IEasyAuthSetupPlanner.cs) |
| App protegida (/ 302/200, /.auth/me, /health) | [`EasyAuthEndpoints.cs`](src/EasyAuth.Demo.Api/Endpoints/EasyAuthEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Qué construyes / recorrido | 2 | `IEasyAuthSetupPlanner.Checklist` |
| Proveedores soportados (Entra, Google, …) | 3 | `EasyAuthConfigAdvisor.Proveedores` |
| Headers que añade Easy Auth | 4 | [`EasyAuthHeaders.cs`](src/EasyAuth.Demo.Api/EasyAuth/EasyAuthHeaders.cs) |
| Habilitar Easy Auth (acción no-auth, token store) | 5 | [`EasyAuthConfigAdvisor.cs`](src/EasyAuth.Demo.Api/EasyAuth/EasyAuthConfigAdvisor.cs) |
| Flujo de auth (302 → login) | 6 | `/` endpoint + `Api_EasyAuthTests` |
| `/.auth/me`, `/.auth/logout`, `/.auth/login` | 4-6 | [`AuthEndpoints.cs`](src/EasyAuth.Demo.Api/EasyAuth/AuthEndpoints.cs) |

## Estructura

```
S6.P2-practica-easy-auth/
├── src/EasyAuth.Demo.Api/
│   ├── EasyAuth/   EasyAuthConfigAdvisor, AuthEndpoints,
│   │               EasyAuthHeaders (lógica pura)
│   │               + IEasyAuthSetupPlanner/EasyAuthSetupPlanner
│   ├── Endpoints/  EasyAuthEndpoints (/, /.auth/me, /easyauth/*)
│   └── Program.cs  AddSingleton<IEasyAuthSetupPlanner>
├── tests/EasyAuth.Demo.Api.Tests/
│   ├── Unit_*            lógica pura
│   ├── DiContainer_Tests resuelve IEasyAuthSetupPlanner (contenedor real)
│   └── Api_EasyAuthTests E2E: /health 200, / 302 vs 200, /.auth/me []
└── scripts/        01-verify-easyauth (entregable — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 16 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `EasyAuthConfigAdvisor` (web→302 / API→401, 6
  proveedores), `AuthEndpoints` (login/logout/callback, ruta `/.auth/*`),
  `EasyAuthHeaders` (principal desde cabeceras).
- **CAPA 0 · DI**: resuelve `IEasyAuthSetupPlanner` del contenedor real
  (`Assert.Same` singleton).
- **CAPA E2E**: app completa vía `WebApplicationFactory` **simulando las
  cabeceras `X-MS-CLIENT-PRINCIPAL-*`** (con `AllowAutoRedirect=false`
  para afirmar el 302): `/health`→200, `/` sin sesión→302 al login, con
  cabeceras→200, `/.auth/me` sin sesión→`[]`. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).

> 🧠 **No es integración con Entra (no emulable)**: en Azure, Easy Auth
> es middleware que vive **antes** de tu código en App Service; aquí se
> replica su contrato (302/401 + cabeceras) para testearlo. El login
> real se prueba a mano (slide 6: navegador en incógnito).

## Ejecución local

```bash
dotnet run --project src/EasyAuth.Demo.Api
# http://localhost:5095  — usa src/EasyAuth.Demo.Api/api.http
```

`/health` público; `/` sin las cabeceras de Easy Auth → 302 al login
(comportamiento de sitio web, slide 5/6); con ellas → 200 + identidad.

## Despliegue por Portal (entregable)

1. **Web App F1** (o reutiliza la de M02-S2.P2).
2. *Authentication → Add identity provider* → **Microsoft** →
   *Create new app registration* (Easy Auth crea la App Registration y
   gestiona el secret — **sin tocar código**, slide 5).
3. *Restrict access* = **Require authentication**;
   *Unauthenticated requests* = **HTTP 302** (sitio web) o **401** (API).
4. **Verificar** (slide 6/11): sin sesión → 302/401; tras login →
   tu identidad; `/.auth/me` muestra claims; cero código C# de auth.

> Scripts `az` en [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**:
> `01-verify-easyauth.sh` comprueba que Easy Auth está habilitado, la
> acción no-auth y que `/.auth/me` responde.

## Ideas centrales

> Easy Auth = auth **sin código**: middleware integrado en App Service,
> *antes* de tu app. Crea la App Registration y gestiona el secret por
> ti. Sitio web → 302 al login; API → 401. Inyecta
> `X-MS-CLIENT-PRINCIPAL-*` y expone `/.auth/me|login|logout`. Cubre el
> ~95% de apps internas corporativas.

## Módulo M06 completo ✅

S6.1 responsabilidad compartida · S6.2 Entra ID · S6.3 OAuth2/OIDC ·
S6.4 auth desktop/MSIX · S6.5 seguridad de datos · S6.6 Key Vault ·
S6.P práctica OAuth2+KV · **S6.P2 práctica Easy Auth** (este).

**Siguiente módulo:** M07 — Integración y MSIX.
