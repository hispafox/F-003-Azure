# S9.3 — Claude Code para infraestructura Azure

> **Submódulo de referencia:** [M09-S9.3](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.3-cc-infraestructura-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; no invoca Claude Code real)

> 🎓 **Submódulo conceptual** (lección 9 del HANDOFF). Claude Code
> genera Bicep/YAML/Dockerfile a partir de descripciones en lenguaje
> natural — aquí extraemos las heurísticas pedagógicas: parser de
> requisitos, generador de los 7 prompts canónicos de IaC y audit
> checker contra reglas mínimas (HTTPS, MI, tags, TLS).

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el aparejador que te dibuja los planos del chalet como analogía, parser de requisitos con avisos automáticos (HTTPS/MI/Private Endpoint), 7 prompts canónicos (Bicep, Dockerfile, GH Actions, reverse ARM, audit, runbook, ops scripts) y el audit checker como gate determinístico en el pipeline IaC.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Parser de requisitos IaC (slides 2/3/17) | [`InfraRequirementsParser.cs`](src/ClaudeCode.Infra.Demo.Api/Infra/InfraRequirementsParser.cs) |
| Generador de prompts canónicos (slides 2-17) | [`InfraPromptBuilder.cs`](src/ClaudeCode.Infra.Demo.Api/Infra/InfraPromptBuilder.cs) |
| Audit checker contra reglas mínimas (slide 15) | [`InfraAuditChecker.cs`](src/ClaudeCode.Infra.Demo.Api/Infra/InfraAuditChecker.cs) |
| Plan + checklist del flujo "requirements → IaC" | [`IInfraPlanner.cs`](src/ClaudeCode.Infra.Demo.Api/Infra/IInfraPlanner.cs) |
| API que expone la lógica (`/infra/*`) | [`InfraEndpoints.cs`](src/ClaudeCode.Infra.Demo.Api/Endpoints/InfraEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| IaC con IA como caso de uso | 2 | `InfraRequirementsParser` |
| Módulos Bicep organizados | 3 | `InfraPromptBuilder.BicepDesdeRequirements` |
| Pipeline YAML completo | 4/17 | `InfraPromptBuilder.GhActionsPipeline` |
| Dockerfile multi-stage optimizado | 5 | `InfraPromptBuilder.DockerfileMultiStage` |
| Configurar Service Bus, App Service, etc. | 6/9 | (parte del prompt Bicep) |
| Migración de datos | 7 | (no se modela — varía por escenario) |
| Validar IaC generada (`what-if`) | 8 | `Checklist` (paso 3) |
| Troubleshooting de infra con IA | 10 | `Checklist` + S9.2 ya cubre el patrón |
| Runbooks de operaciones | 11 | `InfraPromptBuilder.RunbookOperaciones` |
| GH Actions equivalente | 12 | `InfraPromptBuilder.GhActionsPipeline` |
| Scripts de operaciones | 14 | `InfraPromptBuilder.ScriptOps` |
| Auditar infraestructura existente | 15 | `InfraAuditChecker.Auditar` |
| Reverse engineering ARM → Bicep | 16 | `InfraPromptBuilder.ReverseArmABicep` |
| Pipeline IaC completo desde requirements | 17 | `IInfraPlanner.Planificar` |

## Estructura

```
S9.3-cc-infraestructura/
├── src/ClaudeCode.Infra.Demo.Api/
│   ├── Infra/      InfraRequirementsParser, InfraPromptBuilder,
│   │              InfraAuditChecker
│   │              + IInfraPlanner/InfraPlanner
│   ├── Endpoints/  InfraEndpoints (/health, /infra/*)
│   └── Program.cs  AddSingleton<IInfraPlanner> + enums por nombre
└── tests/ClaudeCode.Infra.Demo.Api.Tests/
    ├── Unit_*                lógica pura (parser, prompts, audit)
    ├── DiContainer_Tests     resuelve IInfraPlanner
    └── Api_InfraTests        E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 36 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `InfraRequirementsParser` (detecta App Service + Cosmos + Service
    Bus + Key Vault sin duplicar; detecta multi-region cuando aparecen
    `West Europe` y `North Europe`; GDPR → `ComplianceEuropa=true`;
    slots + autoscale; **avisos** cuando falta HTTPS only, MI o
    Private Endpoint en Storage; aviso adicional si multi-region +
    GDPR para confirmar región UE).
  - `InfraPromptBuilder` (cada uno de los 7 escenarios devuelve el
    texto característico esperado; el Bicep con requirements refleja
    los recursos detectados; sin requirements pone placeholder
    genérico; GH Actions menciona OIDC + auto-rollback; reverse ARM
    pide what-if de verificación; audit cubre HTTPS + TLS + tags + MI).
  - `InfraAuditChecker` (recurso conforme no genera hallazgos; web app
    sin HTTPS → `Critico`; storage público → `Critico`; SQL sin
    firewall → `Alto`; web app sin MI → `Alto`; sin tags → `Medio`;
    TLS 1.0 → `Alto`; informe cuenta `Criticos` y `Altos`
    correctamente).
- **CAPA 0 · DI**: resuelve `IInfraPlanner` del contenedor real
  (`Assert.Same` singleton) y compone requisitos + dos prompts + audit
  + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/infra/{requisitos, prompt/{escenario}, audit, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: Claude Code genera el
> Bicep — pero los Bicep generados se validan en S8.5 (que SÍ tiene
> SkippableFact con `bicep build`). Aquí lo testeable son los
> **inputs** a Claude (prompts, requisitos, audit) — eso es lógica
> pura.

## Ejecución local

```bash
dotnet run --project src/ClaudeCode.Infra.Demo.Api
# http://localhost:5115  — usa src/ClaudeCode.Infra.Demo.Api/api.http
```

- `/infra/requisitos` mastica la descripción y devuelve recursos +
  flags no-funcionales + avisos.
- `/infra/prompt/{escenario}` devuelve uno de los 7 prompts canónicos
  (`BicepDesdeRequirements`, `DockerfileMultiStage`, `GhActionsPipeline`,
  `ReverseArmABicep`, `AuditarRecursos`, `RunbookOperaciones`,
  `ScriptOps`).
- `/infra/audit` evalúa recursos contra HTTPS + MI + tags + TLS +
  público + firewall y devuelve hallazgos con severidad + comando fix.
- `/infra/plan` compone todo + checklist de 9 puntos.

## Entregable

El entregable es el flujo completo "requirements → IaC desplegable":

1. **Describe** los requisitos en lenguaje natural (recursos +
   compliance + escala) → `/infra/requisitos`.
2. **Genera** el Bicep con el prompt canónico → cópialo a Claude Code.
3. **Genera** el pipeline IaC (`GhActionsPipeline`) → cópialo a Claude.
4. **Valida** con `az bicep build` + `az deployment group what-if`
   (esto lo cubre S8.5 con `bicep build` real).
5. Si hay infra existente: **`az group export` + `az bicep decompile`**
   y pásalo por el prompt `ReverseArmABicep` para limpiarlo.
6. **Audita** los recursos finales (`/infra/audit`) y arregla los
   hallazgos críticos antes de mergear.

## Ideas centrales

> **Claude Code es bueno generando Bicep estructurado** cuando le das
> los 4 ingredientes (slide 2-3): recursos concretos, requisitos no
> funcionales, convenciones del equipo (tags, naming, AVM modules) y
> criterio de éxito (`what-if` sin Delete inesperados). **Reverse
> engineering** (slide 16) pasa de 8-12 h manuales a 30 min — el
> `az bicep decompile` da el Bicep crudo y Claude lo modulariza.
> **Audit** (slide 15) son reglas binarias (HTTPS, MI, TLS, tags,
> público, firewall) — perfecta candidata a hook `PreToolUse` o stage
> de pipeline antes del deploy.

## Próximo paso

[`S9.4 — MCP y herramientas externas`](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.4-mcp-herramientas-v3.md):
conectar Claude Code con sistemas externos (GitHub, Notion, bases de
datos) vía Model Context Protocol.
