# S6.1 — Responsabilidad compartida y defense in depth

> **Submódulo de referencia:** [M06-S6.1](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.1-responsabilidad-compartida-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API (advisory) · **Coste:** 0 € (scripts solo lectura)

> ℹ️ Arranca **M06** (Seguridad/Auth). Submódulo **conceptual** (como
> M05-S5.4/S5.5): el valor es la lógica de decisión de seguridad como
> funciones puras + el grafo DI real. Sin CAPA de integración (nada
> emulable).

## Objetivo

Codificar los principios del modelo de seguridad de la nube:

| Concepto | Dónde |
| --- | --- |
| Modelo de responsabilidad compartida (la línea que nunca cambia) | [`ResponsibilityMatrix.cs`](src/Security.Demo.Api/Security/ResponsibilityMatrix.cs) |
| STRIDE: amenaza + mitigaciones por categoría | [`StrideAnalyzer.cs`](src/Security.Demo.Api/Security/StrideAnalyzer.cs) |
| Detección de secretos en config/repos (estilo gitleaks) | [`SecretScanner.cs`](src/Security.Demo.Api/Security/SecretScanner.cs) |
| Secure Score a partir del checklist del equipo | [`ISecureScore.cs`](src/Security.Demo.Api/Security/ISecureScore.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Modelo de responsabilidad compartida | 3 | [`ResponsibilityMatrix.cs`](src/Security.Demo.Api/Security/ResponsibilityMatrix.cs) |
| Errores de configuración más comunes | 4 | `SecretScanner` + `scripts/01-posture-check.sh` |
| Defense in depth (4 capas) | 5 | README + endpoint `/seguridad/responsabilidad` |
| Capa 2: red (firewall, NSG) | 7 | [`scripts/01-posture-check.sh`](scripts/01-posture-check.sh) |
| Secure Score / Defender for Cloud | 10 | `ISecureScore` + [`scripts/02-secure-score.sh`](scripts/02-secure-score.sh) |
| Auditoría: quién hizo qué | 12 | `scripts/02-secure-score.sh` (Activity Log) |
| Checklist de seguridad del equipo | 17 | [`ISecureScore.cs`](src/Security.Demo.Api/Security/ISecureScore.cs) |
| Threat modeling STRIDE | 20 | [`StrideAnalyzer.cs`](src/Security.Demo.Api/Security/StrideAnalyzer.cs) |
| Secrets scanning en git | 22 | [`SecretScanner.cs`](src/Security.Demo.Api/Security/SecretScanner.cs) |

## Estructura

```
S6.1-responsabilidad-compartida/
├── src/Security.Demo.Api/
│   ├── Security/   ResponsibilityMatrix, StrideAnalyzer, SecretScanner
│   │               (lógica pura) + ISecureScore/SecureScoreCalculator
│   ├── Endpoints/  SeguridadEndpoints
│   └── Program.cs  AddSingleton<ISecureScore>
├── tests/Security.Demo.Api.Tests/
│   ├── Unit_*            las 4 piezas de lógica
│   └── DiContainer_Tests resuelve ISecureScore del contenedor real
└── scripts/        01-posture-check / 02-secure-score (SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 46 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `ResponsibilityMatrix` (tabla slide 3 + "siempre
  tuya"), `StrideAnalyzer` (6 categorías, iniciales = STRIDE),
  `SecretScanner` (reglas tipo gitleaks; Key Vault ref no es secreto),
  `SecureScoreCalculator` (0-100 + faltantes + veredicto).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve `ISecureScore` (mismo
  singleton) y calcula un score real. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).

> 🧠 **Sin CAPA de integración (a propósito)**: responsabilidad
> compartida, STRIDE y Secure Score son **conceptos**, no servicios
> emulables. La parte testable se aísla en lógica pura (CAPA 1) + el
> grafo DI (CAPA 0); la postura real se inspecciona con los scripts
> `az` de solo lectura (mismo criterio que M05-S5.4/S5.5).

## Ejecución local

```bash
dotnet run --project src/Security.Demo.Api
# http://localhost:5088  — usa src/Security.Demo.Api/api.http
```

Todo offline. Endpoints: `/seguridad/responsabilidad`, `/seguridad/stride/{cat}`,
`/seguridad/scan` (POST), `/seguridad/secure-score` (POST checklist).

## Auditar la postura real (Portal / scripts)

- **Portal** — *Microsoft Defender for Cloud → Secure Score* y
  *Recommendations* (slide 10). Revísalo **cada mes** y cierra las
  recomendaciones (objetivo > 70%, slide 17).
- **Scripts** [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**, no
  crean nada (sin cleanup): `01-posture-check.sh` (storage público / SQL
  firewall abierto / HTTPS, slide 4/7); `02-secure-score.sh` (Secure
  Score + recomendaciones + Activity Log, slide 10/12). Requieren rol
  *Security Reader*.

## La idea central (slide 2-3)

> El 80% de las brechas en la nube son **errores de configuración del
> cliente**, no fallos de Azure. Tus **datos, identidades y
> dispositivos** son SIEMPRE tu responsabilidad — en cualquier modelo,
> sin excepción. Defense in depth: identidad + red + aplicación + datos.

## Próximo paso

[`S6.2 — Microsoft Entra ID`](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.2-entra-id-v3.md):
usuarios, grupos, roles y App Registrations.
