# S6.3 — OAuth2 y OpenID Connect

> **Submódulo de referencia:** [M06-S6.3](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.3-oauth2-openid-connect-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API (advisory) · **Coste:** 0 € (scripts solo lectura)

> ℹ️ Submódulo **conceptual** (como S6.1/S6.2): el valor es la lógica de
> los flujos OAuth2/OIDC como funciones puras + el grafo DI real. Sin
> CAPA de integración (no se emula un IdP de forma fiable).

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el portero y la entrega del paquete, cliente público vs confidencial, los seis flujos OAuth2 con su cuándo, PKCE con el vector RFC 7636 y por qué no implementar OAuth a mano.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Qué flujo OAuth2 usar por tipo de cliente | [`OAuthFlowAdvisor.cs`](src/Oauth.Demo.Api/Oauth/OAuthFlowAdvisor.cs) |
| PKCE (code_verifier / code_challenge S256) | [`PkceGenerator.cs`](src/Oauth.Demo.Api/Oauth/PkceGenerator.cs) |
| Construir la URL de `/authorize` | [`AuthorizeUrlBuilder.cs`](src/Oauth.Demo.Api/Oauth/AuthorizeUrlBuilder.cs) |
| Plan de login completo (flujo + URL + PKCE) | [`ILoginPlanner.cs`](src/Oauth.Demo.Api/Oauth/ILoginPlanner.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| OAuth2 vs OIDC; roles | 3-4 | README + `AuthorizeUrlBuilder` (scope `openid`) |
| Flujos: cuándo usar cada uno | 5 | [`OAuthFlowAdvisor.cs`](src/Oauth.Demo.Api/Oauth/OAuthFlowAdvisor.cs) |
| Authorization Code + PKCE | 6 | [`PkceGenerator.cs`](src/Oauth.Demo.Api/Oauth/PkceGenerator.cs) + `AuthorizeUrlBuilder` |
| Client Credentials / Device Code / OBO | 9-11 | `OAuthFlowAdvisor` + `LoginPlanner` (notas) |
| Tokens JWT: anatomía | 12 | (decodificación → ver S6.2 `JwtInspector`) |
| Implicit / ROPC deprecados | 5 | `OAuthFlowAdvisor.EstaDeprecado` |
| Config App Registration (redirect, audience) | 7-8, 17 | [`scripts/01-oauth-config.sh`](scripts/01-oauth-config.sh) |

## Estructura

```
S6.3-oauth2-oidc/
├── src/Oauth.Demo.Api/
│   ├── Oauth/      OAuthFlowAdvisor, PkceGenerator, AuthorizeUrlBuilder
│   │               (lógica pura) + ILoginPlanner/LoginPlanner
│   ├── Endpoints/  OauthEndpoints
│   └── Program.cs  AddSingleton<ILoginPlanner>
├── tests/Oauth.Demo.Api.Tests/
│   ├── Unit_*            las 3 piezas (PKCE con vector RFC 7636)
│   └── DiContainer_Tests resuelve ILoginPlanner del contenedor real
└── scripts/        01-oauth-config (App Registrations — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 27 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `OAuthFlowAdvisor` (tabla slide 5 + deprecados),
  `PkceGenerator` (**verificado con el vector del RFC 7636 §B**),
  `AuthorizeUrlBuilder` (params PKCE, fuerza `openid`, URL-encoding).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve `ILoginPlanner`
  (mismo singleton) y planifica un login SPA (con authorize URL+PKCE) y
  uno daemon (Client Credentials, sin URL). Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).

> 🧠 **Sin CAPA de integración (a propósito)**: el flujo completo exige
> un IdP real (Entra ID) con usuario interactivo — no es emulable de
> forma fiable en un test verde. La lógica (qué flujo, PKCE correcto,
> URL bien formada) sí es pura y se testea al 100%. El round-trip real
> se valida a mano con `Microsoft.Identity.Web` contra Entra ID.

## Ejecución local

```bash
dotnet run --project src/Oauth.Demo.Api
# http://localhost:5090  — usa src/Oauth.Demo.Api/api.http
```

Todo offline. Endpoints: `/oauth/flujo`, `/oauth/deprecado/{flujo}`,
`/oauth/pkce`, `/oauth/plan` (POST).

## Inspeccionar la config OAuth real (Portal / scripts)

- **Portal** — *Entra ID → App registrations → tu app →
  Authentication* (redirect URIs, flujos) y *API permissions* (scopes).
- **Scripts** [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**:
  `01-oauth-config.sh` lista redirect URIs, audiencia, permisos y avisa
  si alguna app tiene **implicit** habilitado (deprecado, slide 5).
  Requiere *Directory Readers*.

## Ideas centrales

> **Auth Code + PKCE** para apps con usuario (SPA/móvil/web), **Client
> Credentials** para servicios sin usuario (slide 5). **Implicit y ROPC
> están deprecados** — nunca en apps nuevas. PKCE evita el robo del
> `code`. Tu app **no implementa OAuth a mano**: usa
> `Microsoft.Identity.Web` (slide 7/13); este ejemplo modela la
> *decisión* y los *parámetros*, no reemplaza la librería.

## Próximo paso

[`S6.4 — Autenticación desktop / MSIX`](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.4-auth-desktop-msix-v3.md).
