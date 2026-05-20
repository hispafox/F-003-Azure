# S6.6 — Azure Key Vault

> **Submódulo de referencia:** [M06-S6.6](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.6-key-vault-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API (advisory) · **Coste:** 0 € (scripts solo lectura)

> ℹ️ Submódulo **conceptual** (como S6.1–S6.5): qué va a Key Vault, con
> qué rol, cómo se referencia y cuándo rotar — lógica pura + grafo DI.
> El SDK `SecretClient` se documenta pero **no se invoca** (KV no es
> emulable de forma fiable → sin CAPA de integración).

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: la caja fuerte del banco y las llaves bajo el felpudo, la regla "MI primero, KV después", la sintaxis de Key Vault Reference y la política de rotación con Event Grid `SecretNearExpiry`.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| ¿MI o Key Vault? + tipo (Secret/Key/Cert) + rol mínimo | [`KeyVaultItemAdvisor.cs`](src/KeyVault.Demo.Api/KeyVault/KeyVaultItemAdvisor.cs) |
| Key Vault References (`@Microsoft.KeyVault(...)`) | [`KeyVaultReference.cs`](src/KeyVault.Demo.Api/KeyVault/KeyVaultReference.cs) |
| Política de rotación (SecretNearExpiry) | [`SecretRotationPolicy.cs`](src/KeyVault.Demo.Api/KeyVault/SecretRotationPolicy.cs) |
| Plan de almacenamiento completo | [`IKeyVaultPlanner.cs`](src/KeyVault.Demo.Api/KeyVault/IKeyVaultPlanner.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| La regla: si no puede ser MI → Key Vault | 2 | [`KeyVaultItemAdvisor.cs`](src/KeyVault.Demo.Api/KeyVault/KeyVaultItemAdvisor.cs) |
| Qué almacena (Secrets/Keys/Certificates) | 3 | `KeyVaultItemAdvisor` (Destino) |
| Crear KV + almacenar secretos | 4 | `scripts/01-kv-inventory.sh` |
| RBAC vs Access Policies + roles mínimos | 5 | `KeyVaultItemAdvisor.RolMinimo` |
| Key Vault References en App Settings | 6 | [`KeyVaultReference.cs`](src/KeyVault.Demo.Api/KeyVault/KeyVaultReference.cs) |
| Versionado de secretos | 8 | [`SecretRotationPolicy.cs`](src/KeyVault.Demo.Api/KeyVault/SecretRotationPolicy.cs) |
| Rotación automática (SecretNearExpiry) | 9 | `SecretRotationPolicy` (ventana 30 días) |
| Certificados / Keys en KV | 10-11 | `KeyVaultItemAdvisor` (Certificate / Key) |

## Estructura

```
S6.6-key-vault/
├── src/KeyVault.Demo.Api/
│   ├── KeyVault/   KeyVaultItemAdvisor, KeyVaultReference,
│   │               SecretRotationPolicy (lógica pura)
│   │               + IKeyVaultPlanner/KeyVaultPlanner
│   ├── Endpoints/  KeyVaultEndpoints
│   └── Program.cs  AddSingleton<IKeyVaultPlanner>
├── tests/KeyVault.Demo.Api.Tests/
│   ├── Unit_*            las 3 piezas (referencia con GeneratedRegex)
│   └── DiContainer_Tests resuelve IKeyVaultPlanner (contenedor real)
└── scripts/        01-kv-inventory (RBAC/secretos/caducidad — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 27 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `KeyVaultItemAdvisor` (MI vs KV + rol mínimo;
  ninguno recomendado es Administrator), `KeyVaultReference`
  (construir/parsear `@Microsoft.KeyVault(...)`, case-insensitive,
  round-trip), `SecretRotationPolicy` (vigente / próximo / expirado con
  reloj inyectable, ventana configurable).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve `IKeyVaultPlanner`
  (mismo singleton) — plan API-key→Secret+reference y Azure-a-Azure→MI.
  Cubre la [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).

> 🧠 **Sin CAPA de integración (a propósito)**: Key Vault no se emula
> de forma fiable en un test verde. La *decisión* (dónde, qué rol, qué
> referencia, cuándo rotar) sí es pura y se testea al 100%; el acceso
> real se hace con `SecretClient` + `DefaultAzureCredential` (slide 7)
> y se valida con los scripts `az` de solo lectura.

## Ejecución local

```bash
dotnet run --project src/KeyVault.Demo.Api
# http://localhost:5093  — usa src/KeyVault.Demo.Api/api.http
```

Endpoints: `/kv/donde`, `/kv/referencia` (GET build / POST parse),
`/kv/rotacion` (POST), `/kv/plan` (POST).

## Inventario real (Portal / scripts)

- **Portal** — *Key Vault → Access configuration*: usa **RBAC** (no
  Access Policies, slide 5); *purge protection* ON en producción.
  *Secrets*: pon expiración y configura Event Grid `SecretNearExpiry`
  (slide 9).
- **Scripts** [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**,
  **nunca leen valores**: `01-kv-inventory.sh` muestra modo RBAC, purge
  protection y los **nombres** de secretos con su caducidad. Requiere
  *Key Vault Reader* / *Secrets User*.

## Ideas centrales

> **Si es un secreto y no puede ser Managed Identity, va a Key Vault.**
> RBAC sobre Access Policies; rol **mínimo** por tipo (Secrets User /
> Crypto User / Certificates Officer), nunca Administrator. App Settings
> como **Key Vault Reference** (slide 6) — el código lee la variable sin
> saber que viene de KV. Rotación: Event Grid avisa 30 días antes.

## Próximo paso

[`S6.P — Práctica: OAuth2 + Key Vault`](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.P-practica-oauth2-keyvault-v3.md):
integra S6.3 (OAuth2) + S6.6 (Key Vault).
