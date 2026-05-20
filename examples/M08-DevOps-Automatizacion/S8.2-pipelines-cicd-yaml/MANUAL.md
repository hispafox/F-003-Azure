# Manual del alumno — S8.2 · Pipelines CI/CD YAML

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta por qué los pipelines son **código que vive en el repo**, qué jerarquía tiene un `azure-pipelines.yml` real, qué errores estructurales caza el validador y por qué los `trigger:` se combinan en tres bloques estándar (CI + PR + nightly).

Tiempo de lectura: ~25 min. Submódulo de teoría: [M08-S8.2](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.2-pipelines-cicd-yaml-v3.md). Tres piezas (parser con YamlDotNet, validador estructural con seis reglas, advisor de triggers) más un planificador. **Primera excepción a "sin packages": YamlDotNet 16** porque parsear YAML a mano sería ruido sobre la lección.

*Creado: 2026-05-20 23:00 +0200*

---

## 1. La idea en una frase

Los pipelines de Azure DevOps son **un archivo YAML en la raíz del repo** (`azure-pipelines.yml`) que describe qué pasos ejecutar en cada commit. La conversación moderna no es "voy a configurar la build en el portal con clicks" (eso es Classic Pipelines, deprecado), sino **escribir el pipeline en código, versionarlo en el repo, revisarlo en PRs como cualquier otro código**. El submódulo enseña a leer y validar ese YAML: parsear la jerarquía stages → jobs → steps, detectar errores estructurales antes de subirlo (dependsOn rotos, deployment sin environment, falta step de tests), y construir los tres bloques de `trigger:` típicos.

---

## 2. El problema real que hay detrás

Tres situaciones que justifican validar el YAML antes de subirlo:

**Caso 1 — el `dependsOn` apuntando a un stage que no existe.** Un equipo escribió un pipeline con stages `Build`, `Deploy_Staging` y `Deploy_Production`. En `Deploy_Production` puso `dependsOn: Deploy_Stagging` (con doble `g`). Azure DevOps **aceptó el YAML, lo subió, lo ejecutó**, y el stage de producción quedó **colgado para siempre** esperando a un stage que no existía. Nadie se enteró durante días porque el stage Build pasaba verde. El validador del ejemplo lo cazaría en milisegundos: "Stage 'Deploy_Production' depende de 'Deploy_Stagging' que no existe".

**Caso 2 — el deployment sin environment.** Otro equipo configuró un `deployment:` job para producción, con sus `strategy.runOnce.deploy.steps`, pero **olvidó la propiedad `environment:`**. Azure DevOps lo aceptó. El deploy se ejecutó sin pasar por el environment, así que **no hubo aprobación manual**. Cambio rota llegó a producción a las 3 de la madrugada. El validador caza esto: "Deployment job 'X' sin `environment:`".

**Caso 3 — el stage de Build sin tests.** Un equipo nuevo creó su primer pipeline. Stages: `Build` (con `dotnet restore` + `dotnet build` + `publish`), `Deploy`. **Olvidaron `dotnet test`**. El pipeline pasaba verde siempre. Tres releases después descubren que llevan meses sin ejecutar los tests del proyecto en CI. El validador avisa: "Stage 'Build' no parece tener un step de tests".

Los tres casos los previene el validador estructural del ejemplo. Y un cuarto que también detecta: jobs normales (no `deployment:`) con nombre de environment que parece producción — no podrán usar aprobaciones porque solo los `deployment:` jobs las soportan.

---

## 3. Por qué esto importa en tu stack

Si tienes —o vas a tener— un pipeline CI/CD para tu proyecto en Azure DevOps, las tres preguntas obvias:

- **¿Cómo se estructura mi pipeline?** Stages → jobs → steps. Cada stage es un bloque grande (Build, Test, Deploy_Staging, Deploy_Production); cada job dentro corre en un agente; cada step es una task concreta. La jerarquía no es opcional: si te saltas stages, Azure DevOps asume un single-stage pipeline con limitaciones (no hay aprobaciones por environment).
- **¿Qué triggers configuro?** Lo estándar: `trigger:` para CI en `main`, `pr:` para validar pull requests, `schedules:` para nightly. Los tres bloques cubren el 95% de los casos.
- **¿Cómo valido el YAML antes de subirlo?** Localmente: con el parser y validador del ejemplo. En CI: con el linter de Azure DevOps (más estricto). En producción: revisar el primer run con calma; corregir si hay errores; nunca asumir "lo que pasó verde una vez está bien".

Tres respuestas claras te ahorran horas de "el pipeline no hace lo que esperaba".

---

## 4. La analogía vertebradora: la cadena de montaje

Imagina una cadena de montaje en una fábrica. Tiene tres niveles jerárquicos:

- **Fase** (stage): un bloque grande de la cadena. Por ejemplo "Soldadura", "Pintura", "Control de calidad", "Embalaje".
- **Estación dentro de la fase** (job): un puesto concreto. En la fase "Pintura": "Primer", "Color base", "Acabado". Cada estación puede correr en un sitio físico distinto (en pipelines: un agente distinto).
- **Tarea dentro de la estación** (step): la acción concreta. En la estación "Color base": "Limpiar superficie", "Aplicar capa 1", "Esperar 5 min", "Aplicar capa 2".

Entre fases hay **dependencias declaradas** (`dependsOn`). Pintura depende de Soldadura: no se puede pintar antes. Control de calidad depende de Pintura. Si te confundes y declaras que Embalaje depende de "Pinturas" (con plural), la cadena se detiene esperando una fase que no existe — el caso 1.

Algunas fases **necesitan permiso** antes de ejecutarse: por ejemplo, "Despacho a clientes" requiere firma del responsable. En pipelines, eso son los **environments con aprobaciones**: defines un environment "production", configuras una aprobación manual, y el `deployment:` job se queda esperando aprobación antes de correr. Si tu job es **normal** (no `deployment:`), no puede registrar la aprobación — sigue corriendo sin parar.

Y luego están los **disparadores** de la cadena: la cadena no funciona sola, alguien tiene que decirle "empieza". Tres formas:

- **A cada producto que entra** (commit a `main`): `trigger:`.
- **Antes de aceptar un pedido nuevo** (PR): `pr:`.
- **Por la noche, para revisión** (cron): `schedules:`.

Mantén la imagen mientras lees el código: fases-estaciones-tareas, dependencias, aprobaciones por environment, disparadores.

---

## 5. Recorrido por el código

### `PipelineYamlParser` — del YAML al DTO

El parser usa **YamlDotNet 16**, primer paquete externo que el módulo añade. La decisión: parsear YAML a mano significaría reescribir media librería; mejor delegar.

El parser convierte un `azure-pipelines.yml` en una jerarquía de records C#:

```csharp
public sealed record PipelineDef(
    IReadOnlyList<string> TriggerBranches,
    string? VmImage,
    IReadOnlyList<string> VariableGroups,
    IReadOnlyList<StageDef> Stages);

public sealed record StageDef(
    string Name,
    IReadOnlyList<string> DependsOn,
    IReadOnlyList<JobDef> Jobs,
    string? Condition);

public sealed record JobDef(
    string Name,
    IReadOnlyList<StepDef> Steps,
    bool IsDeployment,
    string? Environment,
    string? Strategy);

public sealed record StepDef(string? Display, string? Body);
```

Detalles que el parser maneja bien:

- **`trigger: none`** (string en vez de mapping): lo trata como "sin trigger".
- **`deployment:` jobs**: detecta `strategy.runOnce.deploy.steps` y extrae los steps desde ahí.
- **Variable groups y schedules**: los lee del nivel raíz.
- **YAML inválido**: lanza `FormatException` con mensaje claro.

Tener el DTO te permite hacer cualquier cosa después: validar, recomendar cambios, generar diagramas, comparar dos pipelines.

### `PipelineStructureValidator.Validar` — las seis reglas

La función central. Devuelve errores y avisos:

```csharp
public sealed record ResultadoValidacion(
    bool Valido,
    IReadOnlyList<string> Errores,
    IReadOnlyList<string> Avisos);
```

**Errores** (bloquean el pipeline):

1. **Sin stages**: `errores.Add("El pipeline no tiene 'stages'")`.
2. **Stage sin nombre**: `errores.Add("Hay un stage sin 'stage:' (nombre)")`.
3. **`dependsOn` a stage inexistente**: `errores.Add($"Stage '{x}' depende de '{y}' que no existe")`. **Este es el caso 1 de la sección 2.**
4. **Stage sin jobs**: el stage es un contenedor vacío, no hace nada.
5. **Job sin steps**: igual, contenedor vacío.
6. **Deployment job sin environment**: `errores.Add($"Deployment job '{x}' sin 'environment:'")`. **Este es el caso 2.**

**Avisos** (no bloquean pero conviene mirarlos):

- **Stage de Build sin step de test**: detecta `dotnet test`, `VSTest`, `PublishTestResults` o display que contenga "test". Si no aparece, aviso. **Este es el caso 3.**
- **Job normal con environment de producción**: si el job no es `deployment:` y su `environment:` se parece a "production", aviso porque no podrá usar aprobaciones.

La función detecta producción de forma flexible:

```csharp
private static readonly HashSet<string> ProdAliases = new(StringComparer.OrdinalIgnoreCase)
{
    "prod", "production", "produccion",
};
```

Si tu environment se llama "Production-EU" o "prod-eu", el aviso salta. Eso evita el típico "configuré aprobaciones en el environment pero el deploy se las salta".

### `TriggerAdvisor.Recomendar` — los tres bloques estándar

Genera el YAML de cuatro escenarios:

**`CiPrincipal`** — CI en push a `main`, ignorando cambios solo de docs:

```yaml
trigger:
  branches:
    include: [main]
  paths:
    include: [src/*, tests/*]
    exclude: ['*.md', docs/*]
```

**`ValidacionPr`** — validar PRs hacia `main`:

```yaml
pr:
  branches:
    include: [main]
  paths:
    include: [src/*]
```

**`NightlyBuild`** — build nocturno a las 2:00, aunque no haya cambios:

```yaml
schedules:
- cron: '0 2 * * *'
  displayName: 'Nightly Build'
  branches:
    include: [main]
  always: true
```

**`ManualOnly`** — sin trigger automático:

```yaml
trigger: none
```

Y la **recomendación estándar** (`RecomendacionEstandar()`) combina los tres primeros. Para un repo "serio" típico, los tres son útiles:

- CI principal: build automático cuando algo entra a `main`.
- Validación PR: cada PR pasa por CI antes de poder mergearse.
- Nightly: una vuelta entera todas las noches, captura problemas que solo aparecen con tiempo (dependencias caducadas, certificados expirando, datos de prueba acumulados).

Lo de `paths.exclude: ['*.md', docs/*]` es una optimización clásica: si solo cambia el README, no merece la pena un build de 5 minutos. La build se salta y el commit pasa "verde" inmediatamente.

### `PipelinePlanner` — el plan + checklist

El servicio inyectable. Recibe un YAML, lo parsea, lo valida, y devuelve un plan con:

- Resultado del parser (jerarquía completa).
- Resultado del validador (errores + avisos).
- Bloques de `trigger:` recomendados.
- Checklist del entregable (slide 6, 7, 8, 9, 15, 22 — variable groups, Key Vault, OIDC, caching, etcétera).

---

## 6. La conversación con seguridad: secretos en el pipeline

Una parte que el ejemplo no implementa pero el checklist menciona y vale la pena tener clara. **Cómo NO se hacen los secretos en un pipeline**:

❌ Variables del pipeline en claro:

```yaml
variables:
  dbPassword: 'Pa$$w0rd!'   # NUNCA
```

Cualquiera con acceso al repo lo lee.

❌ Variables marcadas como "secret" en la UI:

```yaml
variables:
- name: dbPassword
  value: $(dbPassword)   # configurado en la UI
```

Mejor, pero las variables del pipeline se ven en logs si alguien hace `echo $(dbPassword)`. Y rotar es manual.

✅ **Variable Group linked a Key Vault**:

```yaml
variables:
- group: prod-secrets   # variable group ligado a un Key Vault
```

El variable group se configura una vez en Library → Variable groups → Link secrets from Azure Key Vault. A partir de ahí, los secretos vienen del Vault en cada run; rotar es rotar en el Vault. Tu código no se entera.

Y para autenticarse contra Azure sin secrets: **Service Connection con OIDC** (Federated Identity). El pipeline obtiene un token corto cada vez que arranca, sin password almacenado. Es lo que recomienda el checklist.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/Pipelines.Demo.Api
# http://localhost:5106
```

Endpoints:

```http
### Parsear un YAML
POST http://localhost:5106/pipeline/parsear
Content-Type: application/x-yaml

trigger:
  branches:
    include: [main]
pool:
  vmImage: 'ubuntu-latest'
stages:
- stage: Build
  jobs:
  - job: BuildJob
    steps:
    - script: dotnet build
    - script: dotnet test
- stage: Deploy
  dependsOn: Build
  jobs:
  - deployment: DeployJob
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - script: az webapp deploy ...

### Validar (devuelve errores + avisos)
POST http://localhost:5106/pipeline/validar
Content-Type: application/x-yaml

# (mismo YAML; respuesta dice si pasa)

### Obtener el bloque de trigger nightly
GET http://localhost:5106/pipeline/trigger/recomendado?escenario=NightlyBuild

### Los tres bloques estándar
GET http://localhost:5106/pipeline/trigger/estandar
```

Los 29 tests cubren el parseo de YAMLs típicos, los seis casos de error del validador, los avisos (falta test, environment de prod sin deployment), los cuatro escenarios de trigger.

Para auditar el proyecto real de Azure DevOps:

```bash
./scripts/demo.sh
# 1) 01-inventory-pipelines.sh → pipelines + últimas 5 runs por pipeline + environments
```

Te muestra qué pipelines tienes, cómo van las últimas runs, y qué environments tienes con sus aprobaciones. Solo lectura.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. Los tres errores que cazas en la primera review de PR

Cuando te llegue el primer PR con un `azure-pipelines.yml`, mira específicamente:

**Error 1 — `dependsOn` mal escrito**. Pasa el YAML por el validador. Si dice "stage X depende de Y que no existe", arregla.

**Error 2 — Deployment sin environment**. El validador lo detecta. La regla operativa: cualquier job `deployment:` exige `environment:`. Sin él, no hay aprobaciones ni separación staging/prod.

**Error 3 — Stage de Build sin test**. El validador avisa. Es lo primero que un reviewer competente comprueba: ¿se están ejecutando los tests en CI o solo el build? Sin tests, el pipeline es decorativo.

Una review de 30 segundos pasando el YAML por el validador te ahorra incidentes futuros.

---

## 9. Los anti-patterns operativos

Cinco prácticas que verás en pipelines mal mantenidos:

**Anti-pattern 1 — Pipelines "Classic" con clicks en la UI**. La configuración no está en el repo, no se versiona, no se revisa. Tampoco se reproduce en otro repo. **Pipelines como código siempre**.

**Anti-pattern 2 — Un solo stage gigante con 30 steps**. Imposible reutilizar, imposible aplicar aprobaciones por environment, imposible paralelizar. **Stages bien delimitados, jobs claros**.

**Anti-pattern 3 — Secretos en variables del pipeline**. Resuelto arriba: Variable Group linked a Key Vault.

**Anti-pattern 4 — Sin caching**. `dotnet restore` baja 200 MB cada run. Con `Cache@2` con clave sobre `**/*.csproj`, baja una vez al día. Pipelines 4 veces más rápidos.

**Anti-pattern 5 — Templates monolíticos sin reutilización**. Cada repo de la organización tiene su propio `azure-pipelines.yml` copiado y pegado. Cuando cambias algo (versión de SDK, política de tests, etcétera), tienes que cambiar 15 archivos. Soluciónalo con **templates en un repo compartido** que cada repo incluye con `template: ...@template-repo`.

---

## 10. Glosario breve

- **`azure-pipelines.yml`**: archivo que define el pipeline. Vive en la raíz del repo.
- **Stage**: bloque de pipeline. Típicamente: Build, Test, Deploy_Staging, Deploy_Production.
- **Job**: bloque dentro de un stage que corre en un agente. Puede ser normal o `deployment:`.
- **Step**: tarea atómica dentro de un job. `script:`, `task:`, `pwsh:`, etcétera.
- **`deployment:` job**: tipo especial de job que se integra con environments y aprobaciones.
- **Environment**: recurso de Azure DevOps con nombre (`production`, `staging`) y configuración (aprobaciones, checks). Los `deployment:` jobs apuntan a uno.
- **`dependsOn`**: declaración de dependencia entre stages o jobs.
- **`trigger:`**: bloque que define qué dispara el pipeline (commits a ramas específicas).
- **`pr:`**: bloque que define qué PRs disparan el pipeline.
- **`schedules:`**: bloque para builds programados (cron).
- **Variable Group**: conjunto de variables (típicamente secretos) reutilizable entre pipelines, opcionalmente linked a Key Vault.
- **Service Connection**: credencial para que el pipeline acceda a Azure (clásico: secret; moderno: OIDC).
- **OIDC / Federated Identity**: forma moderna de autenticar el pipeline contra Azure sin secret almacenado.
- **MS-hosted agent**: máquina virtual gestionada por Microsoft donde corre el job. 1.800 min gratis al mes.
- **Self-hosted agent**: agente que tú instalas en tu propia infra. Ilimitado en tiempo pero tú lo mantienes.

---

## 11. Cierre

S8.2 te da el modelo mental del pipeline como código: jerarquía stages → jobs → steps, validación estructural antes de subir, bloques de trigger estándar, secretos en Variable Group linked a Key Vault, autenticación con OIDC. Si tu PR de pipeline pasa el validador del ejemplo y aplica las cinco mejores prácticas, vas a tener un pipeline robusto desde el día uno.

Lo siguiente es [`S8.3 — Despliegue automatizado`](../S8.3-despliegue-automatizado/MANUAL.md), donde se cubre cómo se hace un release real: estrategias (blue/green, canary, ring-based), health checks post-deploy y rollback.
