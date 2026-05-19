# S6.P — Práctica: OAuth2 con Entra ID + Key Vault

> **Submódulo de referencia:** [M06-S6.P](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.P-practica-oauth2-keyvault-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** < 0,10 € (Key Vault + Easy Auth gratis)

> 🎓 **Práctica del módulo** — integra **S6.3** (OAuth2 / Easy Auth) +
> **S6.6** (Key Vault References). Entregable: una API protegida con
> Entra ID cuyos secretos viven en Key Vault (cero passwords en config).

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Easy Auth: acción 401 (API) vs login (web) + issuer | [`EasyAuthAdvisor.cs`](src/Practica.Demo.Api/Practica/EasyAuthAdvisor.cs) |
| App Settings con Key Vault References (S6.6) | [`KeyVaultRefAppSettings.cs`](src/Practica.Demo.Api/Practica/KeyVaultRefAppSettings.cs) |
| Principal de Easy Auth (cabeceras X-MS-CLIENT-PRINCIPAL-*) | [`EasyAuthPrincipal.cs`](src/Practica.Demo.Api/Practica/EasyAuthPrincipal.cs) |
| Plan + checklist del entregable | [`IPracticaPlanner.cs`](src/Practica.Demo.Api/Practica/IPracticaPlanner.cs) |
| API protegida (/health público, /api/perfil 401/200) | [`PracticaEndpoints.cs`](src/Practica.Demo.Api/Endpoints/PracticaEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| App Registration + client secret a Key Vault | 4-5 | `scripts/01-verify-practica.sh` |
| MI de la Web App con acceso al KV | 6 | `scripts/01-verify-practica.sh` |
| Key Vault References en App Settings | 7 | [`KeyVaultRefAppSettings.cs`](src/Practica.Demo.Api/Practica/KeyVaultRefAppSettings.cs) |
| Easy Auth (`--action Return401`) | 8 | [`EasyAuthAdvisor.cs`](src/Practica.Demo.Api/Practica/EasyAuthAdvisor.cs) |
| API protegida + cabeceras X-MS-CLIENT-PRINCIPAL | 9 | [`EasyAuthPrincipal.cs`](src/Practica.Demo.Api/Practica/EasyAuthPrincipal.cs) + endpoints |
| Probar sin/con token (401/200) | 10 | tests `Api_EasyAuthTests` |
| Verificaciones del entregable | 11 | `IPracticaPlanner.Checklist` + `scripts/01` |

## Estructura

```
S6.P-practica-oauth2-keyvault/
├── src/Practica.Demo.Api/
│   ├── Practica/   EasyAuthAdvisor, KeyVaultRefAppSettings,
│   │               EasyAuthPrincipal (lógica pura)
│   │               + IPracticaPlanner/PracticaPlanner
│   ├── Endpoints/  PracticaEndpoints (/health, /api/perfil, /practica/*)
│   └── Program.cs  AddSingleton<IPracticaPlanner>
├── tests/Practica.Demo.Api.Tests/
│   ├── Unit_*            lógica pura
│   ├── DiContainer_Tests resuelve IPracticaPlanner (contenedor real)
│   └── Api_EasyAuthTests E2E: /health 200, /api/perfil 401 vs 200
└── scripts/        01-verify-practica (entregable — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 12 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `EasyAuthAdvisor` (Return401/Login + issuer v2.0),
  `KeyVaultRefAppSettings` (secretos = referencias; detecta secreto en
  claro), `EasyAuthPrincipal` (parsea las cabeceras Easy Auth).
- **CAPA 0 · DI**: resuelve `IPracticaPlanner` del contenedor real.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` **simulando
  las cabeceras `X-MS-CLIENT-PRINCIPAL-*`** que Easy Auth inyecta en
  Azure: `/health`→200, `/api/perfil` sin cabeceras→401, con cabeceras
  →200. Cubre la [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).

> 🧠 **No es integración con Entra (no emulable)**: en Azure, Easy Auth
> va *delante* de la app y valida el token; en local replicamos su
> contrato leyendo las cabeceras `X-MS-CLIENT-PRINCIPAL-*`. El login
> OAuth real se prueba a mano (slide 10: `az account get-access-token`).

## Ejecución local

```bash
dotnet run --project src/Practica.Demo.Api
# http://localhost:5094  — usa src/Practica.Demo.Api/api.http
```

`/health` es público; `/api/perfil` exige las cabeceras de Easy Auth
(401 sin ellas). El `api.http` incluye una petición que las simula.

## Despliegue por Portal (entregable)

1. **App Registration** (Entra ID) + client secret → guardar en KV.
2. **Key Vault** RBAC; secretos `AzureAd-ClientSecret`, `ExternalApiKey`.
3. **MI** de la Web App con rol *Key Vault Secrets User* en el KV.
4. **App Settings** como `@Microsoft.KeyVault(...)` (slide 7).
5. **Easy Auth**: *Authentication → Add Microsoft* →
   *Unauthenticated requests* = **Return 401** (API) (slide 8).
6. **Verificar** (slide 11): `/health` 200; `/api/perfil` sin token
   401, con token 200; App Settings = Key Vault References en verde;
   cero passwords en claro.

> Scripts `az` en [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**:
> `01-verify-practica.sh` comprueba Easy Auth on, App Settings solo KV
> references y la MI con rol en el KV.

## Próximo paso

[`S6.P2 — Práctica: Easy Auth`](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.P2-practica-easy-auth-v1.md):
cierra el módulo M06.
