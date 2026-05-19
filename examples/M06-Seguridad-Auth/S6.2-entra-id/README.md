# S6.2 — Microsoft Entra ID

> **Submódulo de referencia:** [M06-S6.2](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.2-entra-id-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API (advisory) · **Coste:** 0 € (scripts solo lectura)

> ℹ️ Submódulo **conceptual** (como S6.1): el valor es la lógica de
> decisión de identidad como funciones puras + el grafo DI real. Sin
> CAPA de integración (Entra ID no es emulable).

## Objetivo

| Concepto | Dónde |
| --- | --- |
| MI vs Service Principal vs App Registration | [`IdentityTypeAdvisor.cs`](src/Entra.Demo.Api/Entra/IdentityTypeAdvisor.cs) |
| RBAC de Azure vs roles de Entra ID | [`RoleClassifier.cs`](src/Entra.Demo.Api/Entra/RoleClassifier.cs) |
| Decodificar claims de un JWT (sin validar firma) | [`JwtInspector.cs`](src/Entra.Demo.Api/Entra/JwtInspector.cs) |
| App Roles: autorización por el claim `roles` | [`IAppRolesAuthorizer.cs`](src/Entra.Demo.Api/Entra/IAppRolesAuthorizer.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Tenant / usuarios / grupos | 3-5 | [`scripts/01-directory-inventory.sh`](scripts/01-directory-inventory.sh) |
| RBAC de Azure vs roles de Entra ID | 6-7 | [`RoleClassifier.cs`](src/Entra.Demo.Api/Entra/RoleClassifier.cs) |
| App Registrations / Service Principals | 8-9 | [`scripts/02-app-registrations.sh`](scripts/02-app-registrations.sh) |
| MI vs SP vs App Registration (+ prioridad) | 10 | [`IdentityTypeAdvisor.cs`](src/Entra.Demo.Api/Entra/IdentityTypeAdvisor.cs) |
| Tokens: ID/Access, claims | 18 | [`JwtInspector.cs`](src/Entra.Demo.Api/Entra/JwtInspector.cs) |
| App Roles ([Authorize(Roles=...)]) | 19 | [`IAppRolesAuthorizer.cs`](src/Entra.Demo.Api/Entra/IAppRolesAuthorizer.cs) |
| Gestión de usuarios por CLI | 16 | `scripts/01-directory-inventory.sh` |
| B2B / invitados | 22 | `scripts/01-directory-inventory.sh` (filtro Guest) |
| Workload identities / lifecycle SP | 36 | `scripts/02-app-registrations.sh` |

## Estructura

```
S6.2-entra-id/
├── src/Entra.Demo.Api/
│   ├── Entra/      IdentityTypeAdvisor, RoleClassifier, JwtInspector
│   │               (lógica pura) + IAppRolesAuthorizer/AppRolesAuthorizer
│   ├── Endpoints/  EntraEndpoints
│   └── Program.cs  AddSingleton<IAppRolesAuthorizer>
├── tests/Entra.Demo.Api.Tests/
│   ├── Unit_*            las 4 piezas (+ helper Jwt para tokens de test)
│   └── DiContainer_Tests resuelve IAppRolesAuthorizer del contenedor real
└── scripts/        01-directory-inventory / 02-app-registrations (SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 29 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `IdentityTypeAdvisor` (slide 10 + prioridad),
  `RoleClassifier` (Azure RBAC vs Entra, case-insensitive),
  `JwtInspector` (extrae claims slide 18, detecta expirado con reloj
  inyectado, roles como array o string), `AppRolesAuthorizer`
  (autoriza/deniega case-insensitive).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve `IAppRolesAuthorizer`
  (mismo singleton) y autoriza. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).

> 🧠 **`JwtInspector` SOLO decodifica, NO valida la firma** (slide 18:
> "tu app NUNCA valida tokens manualmente" — eso lo hace
> `Microsoft.Identity.Web`). Aquí es para *inspección/didáctica*: ver
> qué claims trae el token. Nunca uses esto como mecanismo de auth.

> 🧠 **Sin CAPA de integración (a propósito)**: Entra ID no se emula. La
> parte testable se aísla en lógica pura (CAPA 1) + el grafo DI
> (CAPA 0); el directorio real se inspecciona con los scripts `az` de
> solo lectura (mismo criterio que S6.1 / M05-S5.4/S5.5).

## Ejecución local

```bash
dotnet run --project src/Entra.Demo.Api
# http://localhost:5089  — usa src/Entra.Demo.Api/api.http
```

Todo offline. Endpoints: `/entra/identidad`, `/entra/rol`,
`/entra/token` (POST, decodifica JWT), `/entra/autorizar` (POST).

## Auditar el directorio real (Portal / scripts)

- **Portal** — *Entra ID → Users / Groups / App registrations /
  Sign-in logs* (slide 21). Revisa invitados y secretos caducados.
- **Scripts** [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**, no
  crean nada (sin cleanup): `01-directory-inventory.sh` (tenant/usuarios/
  grupos/guests), `02-app-registrations.sh` (apps, secretos, SPs).
  Requieren rol *Directory Readers*.

## Ideas centrales

> Entra ID es el centro de seguridad de toda la organización (slide 37).
> **RBAC de Azure ≠ roles de Entra ID** (slide 6). Prioridad de
> identidad: **Managed Identity > Service Principal con certificado >
> SP con secret** (slide 10). Asigna permisos a **grupos**, no a
> usuarios (slide 5). Tu app **no valida tokens a mano** (slide 18).

## Próximo paso

[`S6.3 — OAuth2 / OpenID Connect`](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.3-oauth2-openid-connect-v3.md):
los flujos de autenticación en detalle.
