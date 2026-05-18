# S5.5 — Backups, replicación y recuperación ante desastres

> **Submódulo de referencia:** [M05-S5.5](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.5-backups-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API (advisory) · **Coste:** ≈ 0 € (scripts: Storage soft-delete)

> ℹ️ Submódulo **conceptual** (como S5.4): no añade un servicio de datos
> nuevo. El valor es la **lógica de decisión** de backup/DR/retención y
> el walkthrough de recuperación con soft delete.

## Objetivo

Codificar las tres decisiones de DR y poder generar un plan:

| Concepto | Dónde |
| --- | --- |
| Backup "de fábrica" por servicio (qué configurar tú) | [`BackupPolicyAdvisor.cs`](src/Dr.Demo.Api/Dr/BackupPolicyAdvisor.cs) |
| RPO/RTO y estrategia por criticidad + coste | [`RpoRtoCalculator.cs`](src/Dr.Demo.Api/Dr/RpoRtoCalculator.cs) |
| Retención por regulación (WORM, derecho al olvido) | [`RetentionPolicyAdvisor.cs`](src/Dr.Demo.Api/Dr/RetentionPolicyAdvisor.cs) |
| Plan de DR completo (compone los tres) | [`IDrPlanner.cs`](src/Dr.Demo.Api/Dr/IDrPlanner.cs) |
| Walkthrough real soft delete (blob + container) | [`scripts/02-smoke-test.sh`](scripts/02-smoke-test.sh) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Backup por servicio (auto / retención / PITR) | 3 | [`BackupPolicyAdvisor.cs`](src/Dr.Demo.Api/Dr/BackupPolicyAdvisor.cs) |
| Cosmos / Azure SQL: PITR | 4-5 | `BackupPolicyAdvisor` (CosmosDb, AzureSql) |
| Blob: soft delete + versioning + immutability | 6, 19 | `01-provision.sh` + `02-smoke-test.sh` |
| Plan de DR: RPO / RTO | 8, 14, 22 | [`RpoRtoCalculator.cs`](src/Dr.Demo.Api/Dr/RpoRtoCalculator.cs) |
| Estrategia 3-2-1 | 11 | `01-provision.sh` (nota GRS/3-2-1) + README |
| Runbook de recuperación | 12 | `02-smoke-test.sh` (borrar→undelete→restore) |
| Checklist de DR | 16 | `IDrPlanner` (avisos: lo que requiere config manual) |
| Soft-delete recovery walkthrough | 19 | [`scripts/02-smoke-test.sh`](scripts/02-smoke-test.sh) |
| Compliance retention (SEC, SOX, RGPD…) | 20 | [`RetentionPolicyAdvisor.cs`](src/Dr.Demo.Api/Dr/RetentionPolicyAdvisor.cs) |
| Estrategias DR + coste (active/warm/cold) | 24 | `RpoRtoCalculator.Perfil` (`MultiplicadorCoste`) |

## Estructura

```
S5.5-backups-dr/
├── src/Dr.Demo.Api/
│   ├── Dr/         BackupPolicyAdvisor, RpoRtoCalculator,
│   │               RetentionPolicyAdvisor  (lógica pura)
│   │               + IDrPlanner/DrPlanner  (servicio que los compone)
│   ├── Endpoints/  DrEndpoints (backup / rpo-rto / retencion / plan)
│   └── Program.cs  AddSingleton<IDrPlanner>
├── tests/Dr.Demo.Api.Tests/
│   ├── Unit_*            lógica pura (backup, rpo/rto, retención)
│   └── DiContainer_Tests resuelve IDrPlanner + plan coherente
└── scripts/        01-provision (soft delete+versioning) /
                    02-smoke (walkthrough recuperación) / 03-cleanup
```

## Tests

```bash
dotnet test     # 31 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `BackupPolicyAdvisor` (auto/PITR/qué configurar tú),
  `RpoRtoCalculator` (estrategia por criticidad, ¿cumple SLA?),
  `RetentionPolicyAdvisor` (años, WORM, derecho al olvido, días de
  inmutabilidad).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve `IDrPlanner` (mismo
  singleton) y verifica que genera un plan coherente, incluido el aviso
  de la slide 22 cuando la estrategia no cumple el SLA. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).

> 🧠 **No hay CAPA de integración (a propósito)**: backups, PITR y
> failover **no son emulables** — requieren Azure real. Igual que S5.4
> (Managed Identity): la parte testable se aísla en lógica pura (CAPA 1)
> + el grafo DI (CAPA 0); el round-trip de soft delete se prueba a mano
> con `scripts/02-smoke-test.sh` contra un Storage real.

## Ejecución local

```bash
dotnet run --project src/Dr.Demo.Api
# http://localhost:5085  — usa src/Dr.Demo.Api/api.http
```

Todo offline (lógica de decisión). `POST /dr/plan` con criticidad +
servicios + objetivos RPO/RTO devuelve la estrategia recomendada y los
avisos (servicios sin backup automático, SLA no cumplido).

## Despliegue por Portal (protección de datos)

1. **Cosmos DB** — *Backup & Restore* → **Continuous (7/30 días)**;
   restore = *Point In Time Restore* a una cuenta nueva (slide 4).
2. **Azure SQL** — *Backups* → PITR activo por defecto; configura
   *Long-term retention* (semanal/mensual/anual) si compliance (slide 5).
3. **Blob Storage** — *Data protection* →
   **Enable soft delete for blobs** (30 d) + **for containers** (30 d) +
   **versioning**; *GRS/GZRS* para offsite (slides 6, 11, 19). Para
   compliance: *immutability policy* WORM en el container (slide 20).
4. **App Service** — *Backups* (requiere Standard+) o, mejor, DR =
   redeploy desde pipeline + IaC (stateless, slide 8/14).
5. **Probar el DR** — restore trimestral a un recurso temporal y
   validación de datos (slides 9, 15, 23). Documentar RPO/RTO **medido**,
   no asumido (slide 22).

> Scripts `az` en [`scripts/`](scripts) (`./demo.sh`): `01-provision.sh`
> crea Storage con soft delete + versioning; `02-smoke-test.sh` ejecuta
> el walkthrough de recuperación de la slide 19 (subir → borrar →
> undelete blob; borrar → restore container); `03-cleanup.sh` borra el
> RG. Complemento de clase, no sustituto del Portal.

## La regla de oro (slide 27)

> "Un backup que nunca se ha restaurado no es un backup — es una
> esperanza." Continuous/PITR + soft delete + apps stateless que se
> redesplegan, y **probarlo** (game day trimestral).

## Próximo paso

[`S5.P — Práctica del módulo`](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.P-practica-v3.md):
Cosmos DB con Managed Identity (integra S5.3 + S5.4).
