# S6.5 — Seguridad de datos: cifrado en tránsito y en reposo

> **Submódulo de referencia:** [M06-S6.5](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.5-seguridad-datos-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API (advisory) · **Coste:** 0 € (scripts solo lectura)

> ℹ️ Submódulo **conceptual** (como S6.1–S6.4): la decisión de cifrado
> y la auditoría de configuración como lógica pura + grafo DI. Sin CAPA
> de integración (cifrado/TLS/CMK/CORS es configuración, no emulable).

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Cifrado at-rest: MMK vs CMK vs Always Encrypted | [`EncryptionAdvisor.cs`](src/Datos.Demo.Api/Datos/EncryptionAdvisor.cs) |
| Cifrado en tránsito: TLS mínimo + connection strings | [`TlsTransitValidator.cs`](src/Datos.Demo.Api/Datos/TlsTransitValidator.cs) |
| CORS seguro (la combinación prohibida) | [`CorsPolicyValidator.cs`](src/Datos.Demo.Api/Datos/CorsPolicyValidator.cs) |
| Checklist de seguridad de datos | [`IDataProtectionAssessor.cs`](src/Datos.Demo.Api/Datos/IDataProtectionAssessor.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Cifrado en tránsito: TLS | 3, 5 | [`TlsTransitValidator.cs`](src/Datos.Demo.Api/Datos/TlsTransitValidator.cs) |
| Cifrado en reposo por defecto (AES-256) | 6 | `EncryptionAdvisor` (`AtRestSiempreActivo`) |
| Customer-Managed Keys (CMK) | 7 | `EncryptionAdvisor` (CmkAtRest) |
| TDE en SQL | 8 | `scripts/01-data-security-check.sh` |
| Always Encrypted (ultra-sensible) | 9 | `EncryptionAdvisor` (AlwaysEncrypted) |
| CORS | 13 | [`CorsPolicyValidator.cs`](src/Datos.Demo.Api/Datos/CorsPolicyValidator.cs) |
| Checklist de seguridad de datos | 14 | [`IDataProtectionAssessor.cs`](src/Datos.Demo.Api/Datos/IDataProtectionAssessor.cs) |

## Estructura

```
S6.5-seguridad-datos/
├── src/Datos.Demo.Api/
│   ├── Datos/      EncryptionAdvisor, TlsTransitValidator,
│   │               CorsPolicyValidator (lógica pura)
│   │               + IDataProtectionAssessor/DataProtectionAssessor
│   ├── Endpoints/  DatosEndpoints
│   └── Program.cs  AddSingleton<IDataProtectionAssessor>
├── tests/Datos.Demo.Api.Tests/
│   ├── Unit_*            las 3 piezas + el assessor
│   └── DiContainer_Tests resuelve IDataProtectionAssessor (contenedor real)
└── scripts/        01-data-security-check (TLS/HTTPS/TDE — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 30 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `EncryptionAdvisor` (MMK/CMK/Always Encrypted por
  sensibilidad+regulación), `TlsTransitValidator` (TLS ≥1.2, Encrypt=true
  en SQL, https en Storage), `CorsPolicyValidator` (detecta el
  `*`+credentials prohibido, slide 13), `DataProtectionAssessor`
  (checklist 0-100, slide 14).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve
  `IDataProtectionAssessor` (mismo singleton) y evalúa. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).

> 🧠 **Sin CAPA de integración (a propósito)**: el cifrado at-rest, TLS,
> TDE, CMK y CORS son **configuración del recurso**, no algo emulable en
> un test verde. La *decisión* y la *auditoría* sí son puras y se testean
> al 100%; la postura real se inspecciona con los scripts `az` de solo
> lectura (mismo criterio que S6.1–S6.4).

## Ejecución local

```bash
dotnet run --project src/Datos.Demo.Api
# http://localhost:5092  — usa src/Datos.Demo.Api/api.http
```

Endpoints: `/datos/cifrado`, `/datos/tls/{version}`, `/datos/cors`
(POST), `/datos/checklist` (POST).

## Auditar la postura real (Portal / scripts)

- **Portal** — fuerza *HTTPS Only* + *Minimum Inbound TLS 1.2* en App
  Service; *Secure transfer required* + *Minimum TLS 1.2* en Storage;
  *TDE* on en Azure SQL (por defecto). CMK solo si la regulación lo
  exige (slide 7).
- **Scripts** [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**:
  `01-data-security-check.sh` revisa min-TLS, HTTPS-only y TDE.
  Requiere *Reader*.

## Ideas centrales

> Azure cifra **at-rest (AES-256) e in-transit (TLS 1.2)** por defecto.
> **CMK** solo si la regulación exige controlar las claves; **Always
> Encrypted** para datos ultra-sensibles (ni Azure los lee, pero sin
> WHERE/ORDER BY). **TLS 1.0/1.1 deprecados.** Y la regla de oro de
> CORS: **nunca `AllowAnyOrigin()` con `AllowCredentials()`** (slide 13).

## Próximo paso

[`S6.6 — Key Vault`](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.6-key-vault-v3.md).
