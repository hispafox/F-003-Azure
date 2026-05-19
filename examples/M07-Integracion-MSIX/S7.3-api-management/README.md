# S7.3 — Azure API Management: el gateway de vuestras APIs

> **Submódulo de referencia:** [M07-S7.3](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.3-api-management-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** APIM **Consumption 0 €** (1M llamadas/mes gratis) — Standard/Premium = €€ si los eliges

> 🎓 **Submódulo conceptual.** APIM no tiene emulador útil. El valor
> docente es **la lógica del gateway** (policies, versionado, elección
> de tier) — pura y testeable. La instancia de APIM se despliega por
> Portal; aquí se materializa el *cómo razona el gateway* en cada
> petición.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Policies inbound: subscription key, ip-filter, validate-jwt, rate-limit/quota, circuit breaker (slides 5-6/9/18) | [`ApimPolicyEvaluator.cs`](src/Apim.Demo.Api/Apim/ApimPolicyEvaluator.cs) |
| Versionado de APIs: Segment / Query / Header (slide 7) | [`ApimVersioningResolver.cs`](src/Apim.Demo.Api/Apim/ApimVersioningResolver.cs) |
| Elección de tier + ¿buen caso para APIM? (slides 3/16/32) | [`ApimTierAdvisor.cs`](src/Apim.Demo.Api/Apim/ApimTierAdvisor.cs) |
| Plan + checklist del entregable | [`IApimPlanner.cs`](src/Apim.Demo.Api/Apim/IApimPlanner.cs) |
| API que expone el gateway (/apim/*) | [`ApimEndpoints.cs`](src/Apim.Demo.Api/Endpoints/ApimEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Crear APIM + tiers (Consumption/Developer/Basic/Standard/Premium) | 3 | `ApimTierAdvisor.RecomendarTier` |
| Importar API + path + backend | 4 | scripts `01-verify-apim` |
| Policies inbound (rate-limit, validate-jwt, cors, set-header) | 5-6 | `ApimPolicyEvaluator.Evaluar` |
| Versionado set: Segment / Query / Header | 7 | `ApimVersioningResolver.Resolver` |
| Subscription keys + products | 8 | `PolicyConfig.SubscriptionRequired` |
| Rate limiting + quota (con choose premium) | 9 | `ApimPolicyEvaluator.Evaluar` |
| ¿Cuándo APIM y cuándo no? | 16 | `ApimTierAdvisor.EsBuenCaso` |
| Circuit breaker con retry | 18 | `ApimPolicyEvaluator.DebeReintentar` |
| Monitorización + alertas | 13 | `IApimPlanner.Checklist` |
| Anti-patterns | 31 | `IApimPlanner.Checklist` |
| Decision tree de tier | 32 | `ApimTierAdvisor.RecomendarTier` |

## Estructura

```
S7.3-api-management/
├── src/Apim.Demo.Api/
│   ├── Apim/       ApimPolicyEvaluator, ApimVersioningResolver,
│   │               ApimTierAdvisor (lógica pura)
│   │               + IApimPlanner/ApimPlanner
│   ├── Endpoints/  ApimEndpoints (/health, /apim/*)
│   └── Program.cs  AddSingleton<IApimPlanner> + enums por nombre
├── tests/Apim.Demo.Api.Tests/
│   ├── Unit_*            lógica pura (policy, versioning, tier)
│   ├── DiContainer_Tests resuelve IApimPlanner (contenedor real)
│   └── Api_ApimTests     E2E vía WebApplicationFactory
└── scripts/        01-verify-apim (entregable — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 31 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `ApimPolicyEvaluator` (OK / 401 sin key / 403 IP /
  401 jwt aud / 429 rate con Retry-After / premium aguanta más / 429
  quota / circuit breaker); `ApimVersioningResolver` (Segment / Query /
  Header, versión inválida, recomendado=Segment); `ApimTierAdvisor`
  (VNet/multi-región/RPS→Premium, dev/test→Developer, prod media→
  Standard, bajo volumen→Consumption, ¿buen caso?).
- **CAPA 0 · DI**: resuelve `IApimPlanner` del contenedor real
  (`Assert.Same` singleton) y compone tier + caso + policies + checklist.
  Cubre la [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/apim/{policy,retry,version,version/recomendado,tier,caso,plan}`.

> 🧠 **Sin CAPA de integración a propósito.** APIM no tiene emulador
> útil (la self-hosted gateway es Premium-only, slide 29). El valor
> docente está en cómo decide el gateway, que es lógica pura. Mismo
> criterio que M06 / S7.1 / S7.2.

## Ejecución local

```bash
dotnet run --project src/Apim.Demo.Api
# http://localhost:5098  — usa src/Apim.Demo.Api/api.http
```

`/apim/policy` aplica el orden inbound (subscription key → ip-filter →
jwt → rate-limit → quota); `/apim/version` resuelve la versión según el
esquema; `/apim/tier` recomienda tier+coste; `/apim/plan` compone el
plan + checklist.

## Despliegue por Portal (entregable)

> ⚠️ **Coste:** **Consumption** = 0 € base (1M llamadas/mes gratis) y
> es lo recomendado para el curso. **Developer ~40 €/mes** (sin SLA —
> nunca en producción, slide 31.1). **Standard ~550 €/mes**,
> **Premium ~2200 €/mes**. Borra el recurso desde el Portal si elegiste
> un tier de pago.

1. **APIM Consumption** en el RG del curso (slide 3).
2. **Importar API** desde un App Service o Function (OpenAPI URL o
   manual) con `path` y `service-url` (slide 4).
3. **Version set** con esquema **Segment** (slide 7).
4. **Product + subscription** con `subscription-required = true`
   (slide 8); la key se envía en `Ocp-Apim-Subscription-Key`.
5. **Policies inbound** (slide 5-6/9): `validate-jwt` (Entra ID),
   `rate-limit-by-key`, `quota-by-key`, `cors`, `cache-lookup`/`-store`.
6. **Alertas** (slide 13): 4xx > 5%, 5xx > 0 sostenido, BackendDuration > 2s.
7. **Verificar** (scripts `az`): tier, APIs publicadas, products,
   suscripciones, métricas.

> Scripts `az` en [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**:
> `01-verify-apim.sh` inventaría la instancia. No crea recursos → sin
> cleanup (borra tú los tiers de pago desde el Portal).

## Ideas centrales

> APIM **profesionaliza** las APIs: una URL de entrada, **policies**
> (rate-limit + JWT + caching + retry + transformaciones), **versionado
> centralizado** (Segment), **subscription key + OAuth2** ambos
> (la app + el usuario, slide 8) y **developer portal** automático.
> Empieza con **Consumption** (gratis hasta 1M); sube a Standard o
> Premium cuando lo justifique el SLA, VNet o multi-región. Configura
> todo como **código (Bicep + GitOps)**, nunca a mano en el Portal
> (slide 31.10).

## Próximo paso

[`S7.4 — ClickOnce vs MSIX`](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.4-clickonce-vs-msix-v3.md):
distribución de aplicaciones de escritorio Windows.
