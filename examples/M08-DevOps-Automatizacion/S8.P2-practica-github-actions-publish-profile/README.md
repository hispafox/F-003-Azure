# S8.P2 — Práctica GitHub Actions + publish profile

> **Submódulo de referencia:** [M08-S8.P2](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.P2-practica-github-actions-publish-profile-v1.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; los scripts solo leen el publish profile + runs)

> 🎓 **Práctica conceptual** (lección 9 del HANDOFF). El workflow real
> corre en GitHub Actions — aquí extraemos las piezas testeables que
> sustentan la práctica: parser del XML publish profile, generador del
> workflow YAML y recomendador Publish Profile vs OIDC.
>
> 🧱 **Cierra M08 (8/8)**: última práctica del módulo de DevOps.

> 📘 **¿Primera vez con esta práctica?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: las dos llaves de la casa como analogía (Publish Profile vs OIDC), parser del XML con detección de placeholders, tres niveles de workflow (minimal, con tests, con environment+smoke), recomendador con tres salidas y rotación obligatoria del publish profile cada 90 días.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Parser del XML publish profile (slide 7/17) | [`PublishProfileParser.cs`](src/Practica.GhActions.Demo.Api/GhActions/PublishProfileParser.cs) |
| Generador del workflow GitHub Actions (slides 9, 14, 15, 18) | [`WorkflowBuilder.cs`](src/Practica.GhActions.Demo.Api/GhActions/WorkflowBuilder.cs) |
| Recomendador Publish Profile vs OIDC vs Environment (slide 13/18) | [`MetodoAuthRecomendador.cs`](src/Practica.GhActions.Demo.Api/GhActions/MetodoAuthRecomendador.cs) |
| Plan + checklist de la práctica (slide 2/16) | [`IPracticaGhActionsPlanner.cs`](src/Practica.GhActions.Demo.Api/GhActions/IPracticaGhActionsPlanner.cs) |
| API que expone la lógica (`/ghactions/*`) | [`GhActionsEndpoints.cs`](src/Practica.GhActions.Demo.Api/Endpoints/GhActionsEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Qué construye la práctica (8 pasos) | 2 | `IPracticaGhActionsPlanner.Checklist` |
| Pre-flight: SDK, az, gh, plan F1 | 3 | `Checklist` |
| Crear Web App F1 (Linux, .NET 8) | 4 | `Checklist` (paso 1) |
| Código mínimo del API | 5 | (el alumno lo escribe en clase) |
| Subir el repo a GitHub | 6 | `Checklist` (paso 2) |
| Descargar el publish profile | 7 | `PublishProfileParser.Parsear` |
| Configurar el secret en GitHub | 8 | `Checklist` (paso 4 + paso 5) |
| Crear el workflow (`actions/checkout`, `setup-dotnet`, `webapps-deploy`) | 9 | `WorkflowBuilder` (job minimal) |
| Push y ver el deploy en directo | 10 | `Checklist` (paso 8) |
| Hacer un cambio y verificar el flujo | 11 | (se ejercita en clase) |
| Smoke tests automatizados | 12 | `OpcionesWorkflow.SmokeAlFinal` añade el paso |
| Publish Profile vs OIDC | 13 | `MetodoAuthRecomendador.Recomendar` |
| Variante con tests (2 jobs `needs:`) | 14 | `OpcionesWorkflow.IncluirTests` |
| Variante deploy solo en tags | 15 | `OpcionesWorkflow.SoloEnTags` |
| Cleanup | 16 | `Checklist` (último ítem) |
| Errores comunes (XML inválido, password rotada) | 17 | `PublishProfileParser` los reporta |
| Producción: rotar credentials, Environment | 18 | `OpcionesWorkflow.EnvironmentProduccion` |

## Estructura

```
S8.P2-practica-github-actions-publish-profile/
├── src/Practica.GhActions.Demo.Api/
│   ├── GhActions/  PublishProfileParser, WorkflowBuilder,
│   │              MetodoAuthRecomendador
│   │              + IPracticaGhActionsPlanner/PracticaGhActionsPlanner
│   ├── Endpoints/  GhActionsEndpoints (/health, /ghactions/*)
│   └── Program.cs  AddSingleton<IPracticaGhActionsPlanner> + enums por nombre
├── tests/Practica.GhActions.Demo.Api.Tests/
│   ├── Unit_*                lógica pura (parser, workflow, auth)
│   ├── DiContainer_Tests     resuelve IPracticaGhActionsPlanner
│   └── Api_GhActionsTests    E2E vía WebApplicationFactory
└── scripts/        publish-profile descarga + listar runs (SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 32 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `PublishProfileParser` (parsea MSDeploy + FTP, extrae
    `UserName`/`PublishUrl`/`DestinationAppUrl`, detecta password
    placeholder `changeme`/`xxxxxxxx`/`...` y password vacía como
    `PasswordPresente=false`; avisa si falta el perfil MSDeploy; XML
    inválido devuelve `EsValido=false` con la causa en `Advertencias`;
    sin nodo raíz `<publishData>` es inválido).
  - `WorkflowBuilder` (1 job por defecto `build-and-deploy`; `IncluirTests`
    crea 2 jobs con `needs: build-test`; `SoloEnTags` sustituye el
    trigger `push.branches` por `push.tags: ['v*']`; el step de deploy
    referencia `azure/webapps-deploy@v3` + `secrets.AZURE_WEBAPP_PUBLISH_PROFILE`
    + el `app-name`; `SmokeAlFinal` añade el paso; `EnvironmentProduccion`
    pone `environment: production`; respeta la versión .NET solicitada).
  - `MetodoAuthRecomendador` (side project / no controla Entra → Publish
    Profile; producción/auditoría/multi-env con Entra → OIDC; producción
    sin Entra → Publish Profile + Environment con reviewers; razones y
    riesgos siempre no vacíos; OIDC menciona "Federated Credentials").
- **CAPA 0 · DI**: resuelve `IPracticaGhActionsPlanner` del contenedor
  real (`Assert.Same` singleton) y compone profile + workflow +
  recomendación + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/ghactions/{profile/parsear, workflow, auth/recomendar, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: el workflow real corre en
> GHA; ejecutarlo desde un test consume minutos del free tier y
> requiere autenticación contra GitHub. Lo testeable son las piezas
> decisorias — esas se cubren con CAPA 1 + E2E. El alumno valida el
> pipeline manualmente con `gh run watch` y los scripts.

## Ejecución local

```bash
dotnet run --project src/Practica.GhActions.Demo.Api
# http://localhost:5112  — usa src/Practica.GhActions.Demo.Api/api.http
```

- `/ghactions/profile/parsear` mastica el XML y avisa de password
  vacía / placeholder / falta perfil MSDeploy.
- `/ghactions/workflow` devuelve el árbol de jobs/steps con knobs
  (`IncluirTests`, `SoloEnTags`, `SmokeAlFinal`, `EnvironmentProduccion`).
- `/ghactions/auth/recomendar` decide Publish Profile vs OIDC vs
  Environment Secret.
- `/ghactions/plan` compone todo + checklist de 12 puntos.

## Verificar contra Azure + GitHub (scripts)

```bash
./scripts/demo.sh
# 1) 01-publish-profile.sh → descarga el XML con la password enmascarada
# 2) 02-runs.sh            → lista runs del workflow + smoke a la URL
```

`publish-profile.xml` queda en `scripts/` y está en `.gitignore` —
nunca llega a git. **Solo lectura**: no crea ni modifica recursos.

## Despliegue por Portal (entregable, 8 pasos del slide 2)

1. **Web App F1** creada (Linux, .NET 8/10) — Portal → App Service →
   Create → SKU F1 (slide 4).
2. **Repo de GitHub** público — `gh repo create` o https://github.com/new
   (slide 6).
3. **Descarga el publish profile** desde Portal → Web App → Get
   publish profile, o `az webapp deployment list-publishing-profiles
   --xml` (slide 7).
4. **Crea el secret** `AZURE_WEBAPP_PUBLISH_PROFILE` en
   `Settings → Secrets and variables → Actions → New repository
   secret` (slide 8). Borra el XML local justo después.
5. **Workflow** en `.github/workflows/deploy.yml` con los 6 steps
   canónicos (slide 9). Sustituye el placeholder
   `<CAMBIAD_POR_VUESTRO_APP_NAME>` por el nombre real.
6. **Push** a `main` — el workflow arranca solo (slide 10).
7. **Verifica** con `curl https://<app>.azurewebsites.net/` que la
   versión nueva responde (slide 11).
8. **Cleanup** al final: `az group delete` + `gh repo delete` + `gh
   secret delete` (slide 16).

## Ideas centrales

> **Publish Profile = setup en 5 min** (vs 30-60 min OIDC). Es una
> password de vida larga; perfecta para **side-projects, MVPs y
> aprender CI/CD**. **OIDC = producción seria**: tokens de minutos,
> federated credentials, sin nada que rotar (slide 13). **Migración
> futura es trivial**: solo cambias el step de auth y el secret se
> vuelve `vars.AZURE_CLIENT_ID` + `vars.AZURE_TENANT_ID`. **Rotación
> obligatoria** del publish profile cada 90 días si te quedas con él
> en producción (slide 18). **Cleanup siempre** — F1 es gratis pero
> los repos huérfanos se acumulan.

## Cierre M08

Con S8.P2 cerramos el módulo M08 (8/8): Repos/Boards, Pipelines YAML,
Despliegue, ADO vs GHA, IaC Bicep, App Insights, Práctica Pipeline,
Práctica GitHub Actions. **Próximo: M09 — IA como Herramienta de
Desarrollo (Claude Code + Copilot + MCP)**.
