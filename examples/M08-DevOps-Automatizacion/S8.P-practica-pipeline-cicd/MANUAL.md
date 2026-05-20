# Manual del alumno — S8.P · Práctica Pipeline CI/CD completo

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta cómo se compone un pipeline CI/CD real (tres puertas: build verde, smoke staging, aprobación humana), qué requisitos verificar antes de empezar para no perder 30 minutos a mitad, y por qué OIDC > Service Principal con secret.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M08-S8.P](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.P-practica-pipeline-cicd-v3.md). Tres piezas de lógica pura (validador preflight con 10 comprobaciones, constructor de stages canónicos para ADO/GitHub, evaluador de smoke test con auto-rollback).

*Creado: 2026-05-21 00:55 +0200*

---

## 1. La idea en una frase

S8.P es **la práctica integradora del módulo M08**: monta un pipeline CI/CD end-to-end que cubre todo lo aprendido — Repos+Boards (S8.1), Pipelines YAML (S8.2), despliegue automatizado (S8.3), elección de plataforma (S8.4), y la verificación post-deploy con observabilidad (S8.6). El pipeline real tiene **tres puertas**: build con tests pasando (sin verde no se publica artifact), smoke test del slot staging (sin `/health` 200 no hay swap), y aprobación humana del environment de producción (el último click es de una persona, no automático).

El ejemplo materializa las tres decisiones operativas que diferencian un pipeline bueno de uno casero: preflight pre-práctica para no atascarse, esqueleto canónico con auto-rollback automatizado (`condition: failed()`), y la evaluación de smoke con umbrales claros (HTTP 200, latencia < 2s, error rate < 1%).

---

## 2. El problema real que hay detrás

Tres situaciones que justifican guiar la práctica con preflight + checklist:

**Caso 1 — el plan F1 que no soporta slots.** Un alumno empezó la práctica con un App Service plan Free. Llegó al paso 5 (deploy a staging) y el comando falló: "Deployment slots require Standard tier or higher". **30 minutos perdidos** investigando por qué. La validación `PreflightChecker` del ejemplo lo dice antes de empezar: "App Service Plan S1 o superior → BLOQUEANTE". Cinco segundos vs treinta minutos.

**Caso 2 — el Service Principal con secret que caducó.** Otro equipo configuró el pipeline con Service Connection clásico (Service Principal con secret). El pipeline funcionó tres meses. Al cuarto mes, **el secret expiró** (default: 1-2 años, pero el admin lo había puesto a 90 días). **Pipeline rojo durante 24 horas** hasta que alguien identificó el problema, fue a Entra, generó nuevo secret, actualizó la Service Connection. La solución correcta del slide 17: **OIDC con Workload Identity Federation**. El token dura 1 hora, se renueva solo, **no hay nada que rotar manualmente**.

**Caso 3 — el smoke test que pasaba con un error sutil.** Un equipo tenía smoke test simple: "curl /health → 200 = OK". El deploy pasaba. Pero la app, **aunque devolvía 200**, tenía latencia degradada (4 segundos por request) y un 5% de errores en otros endpoints. **Los usuarios sufrían 3 horas hasta que alguien lo notó**. La solución: smoke test con tres umbrales (HTTP 200 + latencia media < 2s + error rate < 1%). El `SmokeTestEvaluator` del ejemplo aplica exactamente eso. Si alguno falla, **auto-rollback**.

Los tres casos los previene el ejemplo: preflight detecta el caso 1 en milisegundos, el esqueleto recomienda OIDC para el caso 2, el evaluador con tres umbrales caza el caso 3.

---

## 3. Por qué esto importa en tu stack

Si tu equipo va a tener un pipeline serio en producción, los tres conceptos que aplica esta práctica son los que diferencian "pipeline que funciona" de "pipeline que aguanta producción":

- **Tres puertas, no una**. Build → smoke → aprobación. Sin las tres, te encuentras desplegando código roto, con latencia degradada o sin que nadie haya revisado el cambio.
- **OIDC, no secretos**. Workload Identity Federation desde el día uno. Sin rotación, sin caducidad, sin sustos a las 9 de la mañana.
- **Auto-rollback con `condition: failed()`**. Si el smoke test post-swap falla, el pipeline ejecuta el swap inverso automáticamente. **MTTR baja de 30-60 min a < 2 min**. Sin esto, el "rollback" depende de que alguien lo decida y lo ejecute manualmente.

Las tres se aprenden montando esta práctica una vez. A partir de ahí, las replicas en cualquier proyecto nuevo.

---

## 4. La analogía vertebradora: la entrega de un producto en una tienda

Imagina una tienda física que recibe productos del almacén central. Hay tres controles antes de que el producto llegue a la estantería:

- **Control 1 — Inspección de calidad en el almacén** (stage Build): los productos pasan QA, tests, packaging. Si alguno falla, **no sale del almacén**. No se manda a la tienda. Es la primera puerta: sin tests verdes, no hay artifact.
- **Control 2 — Vitrina de prueba en el backstore** (stage DeployStaging + smoke test): el producto llega a la tienda pero **no a la estantería pública todavía**. Se pone en una vitrina interna. El responsable de tienda lo prueba con tres criterios: ¿el escaparate se ve bien? ¿el cliente puede leer las etiquetas a 2 metros? ¿al tocarlo no se desmonta? Si los tres OK, **listo para pasar a la estantería**. Si alguno falla, vuelta al almacén.
- **Control 3 — Aprobación del dueño de la tienda** (environment de producción con required reviewers): el responsable mira el producto en la vitrina y decide si lo pasa a la estantería principal. Es un **gesto humano**, no automático. El último click es de una persona porque puede haber **contexto que la automatización no ve** ("hoy es Black Friday, mejor no hacer cambios", "el cliente importante viene mañana, esperemos").

Y luego está el **sistema de seguridad** detrás de los tres controles: si en cualquier momento alguien detecta que el producto colocado en la estantería no funciona como esperaba (el smoke post-swap falla), **se retira automáticamente** y se reemplaza por la versión anterior. No hay que llamar al gerente; el sistema lo hace solo. Eso es `condition: failed()` con auto-rollback.

Y un detalle de gestión: **el almacén central usa una credencial corporativa** (Workload Identity Federation) para mandar productos a la tienda, no una llave física que se pueda perder y haya que cambiar cada cierto tiempo. La credencial se renueva automáticamente con el ID corporativo del operario que trabaja en cada momento.

Mantén la imagen mientras lees: tres controles, sistema de seguridad automático, credencial corporativa sin físico que rotar.

---

## 5. Recorrido por el código

### `PreflightChecker.Comprobar` — las 10 comprobaciones previas

La función central:

```csharp
public static ReportePreflight Comprobar(EscenarioPreflight e)
{
    var hallazgos = new List<Hallazgo>
    {
        Check(e.TieneOrgADO, "Azure DevOps Organization", "...", Bloqueante),
        Check(e.TieneRepoConPushAccess, "Repo con push access", "...", Bloqueante),
        Check(e.TieneSuscripcionAzure, "Suscripción Azure", "...", Bloqueante),
        Check(e.EsAdminProyectoADO, "Project Administrator en ADO", "...", Bloqueante),
        Check(e.EsOwnerOUserAccessAdmin, "Owner o User Access Administrator", "...", Bloqueante),
        Check(e.PlanS1OSuperior, "App Service Plan S1+", "...", Bloqueante),
        Check(e.SlotStagingExiste, "Slot 'staging' existe", "...", Bloqueante),
        Check(e.TieneAzCliInstalado, "Azure CLI disponible", "...", Aviso),
        Check(e.TieneAppRegistration, "App Registration en Entra ID", "...", Aviso),
        Check(e.TieneServiceConnectionOidc, "Service Connection OIDC", "...", Aviso),
    };

    bool listo = !hallazgos.Any(h => h.Nivel == Bloqueante);
    return new ReportePreflight(listo, hallazgos);
}
```

Diez comprobaciones clasificadas en **bloqueantes** (sin esto la práctica no avanza) y **avisos** (se puede empezar pero conviene resolverlos).

**Bloqueantes** (7):

1. **ADO Organization disponible**: sin ella no hay donde crear el pipeline.
2. **Repo con push access**: sin push no se dispara el pipeline.
3. **Suscripción Azure activa**: sin Azure no hay slot al que desplegar.
4. **Project Administrator en ADO**: necesario para crear Service Connections.
5. **Owner / User Access Administrator en Azure**: necesario para crear App Registration + role assignments.
6. **App Service Plan S1+**: los slots requieren Standard tier. **El caso 1 de la sección 2 falla aquí**.
7. **Slot `staging` existe**: si no, el pipeline no puede desplegar al slot.

**Avisos** (3): tener `az` CLI instalado, App Registration en Entra ID, Service Connection con OIDC. Útiles para no atascarse pero no son obligatorios desde el minuto cero.

La función devuelve `ListoParaArrancar = true` solo si **no hay ningún bloqueante**. Cualquier aviso permite seguir.

### `PipelineStageBuilder.Construir` — el esqueleto canónico

Genera los tres stages básicos + condicionales según opciones:

```csharp
public static IReadOnlyList<EtapaPipeline> Construir(OpcionesPipeline o)
{
    var etapas = new List<EtapaPipeline>();

    if (o.EscanearVulnerables)
        etapas.Add(SecurityScanStage(o));     // dotnet list package --vulnerable

    etapas.Add(BuildStage(o));                 // restore + build + test + publish

    etapas.Add(DeployStagingStage(o));         // deploy slot staging + smoke test
    if (o.AutoRollback) etapas.Last().PasosFin.Add("Auto-rollback si smoke falla");

    etapas.Add(SwapProductionStage(o));        // RequiereAprobacion = true

    return etapas;
}
```

Stages estándar:

- **`SecurityScan`** (opcional): `dotnet list package --vulnerable --include-transitive`. Si encuentra CVEs, falla el pipeline. Va antes del Build para fallar rápido.
- **`Build`**: `dotnet restore` + `dotnet build` + `dotnet test` + `dotnet publish` + upload artifact. Sin tests verdes no se publica el artifact.
- **`DeployStaging`**: descarga el artifact, despliega al slot `staging`, ejecuta `curl /health` con retry (5×10s). **Es la primera vez que el código se ejecuta como app desplegada**.
- **`SwapProduction`**: con `RequiereAprobacion = true`. Espera a que el reviewer humano apruebe. Tras aprobar: swap staging→production, smoke test post-swap. **Si el smoke falla**: auto-rollback (`condition: failed()` ejecuta swap inverso).

Y dos knobs según plataforma:

- **`OpcionesPipeline.Plataforma`**: AzureDevOps (usa `task: AzureWebApp@1`) o GitHubActions (usa `uses: actions/setup-dotnet@v4` + `actions/upload-artifact@v4`). El builder devuelve el esqueleto traducido a la sintaxis correcta. **El mismo deploy expresado en dos sintaxis**.
- **`OpcionesPipeline.UsarOidc`**: si está en `true`, las tasks de auth usan Workload Identity Federation. Si en `false`, Service Principal con secret (legacy). El builder añade nota sobre rotación.

### `SmokeTestEvaluator.Evaluar` — las tres puertas del smoke

La función central:

```csharp
public static ResultadoSmoke Evaluar(MedidasSmoke medidas, UmbralesSmoke? umbrales = null)
{
    umbrales ??= new UmbralesSmoke();   // defaults: 200 / 2.0s / 1.0%

    var razones = new List<string>();

    if (medidas.HttpCode != umbrales.HttpCodeEsperado)
        razones.Add($"HTTP {medidas.HttpCode}, esperado {umbrales.HttpCodeEsperado}");

    if (medidas.LatenciaMediaSegundos > umbrales.LatenciaMaxSegundos)
        razones.Add($"Latencia {medidas.LatenciaMediaSegundos}s > {umbrales.LatenciaMaxSegundos}s");

    if (medidas.ErrorRatePorcentaje > umbrales.ErrorRateMaxPorcentaje)
        razones.Add($"Error rate {medidas.ErrorRatePorcentaje}% > {umbrales.ErrorRateMaxPorcentaje}%");

    var decision = razones.Count == 0 ? Continuar : RollbackNecesario;
    return new ResultadoSmoke(decision, razones, detalles);
}
```

Tres comprobaciones en paralelo, **basta que una falle para que el smoke devuelva RollbackNecesario**:

1. **HTTP Code = 200** (o el esperado): el health endpoint responde OK.
2. **Latencia media < 2 segundos**: la app no está degradada.
3. **Error rate < 1%**: los otros endpoints no están fallando.

Los umbrales son configurables. Para una API crítica con SLA estricto, podrías bajar la latencia a 1s. Para una app interna casual, subirla a 5s. **Los defaults son razonables para el 80% de los casos**.

Y la pieza importante: la función devuelve **todas las razones**, no solo la primera. Si tu smoke falla por dos motivos (latencia + error rate), el log del pipeline muestra los dos. Diagnóstico instantáneo.

### `PracticaPipelinePlanner` — el plan + checklist de 12 puntos

El servicio inyectable. Compone:

- Reporte preflight.
- Esqueleto de stages según opciones (plataforma, OIDC, auto-rollback, security scan).
- Evaluador de smoke con umbrales por defecto.
- Checklist de 12 puntos del entregable.

Checklist del entregable (slide 11 enriquecido):

```
[ ] Preflight verde (sin bloqueantes)
[ ] Service Connection con OIDC (sin secrets)
[ ] azure-pipelines.yml en el repo
[ ] Pipeline configurado en ADO (o GitHub Actions)
[ ] Environment 'staging' sin aprobación
[ ] Environment 'production' con required reviewers
[ ] Push a main dispara el pipeline
[ ] Build con tests verdes publica artifact
[ ] Deploy a staging + smoke test (3 umbrales)
[ ] Aprobación humana del environment de producción
[ ] Swap staging→production + smoke post-swap
[ ] Auto-rollback ejecuta swap inverso si smoke falla
```

Los 12 puntos cubren el flujo end-to-end. Cuando los 12 están verdes, la práctica está completa.

---

## 6. Las tres puertas, en una imagen

| Puerta | Quién decide | Criterio | Tiempo |
| --- | --- | --- | --- |
| **1. Build verde** | Pipeline (automático) | `dotnet test` pasa | minutos |
| **2. Smoke staging** | Pipeline (automático) | `/health` 200, latencia < 2s, error rate < 1% | minutos |
| **3. Aprobación humana** | Tech lead / on-call | Lo que su criterio diga | hasta horas |

Las tres puertas son acumulativas. Pasar la primera no implica pasar las otras. **Las tres juntas son lo que aguanta producción**.

Y dentro de las puertas hay **redes de seguridad**:

- Tras la puerta 2, si el smoke falla → no avanza, no se hace swap. Pipeline rojo, equipo se entera, investigan.
- Tras la puerta 3 + swap, si el smoke post-swap falla → **auto-rollback con swap inverso**. Cinco segundos de degradación máxima.

Sin estas redes, una sola puerta mal puesta tumba producción.

---

## 7. La conversación con seguridad: OIDC vs Service Principal

Hay dos formas de que un pipeline se autentique contra Azure. Vale la pena conocer las dos:

**Service Principal con secret** (la legacy):

- Configurable desde la UI de ADO o GitHub.
- El secret tiene caducidad (1-24 meses).
- Si caduca, todos los pipelines que lo usan fallan hasta rotarlo.
- El secret se almacena en ADO/GitHub. Quien tenga acceso al pipeline puede leerlo en logs si se imprime accidentalmente.

**OIDC / Workload Identity Federation** (la moderna):

- Configurable desde ADO o GitHub con un wizard.
- **No hay secret almacenado**. La autenticación es por token de identidad firmado por el provider (ADO/GitHub) que Entra ID valida.
- El token dura 1 hora y se renueva en cada run.
- **Nada que rotar**.
- Más seguro porque no hay secret que pueda filtrarse.

El caso 2 de la sección 2 (secret caducado a las 90 días) es el típico problema operativo de Service Principal. Con OIDC no existe. La recomendación clara del módulo: **OIDC desde el día uno**.

Para configurarlo:

1. En Azure: crea una App Registration; bajo "Federated credentials", añade una credential federada apuntando a tu pipeline (ADO con su issuer / GitHub con su repo).
2. Asigna a la App Registration el rol RBAC necesario sobre el recurso (Contributor sobre el RG, normalmente).
3. En ADO/GitHub: crea Service Connection / configura el secret de GitHub con `azure/login@v2` apuntando a la App Registration con federación.

Una vez configurado, **olvídate**. Sigue funcionando para siempre sin intervención.

---

## 8. Cómo probarlo en local

```bash
dotnet run --project src/Practica.Pipeline.Demo.Api
# http://localhost:5111
```

Endpoints:

```http
### Comprobar prerequisitos
POST http://localhost:5111/pipeline/preflight
Content-Type: application/json

{
  "tieneOrgADO": true,
  "tieneRepoConPushAccess": true,
  "tieneSuscripcionAzure": true,
  "esAdminProyectoADO": true,
  "esOwnerOUserAccessAdmin": true,
  "planS1OSuperior": false,            // ← bloqueante
  "slotStagingExiste": false,           // ← bloqueante
  "tieneServiceConnectionOidc": false   // aviso, no bloqueante
}
# → { listoParaArrancar: false, hallazgos: [...2 bloqueantes, 1 aviso...] }

### Esqueleto del pipeline para ADO con OIDC y auto-rollback
POST http://localhost:5111/pipeline/etapas
Content-Type: application/json

{
  "plataforma": "AzureDevOps",
  "usarOidc": true,
  "autoRollback": true,
  "escanearVulnerables": true
}
# → [SecurityScan, Build, DeployStaging, SwapProduction]

### Evaluar smoke test
POST http://localhost:5111/pipeline/smoke
Content-Type: application/json

{
  "medidas": { "httpCode": 200, "latenciaMediaSegundos": 1.5, "errorRatePorcentaje": 0.3 },
  "umbrales": { "httpCodeEsperado": 200, "latenciaMaxSegundos": 2.0, "errorRateMaxPorcentaje": 1.0 }
}
# → { decision: "Continuar", razones: ["Smoke test pasa..."], detalles: [...] }

### Plan completo
POST http://localhost:5111/pipeline/plan
```

Los 32 tests cubren las 10 comprobaciones del preflight (con todas las combinaciones de bloqueante/aviso), el constructor de stages para ADO vs GitHub Actions con sus distintas opciones, y el evaluador de smoke con cada umbral por separado y combinados.

Para verificar prerequisitos contra tu Azure real:

```bash
./scripts/demo.sh
# 1) 01-preflight.sh           → plan S1+, slot staging, deploys recientes
# 2) 02-smoke-test.sh staging  → /health 200 + latencia < 2s sobre 10 reqs
# 3) 02-smoke-test.sh production → smoke post-swap
```

Solo lectura: nunca crea ni modifica recursos. El smoke test emula el step 5 del pipeline real.

> Yo no lanzo apps. Tú haces `dotnet run`, `dotnet test` y `az`.

---

## 9. La rúbrica del entregable

Para que la práctica cuente como completada, los doce puntos del checklist deben estar verdes:

```
[x] Preflight verde (sin bloqueantes)
[x] Service Connection con OIDC (sin secrets)
[x] azure-pipelines.yml en el repo
[x] Pipeline configurado en ADO (o GHA equivalente)
[x] Environment 'staging' sin aprobación
[x] Environment 'production' con required reviewers
[x] Push a main dispara el pipeline
[x] Build con tests verdes publica artifact
[x] Deploy a staging + smoke test (3 umbrales)
[x] Aprobación humana del environment de producción
[x] Swap staging→production + smoke post-swap
[x] Auto-rollback ejecuta swap inverso si smoke falla
```

Los 12 cubren las tres puertas + las redes de seguridad. Sin alguno de ellos, la práctica está incompleta y el pipeline tiene un agujero operativo.

---

## 10. Las métricas DORA

Una nota del slide 9 que merece estar en el manual: las **cuatro métricas DORA** (DevOps Research and Assessment) son el estándar para medir la salud de un pipeline:

1. **Deployment frequency**: ¿cuántas veces despliegas a producción? Elite = varias veces al día.
2. **Lead time for changes**: ¿cuánto tarda un commit en llegar a producción? Elite = < 1 hora.
3. **Mean time to recovery (MTTR)**: ¿cuánto tarda en restaurar el servicio tras un incidente? Elite = < 1 hora.
4. **Change failure rate**: ¿qué % de deploys causa incidentes? Elite = 0-15%.

Aplicando esta práctica:

- **MTTR baja a < 2 minutos** gracias al auto-rollback automatizado.
- **Change failure rate baja** porque las tres puertas (build + smoke + aprobación) cazan errores antes de producción.
- **Deployment frequency sube** porque el pipeline es seguro: hacerlo varias veces al día no asusta.

Sin esta arquitectura, las métricas DORA del equipo se quedan en "Medium" o "Low". Con ella, "Elite" es alcanzable.

---

## 11. Glosario breve

- **Preflight**: validación previa de requisitos antes de empezar una operación. Sin preflight, vas a ciegas y te atascas.
- **Service Connection**: credencial de Azure DevOps para acceder a un recurso externo (Azure, GitHub, Docker Hub).
- **Workload Identity Federation (OIDC)**: federación entre ADO/GitHub y Entra ID para autenticación sin secret almacenado.
- **Approval gate**: punto del pipeline donde se requiere intervención humana para continuar. Normalmente vinculado a un environment.
- **Environment**: recurso de ADO/GitHub que agrupa una serie de aprobaciones, secretos y checks para un entorno (staging, production).
- **Required reviewer**: persona o grupo cuya aprobación es necesaria para que el pipeline avance.
- **`condition: failed()`**: condición de Azure Pipelines (`if: failure()` en GitHub Actions) para ejecutar un step solo si algo anterior falló.
- **Auto-rollback**: swap inverso ejecutado automáticamente cuando el smoke post-swap falla. MTTR ≈ 5 segundos.
- **Métricas DORA**: cuatro métricas estándar para medir la salud DevOps (deployment frequency, lead time, MTTR, change failure rate).
- **MTTR** (Mean Time To Recovery): tiempo medio para restaurar el servicio tras un incidente.
- **Smoke test**: serie corta de comprobaciones post-deploy (health + latencia + error rate) que decide si seguir o rollback.

---

## 12. Cierre

S8.P te lleva por el pipeline CI/CD profesional de principio a fin: las tres puertas (build, smoke, aprobación), OIDC desde el día uno (sin secretos que rotar), auto-rollback con `condition: failed()`, smoke test con tres umbrales claros. Si la haces y los 12 puntos del checklist están verdes, tu primer pipeline en producción será robusto. A partir de ahí, replicarlo en otros proyectos es traducción.

Lo siguiente es [`S8.P2 — Práctica GitHub Actions + publish profile`](../S8.P2-practica-github-actions-publish-profile/MANUAL.md), una variante del mismo pipeline en GitHub Actions con publish profile (alternativa más simple a OIDC para repos personales o forks). Cierra el módulo M08.
